using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenRA.Mods.CA.Modular
{
	public sealed class CustomFactionProfile
	{
		public int Schema { get; set; } = 1;
		public string Name { get; set; } = "Meine GDI";
		public string TankName { get; set; } = "Custom Battle Tank";
		public string BaseFaction { get; set; } = "eagle";
		public Dictionary<string, string> Parts { get; set; } = new Dictionary<string, string>
		{
			{ "chassis", "gdi-battle-hull" }, { "running_gear", "tracks-standard" },
			{ "drive", "diesel" }, { "generator", "baseline-generator" }, { "armor", "heavy" },
			{ "carrier", "medium-cannon-mount" }, { "weapon", "medium-cannon" }, { "ammunition", "medium-tank-shell" }
		};
	}

	public sealed class CustomTankValues
	{
		public double Cost, Mass, Hp, Electric, Reserve;
		public int Speed, Turn, Tech, Points;
		public bool Stationary;
	}

	// Pure compiler/persistence service: no Python, world state, UI or engine mutation.
	public sealed partial class CustomFactionDesign
	{
		readonly JObject catalog;
		public static readonly string[] Roles = { "chassis", "running_gear", "drive", "generator", "armor", "carrier", "weapon", "ammunition" };
		static string[][] Bindings => Roles.Select(r => Assemblies.SelectMany(a => a.Choices[r]).Distinct().ToArray()).ToArray();
		static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Error,
			ObjectCreationHandling = ObjectCreationHandling.Replace,
			TypeNameHandling = TypeNameHandling.None
		};

		public CustomFactionDesign(string json)
		{
			catalog = JObject.Parse(json);
			if (catalog.Value<int>("schema") != 1 || catalog["options"] is not JObject || catalog["components"] is not JObject)
				throw new InvalidDataException("Unbekannte Baustein-Version.");
			var alpha = N(catalog, "alpha");
			if (alpha <= 0)
				throw new InvalidDataException("Ungueltige Mobilitaetskurve.");
			for (var i = 0; i < Roles.Length; i++)
			{
				var options = catalog["options"][Roles[i]] as JArray;
				if (options == null || options.Count == 0 || options.Values<string>().Distinct().Count() != options.Count ||
					options.Values<string>().Any(id => !Bindings[i].Contains(id) || catalog["components"][id] is not JObject))
					throw new InvalidDataException("Bausteinkatalog passt nicht zu den Compiler-Bindungen.");
			}
			var descriptors = catalog["assemblies"] as JArray;
			if (descriptors == null || descriptors.Count != Assemblies.Length)
				throw new InvalidDataException("Einbaugruppen fehlen im Katalog.");
			foreach (var assembly in Assemblies)
			{
				var matches = descriptors.Where(d => d["options"]?["chassis"]?.First?.Value<string>() == assembly.Hull).ToArray();
				if (matches.Length != 1 || (matches[0].Value<string>("faction") ?? "gdi") != assembly.Faction || Roles.Any(r => matches[0]["options"][r] is not JArray choices ||
					!choices.Values<string>().SequenceEqual(assembly.Choices[r])) ||
					matches[0]["built_in"] is not JArray builtIn || !builtIn.Values<string>().SequenceEqual(assembly.BuiltIn))
					throw new InvalidDataException("Einbaugruppen passen nicht zu den Grafik- und Trait-Bindungen.");
			}
			if (catalog["components"]["light"].Value<string>("armor_type") != "Light" ||
				catalog["components"]["heavy"].Value<string>("armor_type") != "Heavy" ||
				catalog["components"]["medium-cannon-mount"].Value<int>("burst") != 1)
				throw new InvalidDataException("Panzerungs- oder Waffenbindung nicht unterstuetzt.");
			var gearBindings = new Dictionary<string, (string Kind, string Locomotor)>
			{
				{ "tracks-standard", ("tracks", "tracked") }, { "prototype-hover", ("hover", "hover") },
				{ "gdi-stationary", ("stationary", "wheeled") }, { "wheels-light", ("wheels", "wheeled") },
				{ "light-tracks", ("tracks", "wheeled") }, { "walker-heavy", ("walker", "sheavytracked") },
				{ "designer-heavy-tracks", ("tracks", "heavytracked") }, { "designer-mk2-legs", ("walker", "heavytracked") },
				{ "tracks-light-artillery", ("tracks", "lighttracked") },
				{ "tracks-superheavy", ("tracks", "sheavytracked") },
				{ "designer-flak-gear", ("wheels", "wheeled") },
				{ "heavy-wheels", ("wheels", "heavywheeled") }, { "amphibious-micro", ("wheels", "seal") },
				{ "gdi-light-hover", ("hover", "lighthover") }, { "scrin-walker-gear", ("walker", "wheeled") }, { "scrin-hover-gear", ("hover", "lighthover") }
			};
			foreach (var binding in gearBindings)
			{
				var gear = (JObject)catalog["components"][binding.Key];
				if (gear == null || gear.Value<string>("kind") != binding.Value.Kind || gear.Value<string>("locomotor") != binding.Value.Locomotor)
					throw new InvalidDataException("Fahrwerksbindung nicht unterstuetzt.");
			}
		}

		public string[] Options(string role) => catalog["options"][role].Values<string>().ToArray();
		JObject Part(CustomFactionProfile p, string role) => (JObject)catalog["components"][p.Parts[role]];
		static double N(JObject p, string field)
		{
			var value = p.Value<double?>(field) ?? 0;
			if (double.IsNaN(value) || double.IsInfinity(value))
				throw new InvalidDataException("Nicht-endlicher Bausteinwert.");
			return value;
		}
		public string Label(string id) => catalog["components"][id].Value<string>("display_name") ?? id.Replace('-', ' ');
		static bool ValidName(string s) => s != null && Regex.IsMatch(s, @"\A[\p{L}\p{N} _-]{1,32}\z");
		static string I(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);

		public CustomTankValues Calculate(CustomFactionProfile profile)
		{
			if (profile == null || profile.Schema != 1 || !SupportedBase(profile.BaseFaction) || !ValidName(profile.Name) ||
				!ValidName(profile.TankName) || string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.TankName))
				throw new InvalidDataException("Unterstuetzte Basisfraktion erforderlich; Namen: 1-32 Buchstaben, Zahlen, Leerzeichen, _ oder -.");
			if (profile.Parts == null || profile.Parts.Count != Roles.Length || Roles.Any(r => !profile.Parts.ContainsKey(r)))
				throw new InvalidDataException("Jede Bausteinrolle muss genau einmal belegt sein.");
			foreach (var role in Roles)
			{
				if (!Options(role).Contains(profile.Parts[role]))
					throw new InvalidDataException("Nicht unterstuetzter Baustein: " + role);
				var part = Part(profile, role);
				if (part.Value<string>("role") != role || !part["factions"].Values<string>().Contains(ComponentFaction(profile.BaseFaction)) || N(part, "cp") != 0)
					throw new InvalidDataException("Fraktion, Rolle oder CP-Bindung nicht unterstuetzt.");
				if (N(part, "tier") > 3 || N(part, "tier") % 1 != 0 || N(part, "tech") < 1 || N(part, "tech") > 3 || N(part, "tech") % 1 != 0)
					throw new InvalidDataException("Ungueltige Katalog- oder Techstufe.");
				foreach (var key in new[] { "cost", "mass", "electric_kw", "tier", "tech" })
					if (double.IsNaN(N(part, key)) || double.IsInfinity(N(part, key)) || N(part, key) < 0)
						throw new InvalidDataException("Ungueltiger Bausteinwert.");
			}

			var assembly = Assembly(profile);
			if (assembly.Faction != ComponentFaction(profile.BaseFaction))
				throw new InvalidDataException("Einbaugruppe gehoert nicht zur Basisfraktion.");
			if ((Part(profile, "weapon").Value<bool?>("template_locked") ?? false) != assembly.PreserveWeaponTemplates)
				throw new InvalidDataException("Waffenpaket passt nicht zur vollstaendigen Trait-Bindung.");
			if (Roles.Any(r => !assembly.Choices[r].Contains(profile.Parts[r])))
				throw new InvalidDataException("Rumpf, Grafik, Fahrwerk und Waffengruppe sind nicht kompatibel.");
			if (Part(profile, "carrier")["module_family"] != null)
			{
				var recipe = catalog["turret_modules"]?[profile.Parts["carrier"]];
				if (recipe?.Value<string>("source_actor") != assembly.Actor || recipe.Value<int?>("slots_required") != 1 ||
					N(Part(profile, "carrier"), "slots_required") != 1 ||
					recipe.Value<string>("family") != Part(profile, "carrier").Value<string>("module_family") ||
					recipe["native_hulls"] is not JArray nativeHulls || !nativeHulls.Values<string>().Contains(assembly.Hull) ||
					recipe.Value<bool?>("arbitrary_mounting") != false)
					throw new InvalidDataException("Turmmodul passt nicht zur Originalwaffen-/Grafikbindung.");
			}
			var builtIn = assembly.BuiltIn.Select(id => catalog["components"][id] as JObject).ToArray();
			if (builtIn.Any(p => p == null || p.Value<string>("role") != "equipment" || N(p, "cp") != 0 ||
				!p["factions"].Values<string>().Contains(ComponentFaction(profile.BaseFaction)) || N(p, "cost") < 0 || N(p, "mass") < 0 || N(p, "electric_kw") < 0 ||
				N(p, "tier") < 0 || N(p, "tier") > 3 || N(p, "tier") % 1 != 0 || N(p, "tech") < 1 || N(p, "tech") > 3 || N(p, "tech") % 1 != 0))
				throw new InvalidDataException("Ungueltige fest eingebaute Ausruestung.");
			var selected = Roles.Select(r => Part(profile, r)).Concat(builtIn).ToArray();
			var hull = Part(profile, "chassis");
			var gear = Part(profile, "running_gear");
			if (gear.Value<string>("compatibility_class") is not string gearClass ||
				hull["allowed_running_gear_classes"] is not JArray gearClasses || !gearClasses.Values<string>().Contains(gearClass))
				throw new InvalidDataException("Fahrwerksklasse passt nicht zum Chassis.");
			var capacity = N(hull, "carrier_slots");
			var usedSlots = Part(profile, "carrier")["slots_required"] == null ? 1 : N(Part(profile, "carrier"), "slots_required");
			if (capacity < 1 || capacity > 3 || capacity % 1 != 0 || usedSlots < 1 || usedSlots % 1 != 0 || usedSlots > capacity)
				throw new InvalidDataException("Turmplatzbedarf ueberschreitet die Chassiskapazitaet oder ist ungueltig.");
			if (Part(profile, "carrier")["compatible_chassis_classes"] is JArray mountClasses &&
				!mountClasses.Values<string>().Contains(hull.Value<string>("chassis_class")))
				throw new InvalidDataException("Aufbaumodul passt nicht zur Chassisklasse.");
			var armor = Part(profile, "armor");
			var generator = Part(profile, "generator");
			var stationary = profile.Parts["running_gear"] == "gdi-stationary";
			if (N(hull, "reference_mass") <= 0 || N(hull, "reference_kw") <= 0 ||
				(stationary ? N(gear, "max_speed") != 0 || N(gear, "turn_speed") != 0 : N(gear, "max_speed") <= 0 || N(gear, "turn_speed") <= 0))
				throw new InvalidDataException("Ungueltige Fahrwerksgrenzen.");
			var efficiency = N(generator, "efficiency");
			if (efficiency <= 0 || efficiency > 1)
				throw new InvalidDataException("Ungueltiger Generatorwirkungsgrad.");
			var other = selected.Where(p => p.Value<string>("role") != "chassis" && p.Value<string>("role") != "armor").ToArray();
			var v = new CustomTankValues
			{
				Mass = N(hull, "mass") * N(armor, "mass_percent") / 100 + other.Sum(p => N(p, "mass")),
				Cost = N(hull, "cost") * N(armor, "cost_percent") / 100 + other.Sum(p => N(p, "cost")),
				Hp = N(hull, "hp") * N(armor, "hp_percent") / 100,
				Electric = selected.Sum(p => N(p, "electric_kw")),
				Points = selected.Sum(p => (int)N(p, "tier")),
				Tech = selected.Max(p => (int)N(p, "tech")),
				Stationary = profile.Parts["running_gear"] == "gdi-stationary"
			};
			v.Reserve = N(Part(profile, "drive"), "mechanical_kw") - v.Electric / efficiency;
			if (v.Cost <= 0 || v.Hp <= 0 || v.Mass <= 0 || v.Mass > Math.Min(N(hull, "max_mass"), N(gear, "max_mass")) ||
				v.Electric > N(generator, "max_electric_kw") || v.Reserve < 0 || (!v.Stationary && v.Reserve == 0) || v.Points > 50)
				throw new InvalidDataException("Masse, Leistung, Preis oder Katalogbudget ungueltig.");
			var speed = v.Stationary ? 0 : Math.Min(Math.Min(N(hull, "max_speed"), N(gear, "max_speed")),
				N(hull, "reference_speed") * Math.Pow(v.Reserve / v.Mass / (N(hull, "reference_kw") / N(hull, "reference_mass")), catalog.Value<double>("alpha")));
			v.Speed = (int)Math.Round(speed, MidpointRounding.AwayFromZero);
			v.Turn = v.Stationary ? 0 : (int)Math.Min(N(hull, "turn_speed_limit"), N(gear, "turn_speed"));
			return v;
		}

		public string Serialize(CustomFactionProfile p)
		{
			Calculate(p);
			return new JObject
			{
				["Schema"] = p.Schema, ["Name"] = p.Name, ["TankName"] = p.TankName, ["BaseFaction"] = p.BaseFaction,
				["Parts"] = new JObject(p.Parts.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new JProperty(kv.Key, kv.Value)))
			}.ToString(Formatting.Indented).Replace("\r\n", "\n");
		}

		public CustomFactionProfile Deserialize(string json)
		{
			var p = JsonConvert.DeserializeObject<CustomFactionProfile>(json, JsonSettings);
			MigrateParts(p?.Parts);
			Calculate(p);
			return p;
		}

		static void MigrateParts(Dictionary<string, string> parts)
		{
			if (parts == null) return;
			if (parts.TryGetValue("running_gear", out var id))
				parts["running_gear"] = CustomVehicleAssembly.CanonicalGear(id);
			if (parts.TryGetValue("chassis", out var hull) && parts.TryGetValue("carrier", out var carrier))
				parts["carrier"] = CustomVehicleAssembly.CanonicalCarrier(hull, carrier);
		}

		public void Save(string path, CustomFactionProfile p)
		{
			SaveText(path, Serialize(p));
		}

		static void SaveText(string path, string text)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				File.WriteAllText(temp, text, new UTF8Encoding(false));
				if (File.Exists(path))
					File.Copy(path, path + ".bak", true);
				File.Move(temp, path, true);
			}
			finally
			{
				if (File.Exists(temp))
					File.Delete(temp);
			}
		}

		public string Rules(CustomFactionProfile p)
		{
			return WorldRules(p.Name, p.BaseFaction) + ActorRules(p, "modular.custom", 0);
		}

		static string WorldRules(string name, string baseFaction) => "World:\n\t" + Bases[baseFaction].WorldTrait +
			":\n\t\tName: " + name + " (" + ComponentFaction(baseFaction).ToUpperInvariant() + ")\n";

		string ActorRules(CustomFactionProfile p, string actor, int index)
		{
			var v = Calculate(p);
			var assembly = Assembly(p);
			var s = new StringBuilder(actor + ":\n\tInherits: " + assembly.Actor + "\n");
			if (p.Parts["running_gear"] == "prototype-hover" && !assembly.NativeHover)
				s.Append("\tInherits@MODULARHOVER: ^HoverVehicle\n");
			if (v.Stationary)
				s.Append("\t-Buildable:\n");
			else
				s.Append("\tBuildable:\n\t\tPrerequisites: " + (assembly.StockPrerequisites != null ? assembly.StockPrerequisites + ", ~structures." + p.BaseFaction :
					(assembly.Actor == "HMMV" ? "weap.td, " : "") + "vehicles, ~structures." + p.BaseFaction + (v.Tech > 1 ? ", tier" + v.Tech : "") + assembly.ExtraPrerequisites) +
					"\n\t\tBuildPaletteOrder: " + (1000 + index) + "\n\t\tDescription: Eigener Entwurf - vor dem Spiel eingefroren.\n");
			if (!assembly.PreserveActorGraphics)
				s.Append("\tRenderSprites:\n\t\tImage: " + assembly.Image + "\n");
			s.Append("\tTooltip:\n\t\tName: " + p.TankName + "\n");
			foreach (var tooltip in assembly.AlternateTooltips)
				s.Append("\t" + tooltip + ":\n\t\tName: " + p.TankName + "\n");
			if (assembly.VoxelImage != null) s.Append("\tRenderVoxels:\n\t\tImage: " + assembly.VoxelImage + "\n");
			s.Append("\t-TooltipExtras:\n"); // Stock anti-tank strengths would be misleading for the HE loadout.
			s.Append("\tValued:\n\t\tCost: " + I(v.Cost) + "\n\tHealth:\n\t\tHP: " + I(v.Hp) + "\n");
			s.Append("\tArmor:\n\t\tType: " + Part(p, "armor").Value<string>("armor_type") + "\n\tMobile:\n\t\tLocomotor: " + Part(p, "running_gear").Value<string>("locomotor") + "\n");
			s.Append("\t\tSpeed: " + I(v.Speed) + "\n\t\tTurnSpeed: " + I(v.Turn) + "\n");
			if (v.Stationary)
				s.Append("\t\tImmovableCondition: modular-stationary\n\t\tPauseOnCondition: being-captured || empdisable || being-warped || driver-dead || notmobile || modular-stationary\n" +
					"\tGrantCondition@MODULARSTATIONARY:\n\t\tCondition: modular-stationary\n\t-ChronoshiftableWithSpriteEffect:\n\t-TeleportNetworkTransportable:\n");
			if (assembly.Turret != null && !assembly.PreserveWeaponTemplates)
				s.Append("\t" + assembly.Turret + ":\n\t\tTurnSpeed: " + I(N(Part(p, "carrier"), "turn_speed_reference")) + "\n");
			var slot = 0;
			foreach (var armament in assembly.Armaments)
			{
				s.Append("\t" + armament.Key + ":\n\t\tWeapon: " + BoundWeaponId(actor, slot++, assembly) + "\n");
				if (!assembly.PreserveWeaponTemplates && Part(p, "carrier")["fire_delay_ticks"] != null)
				{
					var delay = N(Part(p, "carrier"), "fire_delay_ticks");
					if (delay < 0 || delay % 1 != 0) throw new InvalidDataException("Ungueltige Feuerverzoegerung.");
					s.Append("\t\tFireDelay: " + I(delay) + "\n");
				}
			}
			if (!assembly.PreserveWeaponTemplates) s.Append("\tCarryable:\n");
			return s.ToString();
		}

		static string BoundWeaponId(string actor, int slot, CustomVehicleAssembly assembly) =>
			actor + (assembly.PreserveWeaponTemplates ? ".w" + I(slot) : slot == 0 ? ".gun" : ".aa");

		public string Weapons(CustomFactionProfile p)
		{
			return ActorWeapons(p, "modular.custom");
		}

		string ActorWeapons(CustomFactionProfile p, string actor)
		{
			Calculate(p);
			var assembly = Assembly(p);
			if (assembly.PreserveWeaponTemplates)
				return string.Concat(assembly.Armaments.Select((binding, i) => BoundWeaponId(actor, i, assembly) +
					":\n\tInherits: " + binding.Value + "\n"));
			var carrier = Part(p, "carrier"); var weapon = Part(p, "weapon"); var ammo = Part(p, "ammunition");
			var text = new StringBuilder();
			var slot = 0;
			foreach (var binding in assembly.Armaments)
			{
				var secondary = slot++ != 0;
				var reload = N(carrier, secondary ? "secondary_reload_ticks" : "reload_ticks");
				var range = N(weapon, secondary ? "secondary_range_cells" : "range_cells");
				var damage = N(ammo, secondary ? "secondary_damage" : "damage");
				var burst = N(carrier, "burst");
				if (reload < 1 || reload % 1 != 0 || burst < 1 || burst % 1 != 0 || damage < 0 || range <= 0)
					throw new InvalidDataException("Ungueltige Waffenwerte.");
				var parent = p.Parts["ammunition"] == "designer-he-shell" ? "120mmHEAT" : binding.Value;
				text.Append(actor + (secondary ? ".aa" : ".gun") + ":\n\tInherits: " + parent + "\n\tReloadDelay: " + I(reload) +
					"\n\tBurst: " + I(burst) + "\n\tRange: " + Distance(range) + "\n");
				if (carrier["burst_delay_ticks"] != null)
				{
					var delay = N(carrier, "burst_delay_ticks");
					if (delay < 0 || delay % 1 != 0) throw new InvalidDataException("Ungueltiger Salvenabstand.");
					text.Append("\tBurstDelays: " + I(delay) + "\n");
				}
				if (!secondary && weapon["min_range_cells"] != null)
				{
					var minimum = N(weapon, "min_range_cells");
					if (minimum > range) throw new InvalidDataException("Mindestreichweite groesser als Reichweite.");
					text.Append("\tMinRange: " + Distance(minimum) + "\n");
				}
				text.Append("\tWarhead@1Dam: SpreadDamage\n\t\tDamage: " + I(damage) + "\n");
			}
			return text.ToString();
		}

		static string Distance(double cells)
		{
			if (cells < 0 || cells > 1024) throw new InvalidDataException("Ungueltige Waffenreichweite.");
			var value = (int)Math.Round(cells * 1024, MidpointRounding.AwayFromZero);
			return I(value / 1024) + "c" + I(value % 1024);
		}

		// Base is the known lab package, not an arbitrary map with unmerged custom rules.
		public byte[] CompileMap(CustomFactionProfile input, byte[] lab)
		{
			var frozen = Serialize(input);
			var p = Deserialize(frozen);
			return PackageMap(p.Name, p.BaseFaction, frozen, Rules(p), Weapons(p), new[] { "modular.custom" }, lab);
		}

		static byte[] PackageMap(string factionName, string baseFaction, string frozen, string rules, string weapons, IEnumerable<string> actors, byte[] lab)
		{
			using var source = new ZipArchive(new MemoryStream(lab), ZipArchiveMode.Read);
			string Read(string name)
			{
				using var reader = new StreamReader(source.GetEntry(name).Open());
				return reader.ReadToEnd();
			}

			var map = Read("map.yaml");
			map = Regex.Replace(map, @"(?m)^\tPlayerReference@Multi[01]:\r?\n(?:\t\t[^\r\n]+\r?\n)*",
				m => Regex.Replace(m.Value, @"(?m)^(\t\tFaction: ).*$", "$1" + baseFaction));
			if (baseFaction != "eagle")
			{
				map = Regex.Replace(map, @"(?m)^\tTransport: [^\r\n]+\r?\n(?:\t\t[^\r\n]+\r?\n)*", "");
				map = Regex.Replace(map, @"(?m)^\tReference: mtnk\r?$", "\tReference: " + Bases[baseFaction].ReferenceActor);
			}
			map = Regex.Replace(map, @"(?m)^Title: .*$", "Title: Custom Faction - " + factionName);
			map = Regex.Replace(map, @"(?m)^\tPrototype\d+: modular\.[^\r\n]+\r?\n(?:\t\t[^\r\n]+\r?\n)*", "");
			var index = 0;
			foreach (var actor in actors)
			{
				map += "\tCustomTank" + (index == 0 ? "" : index.ToString(CultureInfo.InvariantCulture)) + ": " + actor +
					"\n\t\tOwner: Multi0\n\t\tLocation: " + I(103 + index % 4 * 3) + "," + I(-29 - index / 4 * 3) + "\n\t\tFacing: 384\n";
				index++;
			}
			var entries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
			{
				{ "map.yaml", Encoding.UTF8.GetBytes(map) }, { "modular-rules.yaml", Encoding.UTF8.GetBytes(rules) },
				{ "modular-weapons.yaml", Encoding.UTF8.GetBytes(weapons) }, { "custom-faction.json", Encoding.UTF8.GetBytes(frozen) }
			};
			foreach (var name in new[] { "map.bin", "map.png" })
			{
				using var stream = source.GetEntry(name).Open();
				using var memory = new MemoryStream();
				stream.CopyTo(memory);
				entries.Add(name, memory.ToArray());
			}

			using var output = new MemoryStream();
			using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
				foreach (var kv in entries)
				{
					var entry = zip.CreateEntry(kv.Key, CompressionLevel.Optimal);
					entry.LastWriteTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
					using var stream = entry.Open();
					stream.Write(kv.Value, 0, kv.Value.Length);
				}

			return output.ToArray();
		}

		public static string ContentHash(byte[] bytes)
		{
			using var sha = SHA256.Create();
			return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
		}
	}
}
