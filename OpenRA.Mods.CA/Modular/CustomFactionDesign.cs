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
	public sealed class CustomFactionDesign
	{
		readonly JObject catalog;
		public static readonly string[] Roles = { "chassis", "running_gear", "drive", "generator", "armor", "carrier", "weapon", "ammunition" };
		static readonly string[][] Bindings =
		{
			new[] { "gdi-battle-hull" }, new[] { "tracks-standard", "prototype-hover", "gdi-stationary" },
			new[] { "diesel", "diesel-large" }, new[] { "baseline-generator", "efficient-generator" }, new[] { "heavy" },
			new[] { "medium-cannon-mount" }, new[] { "medium-cannon" }, new[] { "medium-tank-shell", "designer-he-shell" }
		};
		static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Error,
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
			if (catalog["components"]["heavy"].Value<string>("armor_type") != "Heavy" ||
				catalog["components"]["medium-cannon-mount"].Value<int>("burst") != 1)
				throw new InvalidDataException("Panzerungs- oder Waffenbindung nicht unterstuetzt.");
			var kinds = new[] { "tracks", "hover", "stationary" };
			var locomotors = new[] { "tracked", "hover", "wheeled" };
			for (var i = 0; i < Bindings[1].Length; i++)
			{
				var gear = (JObject)catalog["components"][Bindings[1][i]];
				if (gear == null || gear.Value<string>("kind") != kinds[i] || gear.Value<string>("locomotor") != locomotors[i])
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
			if (profile == null || profile.Schema != 1 || profile.BaseFaction != "eagle" || !ValidName(profile.Name) ||
				!ValidName(profile.TankName) || string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrWhiteSpace(profile.TankName))
				throw new InvalidDataException("Nur GDI/Eagle; Namen: 1-32 Buchstaben, Zahlen, Leerzeichen, _ oder -.");
			if (profile.Parts == null || profile.Parts.Count != Roles.Length || Roles.Any(r => !profile.Parts.ContainsKey(r)))
				throw new InvalidDataException("Jede Bausteinrolle muss genau einmal belegt sein.");
			foreach (var role in Roles)
			{
				if (!Options(role).Contains(profile.Parts[role]))
					throw new InvalidDataException("Nicht unterstuetzter Baustein: " + role);
				var part = Part(profile, role);
				if (part.Value<string>("role") != role || !part["factions"].Values<string>().Contains("gdi") || N(part, "cp") != 0)
					throw new InvalidDataException("Fraktion, Rolle oder CP-Bindung nicht unterstuetzt.");
				if (N(part, "tier") > 3 || N(part, "tier") % 1 != 0 || N(part, "tech") < 1 || N(part, "tech") > 3 || N(part, "tech") % 1 != 0)
					throw new InvalidDataException("Ungueltige Katalog- oder Techstufe.");
				foreach (var key in new[] { "cost", "mass", "electric_kw", "tier", "tech" })
					if (double.IsNaN(N(part, key)) || double.IsInfinity(N(part, key)) || N(part, key) < 0)
						throw new InvalidDataException("Ungueltiger Bausteinwert.");
			}

			var hull = Part(profile, "chassis");
			var gear = Part(profile, "running_gear");
			var armor = Part(profile, "armor");
			var generator = Part(profile, "generator");
			var stationary = profile.Parts["running_gear"] == "gdi-stationary";
			if (N(hull, "reference_mass") <= 0 || N(hull, "reference_kw") <= 0 ||
				(stationary ? N(gear, "max_speed") != 0 || N(gear, "turn_speed") != 0 : N(gear, "max_speed") <= 0 || N(gear, "turn_speed") <= 0))
				throw new InvalidDataException("Ungueltige Fahrwerksgrenzen.");
			var efficiency = N(generator, "efficiency");
			if (efficiency <= 0 || efficiency > 1)
				throw new InvalidDataException("Ungueltiger Generatorwirkungsgrad.");
			var other = Roles.Where(r => r != "chassis" && r != "armor").Select(r => Part(profile, r)).ToArray();
			var v = new CustomTankValues
			{
				Mass = N(hull, "mass") * N(armor, "mass_percent") / 100 + other.Sum(p => N(p, "mass")),
				Cost = N(hull, "cost") * N(armor, "cost_percent") / 100 + other.Sum(p => N(p, "cost")),
				Hp = N(hull, "hp") * N(armor, "hp_percent") / 100,
				Electric = Roles.Sum(r => N(Part(profile, r), "electric_kw")),
				Points = Roles.Sum(r => (int)N(Part(profile, r), "tier")),
				Tech = Roles.Max(r => (int)N(Part(profile, r), "tech")),
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
			Calculate(p);
			return p;
		}

		public void Save(string path, CustomFactionProfile p)
		{
			var text = Serialize(p);
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
			var v = Calculate(p);
			var s = new StringBuilder("World:\n\tFactionCA@11:\n\t\tName: " + p.Name + " (GDI)\n");
			s.Append("modular.custom:\n\tInherits: MTNK\n");
			if (p.Parts["running_gear"] == "prototype-hover")
				s.Append("\tInherits@MODULARHOVER: ^HoverVehicle\n");
			if (v.Stationary)
				s.Append("\t-Buildable:\n");
			else
				s.Append("\tBuildable:\n\t\tPrerequisites: vehicles, ~structures.eagle" + (v.Tech > 1 ? ", tier" + v.Tech : "") +
					"\n\t\tBuildPaletteOrder: 81\n\t\tDescription: Custom modular vehicle. Frozen before this match.\n");
			s.Append("\tRenderSprites:\n\t\tImage: mtnk\n\tTooltip:\n\t\tName: " + p.TankName + "\n");
			s.Append("\t-TooltipExtras:\n"); // Stock anti-tank strengths would be misleading for the HE loadout.
			s.Append("\tValued:\n\t\tCost: " + I(v.Cost) + "\n\tHealth:\n\t\tHP: " + I(v.Hp) + "\n");
			s.Append("\tArmor:\n\t\tType: Heavy\n\tMobile:\n\t\tLocomotor: " + Part(p, "running_gear").Value<string>("locomotor") + "\n");
			s.Append("\t\tSpeed: " + I(v.Speed) + "\n\t\tTurnSpeed: " + I(v.Turn) + "\n");
			if (v.Stationary)
				s.Append("\t\tImmovableCondition: modular-stationary\n\t\tPauseOnCondition: being-captured || empdisable || being-warped || driver-dead || notmobile || modular-stationary\n" +
					"\tGrantCondition@MODULARSTATIONARY:\n\t\tCondition: modular-stationary\n\t-ChronoshiftableWithSpriteEffect:\n\t-TeleportNetworkTransportable:\n");
			s.Append("\tTurreted@PRIMARY:\n\t\tTurnSpeed: " + I(N(Part(p, "carrier"), "turn_speed_reference")) + "\n");
			s.Append("\tArmament@PRIMARY:\n\t\tWeapon: modular.custom.gun\n\tCarryable:\n");
			return s.ToString();
		}

		public string Weapons(CustomFactionProfile p)
		{
			Calculate(p);
			var range = (int)Math.Round(N(Part(p, "weapon"), "range_cells") * 1024, MidpointRounding.AwayFromZero);
			return "modular.custom.gun:\n\tInherits: " + (p.Parts["ammunition"] == "designer-he-shell" ? "120mmHEAT" : "120mm") +
				"\n\tReloadDelay: " + I(N(Part(p, "carrier"), "reload_ticks")) + "\n\tBurst: 1\n\tRange: " + range / 1024 + "c" + range % 1024 +
				"\n\tWarhead@1Dam: SpreadDamage\n\t\tDamage: " + I(N(Part(p, "ammunition"), "damage")) + "\n";
		}

		// Base is the known lab package, not an arbitrary map with unmerged custom rules.
		public byte[] CompileMap(CustomFactionProfile input, byte[] lab)
		{
			var frozen = Serialize(input);
			var p = Deserialize(frozen);
			using var source = new ZipArchive(new MemoryStream(lab), ZipArchiveMode.Read);
			string Read(string name)
			{
				using var reader = new StreamReader(source.GetEntry(name).Open());
				return reader.ReadToEnd();
			}

			var map = Read("map.yaml");
			map = Regex.Replace(map, @"(?m)^Title: .*$", "Title: Custom Faction - " + p.Name);
			map = Regex.Replace(map, @"(?m)^\tPrototype\d+: modular\.[^\r\n]+\r?\n(?:\t\t[^\r\n]+\r?\n)*", "");
			map += "\tCustomTank: modular.custom\n\t\tOwner: Multi0\n\t\tLocation: 103,-29\n\t\tFacing: 384\n";
			var entries = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
			{
				{ "map.yaml", Encoding.UTF8.GetBytes(map) }, { "modular-rules.yaml", Encoding.UTF8.GetBytes(Rules(p)) },
				{ "modular-weapons.yaml", Encoding.UTF8.GetBytes(Weapons(p)) }, { "custom-faction.json", Encoding.UTF8.GetBytes(frozen) }
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
