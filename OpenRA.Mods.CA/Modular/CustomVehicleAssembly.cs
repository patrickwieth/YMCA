using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Modular
{
	// Explicit, graphics-compatible assemblies. Adding data alone cannot bind arbitrary traits.
	public sealed class CustomVehicleAssembly
	{
		public readonly string Hull, Actor, Image, Turret;
		public readonly Dictionary<string, string[]> Choices;
		public readonly string[] BuiltIn;
		public readonly Dictionary<string, string> Armaments;

		public CustomVehicleAssembly(string hull, string actor, string image, string turret,
			string[] gear, string[] motors, string armor, string carrier, string weapon, string[] ammunition,
			Dictionary<string, string> armaments, params string[] builtIn)
		{
			Hull = hull; Actor = actor; Image = image; Turret = turret; Armaments = armaments; BuiltIn = builtIn;
			Choices = new Dictionary<string, string[]>
			{
				{ "chassis", new[] { hull } }, { "running_gear", gear }, { "drive", motors },
				{ "generator", new[] { "baseline-generator", "efficient-generator" } }, { "armor", new[] { armor } },
				{ "carrier", new[] { carrier } }, { "weapon", new[] { weapon } }, { "ammunition", ammunition }
			};
		}
	}

	public sealed partial class CustomFactionDesign
	{
		static readonly CustomVehicleAssembly[] Assemblies =
		{
			new CustomVehicleAssembly("gdi-battle-hull", "MTNK", "mtnk", "Turreted@PRIMARY",
				new[] { "tracks-standard", "prototype-hover", "gdi-stationary" }, new[] { "diesel", "diesel-large" },
				"heavy", "medium-cannon-mount", "medium-cannon", new[] { "medium-tank-shell", "designer-he-shell" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "120mm" } }),
			new CustomVehicleAssembly("humvee-hull", "HMMV", "hmmv", "Turreted",
				new[] { "wheels-light", "gdi-stationary" }, new[] { "diesel-light" },
				"light", "scout-mg-mount", "scout-mg", new[] { "scout-mg-rounds" },
				new Dictionary<string, string> { { "Armament", "M60mgTD" } }, "scout-sensors"),
			new CustomVehicleAssembly("designer-mlrs-hull", "MLRS", "mlrs", "Turreted",
				new[] { "designer-mlrs-gear" }, new[] { "diesel", "diesel-large" },
				"light", "designer-rocket-mount", "designer-rockets", new[] { "designer-rocket-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "227mm" }, { "Armament@SECONDARY", "227mmAA" } })
		};

		CustomVehicleAssembly Assembly(CustomFactionProfile p) => Assemblies.Single(a => a.Hull == p.Parts["chassis"]);

		public string[] CompatibleOptions(CustomFactionProfile p, string role)
		{
			return role == "chassis" ? Options(role) : Assembly(p).Choices[role].Where(Options(role).Contains).ToArray();
		}

		// Only explicit UI selection normalizes linked parts. Loading profiles never silently repairs invalid data.
		public void SelectPart(CustomFactionProfile p, string role, string id)
		{
			if (!CompatibleOptions(p, role).Contains(id))
				throw new ArgumentException("Nicht kompatibler Baustein.");
			p.Parts[role] = id;
			if (role == "chassis")
				foreach (var r in Roles.Where(r => r != "chassis"))
				{
					var allowed = CompatibleOptions(p, r);
					if (!allowed.Contains(p.Parts[r])) p.Parts[r] = allowed[0];
				}
		}

		public string WeaponSummary(CustomFactionProfile p)
		{
			var a = Assembly(p);
			var range = N(Part(p, "weapon"), "range_cells");
			var burst = N(Part(p, "carrier"), "burst");
			if (a.Actor == "HMMV")
			{
				var sensors = N((Newtonsoft.Json.Linq.JObject)catalog["components"]["scout-sensors"], "cost");
				return $"MG: Boden, Reichweite {range:0.##}, {burst:0}er-Salve. Sensoren enthalten (+{sensors:0} Credits).";
			}
			if (a.Actor == "MLRS")
			{
				var minimum = N(Part(p, "weapon"), "min_range_cells");
				var air = N(Part(p, "weapon"), "secondary_range_cells");
				return $"Raketen: Boden {minimum:0.##}-{range:0.##} / Luft {air:0.##}, je {burst:0}er-Salve; getrennte Waffen.";
			}
			return (p.Parts["ammunition"] == "designer-he-shell" ? "HE-Kanone" : "Panzerkanone") + $": Reichweite {range:0.##}, {burst:0} Schuss pro Salve.";
		}
	}
}
