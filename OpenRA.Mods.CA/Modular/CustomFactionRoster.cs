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
			if (roster == null || roster.Schema != 2 || roster.BaseFaction != "eagle" || !ValidName(roster.Name) ||
				string.IsNullOrWhiteSpace(roster.Name) || roster.Designs == null || roster.Designs.Count < 1 || roster.Designs.Count > MaxDesigns)
				throw new InvalidDataException("Fraktion braucht einen gueltigen Namen und 1-16 GDI-Entwuerfe.");
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
			if (copy != null) design.Parts = new Dictionary<string, string>(copy.Parts);
			roster.Designs.Add(design);
			try { ValidateRoster(roster); }
			catch { roster.Designs.Remove(design); throw; }
			return design;
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
			ValidateRoster(roster);
			return roster;
		}

		public void SaveRoster(string path, CustomFactionRoster roster) => SaveText(path, SerializeRoster(roster));

		// Named library entries use hashed names, never user-supplied file paths. Renaming creates a new entry.
		public void SaveLibrary(string directory, CustomFactionRoster roster)
		{
			var text = SerializeRoster(roster);
			var id = ContentHash(Encoding.UTF8.GetBytes(roster.Name.Trim().ToUpperInvariant()));
			SaveText(Path.Combine(directory, "faction-" + id + ".json"), text);
		}

		public byte[] CompileRosterMap(CustomFactionRoster input, byte[] lab)
		{
			var frozen = SerializeRoster(input);
			var roster = DeserializeRoster(frozen);
			var rules = new StringBuilder(WorldRules(roster.Name));
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
			return PackageMap(roster.Name, frozen, rules.ToString(), weapons.ToString(), actors, lab);
		}
	}
}
