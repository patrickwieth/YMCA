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
		public string Faction { get; init; } = "gdi";
		public bool PreserveWeaponTemplates { get; init; }
		public bool NativeHover { get; init; }
		public string VoxelImage { get; init; }
		public string ExtraPrerequisites { get; init; } = "";
		public string Summary { get; init; }

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
				new Dictionary<string, string> { { "Armament@PRIMARY", "227mm" }, { "Armament@SECONDARY", "227mmAA" } }),
			new CustomVehicleAssembly("juggernaut", "Juggernaut", "juggernaut", null,
				new[] { "walker-heavy" }, new[] { "diesel" }, "heavy", "fixed-triple", "artillery", new[] { "artillery-shell" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "JuggernautGun" }, { "Armament@SECONDARY", "JuggernautDummyAim" } })
			{
				PreserveWeaponTemplates = true,
				Summary = "Juggernaut: Artillerie + Zielhilfswaffe; Schreit-, Ziel- und Schussanimationen bleiben erhalten."
			},
			new CustomVehicleAssembly("designer-mammoth-hull", "Mammoth", "mammoth", "Turreted@PRIMARY",
				new[] { "designer-heavy-tracks" }, new[] { "diesel-heavy" }, "heavy",
				"designer-mammoth-mount", "designer-mammoth-weapons", new[] { "designer-mammoth-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "130mmTD" }, { "Armament@SECONDARY", "MammothTusk" } })
			{
				PreserveWeaponTemplates = true,
				Summary = "Mammut: Doppelkanone, Raketen gegen Infanterie/Luft, Regeneration und schwere Ketten."
			},
			new CustomVehicleAssembly("designer-hmlrs-hull", "hmlrs", "hmlrs", "Turreted",
				new[] { "prototype-hover" }, new[] { "diesel" }, "light",
				"designer-hmlrs-mount", "designer-hmlrs-weapons", new[] { "designer-hmlrs-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "227mmH" }, { "Armament@SECONDARY", "227mmAAH" },
					{ "Armament@PRIMARY-UPG2", "227mm.upg" }, { "Armament@SECONDARY-UPG2", "227mmAA.upg" } })
			{
				PreserveWeaponTemplates = true, NativeHover = true, ExtraPrerequisites = ", ~promotion.hover_mlrs",
				Summary = "Echter Hover-MLRS: Boden/AA + beide Upgrade-Kanaele. Benoetigt Hover-MLRS-Freischaltung."
			},
			new CustomVehicleAssembly("designer-disruptor-hull", "DISR", "disr", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy",
				"designer-disruptor-mount", "designer-disruptor-weapons", new[] { "designer-disruptor-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "SonicZap" }, { "Armament@SECONDARY", "SonicZapVisual" },
					{ "Armament@PRIMARYUPG", "SonicZap.UPG" }, { "Armament@SECONDARYUPG", "SonicZapVisual.UPG" } })
			{
				PreserveWeaponTemplates = true,
				Summary = "Disruptor: Flaechen-Schallstrahl, visueller Begleitstrahl und bedingte Upgrades; kein Granatenersatz."
			},
			new CustomVehicleAssembly("designer-mk2-hull", "MAMMOTHMK2", "mammothmk2", null,
				new[] { "designer-mk2-legs" }, new[] { "diesel-heavy" }, "heavy",
				"designer-mk2-mount", "designer-mk2-weapons", new[] { "designer-mk2-payload" },
				new Dictionary<string, string> { { "Armament@RAILGUN", "Railgun.MKII" }, { "Armament@MISSILES", "Dragon.MKII" },
					{ "Armament@AAMISSILES", "RedEye.MKII" } })
			{
				PreserveWeaponTemplates = true, VoxelImage = "mammothmk2",
				ExtraPrerequisites = ", ~promotion.mammoth_mkii, miss.gdi",
				Summary = "Mk II: Railgun + Bodenraketen + AA; regenerierender Walker. Originalfreischaltung erforderlich."
			},
			new CustomVehicleAssembly("nod-light-hull", "LTNK", "ltnk", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "light-cannon-mount", "light-cannon", new[] { "light-tank-shell" },
				new Dictionary<string, string> { { "Armament", "30mm" } })
			{
				Faction = "nod", PreserveWeaponTemplates = true,
				Summary = "Nod-Leichtpanzer: vollstaendige 30-mm-Kanone, Rueckstoss, Fahrer- und Upgrade-Verhalten."
			},
			new CustomVehicleAssembly("buggy-hull", "BGGY", "buggy", "Turreted@PRIMARY",
				new[] { "wheels-light" }, new[] { "diesel-light" }, "light", "scout-mg-mount", "scout-mg", new[] { "scout-mg-rounds" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "M60mgTD" } }, "scout-sensors")
			{
				Faction = "nod", ExtraPrerequisites = ", ~!promotion.buggy.pdl, ~!promotion.buggy.aa"
			},
			new CustomVehicleAssembly("designer-nod-artillery-hull", "ARTY.nod", "artynod", null,
				new[] { "tracks-light-artillery" }, new[] { "diesel" }, "light", "field-artillery-mount", "field-artillery", new[] { "field-artillery-he" },
				new Dictionary<string, string> { { "Armament", "155mmTD" } })
			{
				Faction = "nod", PreserveWeaponTemplates = true,
				Summary = "Nod-Artillerie: 155mmTD, feste Frontwaffe, leichtes Kettenfahrprofil und Munitionsexplosion."
			},
			new CustomVehicleAssembly("designer-ssm-hull", "SSM", "ssm", "Turreted",
				new[] { "designer-ssm-gear" }, new[] { "diesel" }, "light", "designer-ssm-mount", "designer-ssm-weapons", new[] { "designer-ssm-payload" },
				new Dictionary<string, string> { { "Armament", "HonestJohn" } })
			{
				Faction = "nod", PreserveWeaponTemplates = true, ExtraPrerequisites = ", tmpl, ~promotion.ssm_launcher",
				Summary = "SSM: Napalmraketen, Munitionsvorrat, Nachladen und variable Raketengrafik. Freischaltung + Tempel erforderlich."
			}
		};

		CustomVehicleAssembly Assembly(CustomFactionProfile p) => Assemblies.Single(a => a.Hull == p.Parts["chassis"]);

		public string[] CompatibleOptions(CustomFactionProfile p, string role)
		{
			return role == "chassis" ? Assemblies.Where(a => a.Faction == ComponentFaction(p.BaseFaction))
				.Select(a => a.Hull).Where(Options(role).Contains).ToArray() : Assembly(p).Choices[role].Where(Options(role).Contains).ToArray();
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

		public bool IsNativeHover(CustomFactionProfile p) => Assembly(p).NativeHover;

		public string WeaponSummary(CustomFactionProfile p)
		{
			var a = Assembly(p);
			if (a.Summary != null) return a.Summary;
			var range = N(Part(p, "weapon"), "range_cells");
			var burst = N(Part(p, "carrier"), "burst");
			if (a.Actor == "HMMV" || a.Actor == "BGGY")
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
