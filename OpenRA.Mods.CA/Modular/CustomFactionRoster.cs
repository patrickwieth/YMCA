using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenRA.Mods.CA.Modular
{
	public sealed class CustomVehicleDesign
	{
		public string Id { get; set; } = "tank1";
		public string Name { get; set; } = "Custom Battle Tank";
		public Dictionary<string, string> Parts { get; set; } = new CustomFactionProfile().Parts;
	}

	public sealed class CustomFactionRoster
	{
		public int Schema { get; set; } = 2;
		public string Name { get; set; } = "Meine GDI";
		public string BaseFaction { get; set; } = "eagle";
		public List<CustomVehicleDesign> Designs { get; set; } = new List<CustomVehicleDesign> { new CustomVehicleDesign() };
	}

	public sealed partial class CustomFactionDesign
	{
		public const int MaxDesigns = 16;

		public CustomFactionProfile Profile(CustomFactionRoster roster, CustomVehicleDesign design) => new CustomFactionProfile
		{
			Name = roster.Name, BaseFaction = roster.BaseFaction, TankName = design.Name, Parts = design.Parts
		};

		public int ValidateRoster(CustomFactionRoster roster)
		{
			if (roster == null || roster.Schema != 2 || !SupportedBase(roster.BaseFaction) || !ValidName(roster.Name) ||
				string.IsNullOrWhiteSpace(roster.Name) || roster.Designs == null || roster.Designs.Count < 1 || roster.Designs.Count > MaxDesigns)
				throw new InvalidDataException("Fraktion braucht einen gueltigen Namen und 1-16 passende Entwuerfe.");
			if (roster.Designs.Any(d => d == null || d.Id == null || !Regex.IsMatch(d.Id, @"\A[a-z][a-z0-9]{0,32}\z")))
				throw new InvalidDataException("Ungueltige interne Entwurfs-ID.");
			if (roster.Designs.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != roster.Designs.Count ||
				roster.Designs.Select(d => d.Name?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != roster.Designs.Count)
				throw new InvalidDataException("Fahrzeugnamen und IDs muessen innerhalb der Fraktion eindeutig sein.");
			var points = roster.Designs.Sum(d => Calculate(Profile(roster, d)).Points);
			if (points > 50) throw new InvalidDataException("Gemeinsames Katalogbudget von 50 ueberschritten.");
			return points;
		}

		public CustomVehicleDesign AddDesign(CustomFactionRoster roster, CustomVehicleDesign copy = null)
		{
			ValidateRoster(roster);
			if (roster.Designs.Count == MaxDesigns) throw new InvalidDataException("Maximal 16 Entwuerfe pro Fraktion.");
			var seed = copy == null ? "Fahrzeug" : copy.Name.Substring(0, Math.Min(copy.Name.Length, 23)) + " Kopie";
			var name = seed;
			for (var i = 2; roster.Designs.Any(d => string.Equals(d.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)); i++)
				name = seed + " " + i;
			var design = new CustomVehicleDesign { Id = "v" + Guid.NewGuid().ToString("N"), Name = name };
			design.Parts = copy == null ? DefaultParts(roster.BaseFaction) : new Dictionary<string, string>(copy.Parts);
			roster.Designs.Add(design);
			try { ValidateRoster(roster); }
			catch { roster.Designs.Remove(design); throw; }
			return design;
		}

		// Fill available slots without rolling back successful additions when the catalog exceeds roster limits.
		public int AddTemplates(CustomFactionRoster roster)
		{
			ValidateRoster(roster);
			var hulls = CompatibleOptions(Profile(roster, roster.Designs[0]), "chassis");
			foreach (var hull in hulls.Where(h => !roster.Designs.Any(d => d.Parts["chassis"] == h)))
			{
				if (roster.Designs.Count >= MaxDesigns) break;
				var name = Regex.Replace("Eigen " + Label(hull).Split(new[] { " - " }, StringSplitOptions.None)[0], @"[^\p{L}\p{N} _-]", "-").Trim();
				name = name.Substring(0, Math.Min(27, name.Length));
				var unique = name;
				for (var i = 2; roster.Designs.Any(d => string.Equals(d.Name.Trim(), unique, StringComparison.OrdinalIgnoreCase)); i++) unique = name + " " + i;
				var design = new CustomVehicleDesign { Id = "v" + Guid.NewGuid().ToString("N"), Name = unique, Parts = DefaultParts(roster.BaseFaction) };
				SelectPart(Profile(roster, design), "chassis", hull);
				roster.Designs.Add(design);
				try { ValidateRoster(roster); }
				catch (InvalidDataException) { roster.Designs.Remove(design); }
			}
			return hulls.Count(h => !roster.Designs.Any(d => d.Parts["chassis"] == h));
		}

		public string SerializeRoster(CustomFactionRoster roster)
		{
			ValidateRoster(roster);
			return new JObject
			{
				["Schema"] = 2, ["Name"] = roster.Name, ["BaseFaction"] = roster.BaseFaction,
				["Designs"] = new JArray(roster.Designs.Select(d => new JObject
				{
					["Id"] = d.Id, ["Name"] = d.Name,
					["Parts"] = new JObject(d.Parts.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new JProperty(kv.Key, kv.Value)))
				}))
			}.ToString(Formatting.Indented).Replace("\r\n", "\n");
		}

		public CustomFactionRoster DeserializeRoster(string json)
		{
			var root = JObject.Parse(json);
			if (root.Value<int?>("Schema") == 1)
			{
				var old = Deserialize(json);
				return new CustomFactionRoster
				{
					Name = old.Name, BaseFaction = old.BaseFaction,
					Designs = new List<CustomVehicleDesign> { new CustomVehicleDesign { Id = "tank1", Name = old.TankName, Parts = old.Parts } }
				};
			}
			if (root.Value<int?>("Schema") != 2 || root["Designs"] is not JArray designs || root["Name"] == null || root["BaseFaction"] == null)
				throw new InvalidDataException("Unbekanntes oder unvollstaendiges Fraktionsformat.");
			if (designs.Any(d => d is not JObject entry || entry["Id"] == null || entry["Name"] == null || entry["Parts"] is not JObject))
				throw new InvalidDataException("Jeder gespeicherte Entwurf braucht ID, Namen und Bausteine.");
			var roster = JsonConvert.DeserializeObject<CustomFactionRoster>(json, JsonSettings);
			if (roster?.Designs != null)
				foreach (var design in roster.Designs) MigrateParts(design?.Parts);
			ValidateRoster(roster);
			return roster;
		}

		public void SaveRoster(string path, CustomFactionRoster roster) => SaveText(path, SerializeRoster(roster));

		static string LibraryFileName(string directory, string name) => Path.Combine(directory,
			"faction-" + ContentHash(Encoding.UTF8.GetBytes(name.Trim().ToUpperInvariant())) + ".json");

		// Named library entries use hashed names, never user-supplied file paths. Renaming creates a new entry.
		public void SaveLibrary(string directory, CustomFactionRoster roster)
		{
			var text = SerializeRoster(roster);
			SaveText(LibraryFileName(directory, roster.Name), text);
		}

		public byte[] CompileRosterMap(CustomFactionRoster input, byte[] lab)
		{
			var frozen = SerializeRoster(input);
			var roster = DeserializeRoster(frozen);
			var rules = new StringBuilder(WorldRules(roster.Name, roster.BaseFaction));
			var weapons = new StringBuilder();
			var actors = new List<string>();
			foreach (var design in roster.Designs)
			{
				var id = "modular.custom." + design.Id;
				var profile = Profile(roster, design);
				rules.Append(ActorRules(profile, id, actors.Count));
				weapons.Append(ActorWeapons(profile, id));
				actors.Add(id);
			}
			return PackageMap(roster.Name, roster.BaseFaction, frozen, rules.ToString(), weapons.ToString(), actors, lab);
		}
	}
}
