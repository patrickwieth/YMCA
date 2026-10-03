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
		public bool PreserveActorGraphics { get; init; }
		public bool InheritStockWeapons { get; init; }
		public bool NativeHover { get; init; }
		public string VoxelImage { get; init; }
		public string ExtraPrerequisites { get; init; } = "";
		// Exact non-national stock gates; engineering component tiers must not add gameplay gates.
		public string StockPrerequisites { get; init; }
		public string Summary { get; init; }
		public string[] AlternateTooltips { get; init; } = Array.Empty<string>();

		public CustomVehicleAssembly(string hull, string actor, string image, string turret,
			string[] gear, string[] motors, string armor, string carrier, string weapon, string[] ammunition,
			Dictionary<string, string> armaments, params string[] builtIn)
		{
			Hull = hull; Actor = actor; Image = image; Turret = turret; Armaments = armaments; BuiltIn = builtIn;
			Choices = new Dictionary<string, string[]>
			{
				{ "chassis", new[] { hull } }, { "running_gear", gear.Select(CanonicalGear).ToArray() }, { "drive", motors },
				{ "generator", new[] { "baseline-generator", "efficient-generator" } }, { "armor", new[] { armor } },
				{ "carrier", new[] { carrier } }, { "weapon", new[] { weapon } }, { "ammunition", ammunition }
			};
		}

		public static string CanonicalGear(string id) => id == "designer-mlrs-gear" || id == "designer-ssm-gear" || id == "designer-prism-gear" ? "light-tracks" : id;
	}

	public sealed partial class CustomFactionDesign
	{
		static CustomVehicleAssembly ScrinAssembly(string key, string actor, string gear, string turret,
			Dictionary<string, string> weapons, string prerequisites, string summary)
		{
			var a = new CustomVehicleAssembly("scrin-" + key + "-hull", actor, actor.ToLowerInvariant(), turret,
				new[] { gear }, new[] { "scrin-drive" }, "light", "scrin-" + key + "-mount", "scrin-" + key + "-weapon",
				new[] { "scrin-" + key + "-payload" }, weapons)
			{
				Faction = "scrin", PreserveWeaponTemplates = true, NativeHover = gear == "scrin-hover-gear",
				ExtraPrerequisites = prerequisites, Summary = summary
			};
			a.Choices["generator"] = new[] { "scrin-converter", "scrin-converter-efficient" };
			return a;
		}

		static CustomVehicleAssembly NodCombatAssembly(string key, string actor, string image, string gear, string motor,
			string armor, string prerequisites, Dictionary<string, string> weapons, string summary) =>
			new CustomVehicleAssembly("nod-combat-" + key + "-hull", actor, image, null, new[] { gear }, new[] { motor },
				armor, "integrated-mount", "nod-combat-" + key + "-weapon", new[] { "integral-stores" }, weapons)
			{
				Faction = "nod", PreserveWeaponTemplates = true, StockPrerequisites = prerequisites, Summary = summary
			};

		static CustomVehicleAssembly ChinaCombatAssembly(string key, string actor, string image, string gear,
			string armor, string prerequisites, Dictionary<string, string> weapons, string summary) =>
			new CustomVehicleAssembly("china-combat-" + key + "-hull", actor, image, null, new[] { gear }, new[] { "diesel" },
				armor, "integrated-mount", "china-combat-" + key + "-weapon", new[] { "integral-stores" }, weapons)
			{
				Faction = "china", PreserveWeaponTemplates = true, StockPrerequisites = prerequisites, Summary = summary
			};

		static readonly CustomVehicleAssembly[] Assemblies = new CustomVehicleAssembly[]
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
			new CustomVehicleAssembly("designer-titan-hull", "TITN", "titn", "Turreted@PRIMARY",
				new[] { "walker-heavy" }, new[] { "diesel" }, "heavy", "designer-titan-mount", "designer-titan-weapon", new[] { "designer-titan-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "TitanGun" }, { "Armament@SECONDARY", "TitanTusk" } })
			{
				PreserveWeaponTemplates = true, ExtraPrerequisites = ", ~!upg.titan",
				Summary = "Titan: Sprite-Walker mit Kanone, Raketen, Regeneration und originalem Turm-/Laufverhalten."
			},
			new CustomVehicleAssembly("designer-slingshot-hull", "SLNG", "slng", "Turreted",
				new[] { "gdi-light-hover" }, new[] { "diesel-light" }, "light", "designer-slingshot-mount", "designer-slingshot-weapon", new[] { "designer-slingshot-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "SlingshotAA" } })
			{
				PreserveWeaponTemplates = true, NativeHover = true, ExtraPrerequisites = ", ~promotion.slingshot",
				Summary = "Slingshot: echte leichte Hover-Flak, nur Luftziele; Originalfreischaltung erforderlich."
			},
			new CustomVehicleAssembly("designer-marv-hull", "MARV", "marv", "Turreted",
				new[] { "tracks-superheavy" }, new[] { "diesel-heavy" }, "heavy", "designer-marv-mount", "designer-marv-weapon", new[] { "designer-marv-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "IonZap.Marv" } })
			{
				PreserveWeaponTemplates = true, ExtraPrerequisites = ", ~promotion.marv, miss.gdi",
				Summary = "MARV: Dreifach-Ionenwaffe, Regeneration und Ernte beim Ueberfahren; Originalfreischaltung erforderlich."
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
			},
			new CustomVehicleAssembly("battlemaster", "chbattle", "chbattle", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel", "diesel-large" }, "heavy", "cannon-turret", "cannon", new[] { "shell" },
				new Dictionary<string, string> { { "Armament", "CHBattlemasterCannon" } })
			{
				Faction = "china", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", chweap, ~vehicles.china, ~!vehicles.chinainf, ~!upg.battlemaster",
				Summary = "Battlemaster: Kanone, Horde und Nuklear-Upgrades geerbt. Anzeige zeigt Basiswerte ohne situative Boni."
			},
			new CustomVehicleAssembly("dragon-chassis", "chdragon", "chdragon", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel", "diesel-large" }, "heavy", "flame-turret", "dragon-flamer", new[] { "flame-fuel" },
				new Dictionary<string, string> { { "Armament", "CHDragonFlamer" }, { "Armament@Black_Napalm", "CHDragonFlamer.Black_Napalm" },
					{ "Armament@Firewall1", "CHDragonFirestorm" }, { "Armament@Firewall2", "CHDragonFirestorm2" } })
			{
				Faction = "china", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", ~vehicles.china, ~!promotion.dragon_tank.pdl, ~!promotion.dragon_tank.reflector",
				Summary = "Dragon: Flammenwerfer, Black Napalm und beide Firewall-Phasen; originales Deploy-/Bewegungsverhalten."
			},
			new CustomVehicleAssembly("gatling-chassis", "chgtnk", "chgtnk", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel", "diesel-large" }, "heavy", "gatling-turret", "gatling-gun", new[] { "gatling-rounds" },
				new Dictionary<string, string> { { "Armament@GAT0", "ChinaMGatt.0" }, { "Armament@GAT1", "ChinaMGatt.1" },
					{ "Armament@GAT2", "ChinaMGatt.2" }, { "Armament@GAT3", "ChinaMGatt.3" },
					{ "Armament@GAT0G", "ChinaMGatt.0G" }, { "Armament@GAT1G", "ChinaMGatt.1G" },
					{ "Armament@GAT2G", "ChinaMGatt.2G" }, { "Armament@GAT3G", "ChinaMGatt.3G" } })
			{
				Faction = "china", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", ~vehicles.china, ~!promotion.gatling.pdl, ~!promotion.gatling.reflector",
				Summary = "Gatling: je vier Boden-/Luft-Drehzahlstufen mit getrenntem Hochlauf, Zielwechsel und Abklingzeit."
			},
			new CustomVehicleAssembly("overlord-chassis", "choverlord", "choverlord", "Turreted",
				new[] { "tracks-superheavy" }, new[] { "diesel-heavy", "diesel-heavy-boost" }, "heavy", "heavy-twin-turret", "overlord-cannon", new[] { "heavy-shell" },
				new Dictionary<string, string> { { "Armament", "OverlordCannon" } })
			{
				Faction = "china", PreserveWeaponTemplates = true, AlternateTooltips = new[] { "Tooltip@Emperor" },
				ExtraPrerequisites = ", radar, ~vehicles.china, ~!vehicles.chinainf, ~!upg.overlord",
				Summary = "Overlord: Doppelkanone; Tank-General nutzt Emperor-Grafik und 80% erlittenen Schaden. HP-Anzeige ist roh."
			},
			new CustomVehicleAssembly("allied-medium-hull", "Challenger_Tank", "Challenger_Tank", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "medium-cannon-mount", "medium-cannon", new[] { "medium-tank-shell" },
				new Dictionary<string, string> { { "Armament", "90mm" } })
			{
				Faction = "allies", ExtraPrerequisites = ", ~england",
				Summary = "Challenger: volle 90-mm-Kanone und eigene Grafik. Angezeigter Basispreis vor Panzer-Doktrinrabatt."
			},
			new CustomVehicleAssembly("designer-ranger-hull", "JEEP", "jeep", "Turreted",
				new[] { "wheels-light" }, new[] { "diesel-light" }, "light", "designer-ranger-mount", "scout-mg", new[] { "scout-mg-rounds" },
				new Dictionary<string, string> { { "Armament", "M60mg" } }, "scout-sensors")
			{
				Faction = "allies",
				ExtraPrerequisites = ", weap, ~allies, ~!promotion.infantry_doctrine, ~!promotion.armored_doctrine, ~!promotion.airforce_doctrine",
				Summary = "Ranger: M60mg und Sensoren (50 Credits enthalten). Wie das Original nur vor der Doktrinwahl baubar."
			},
			new CustomVehicleAssembly("field-artillery-hull", "ARTY", "arty", null,
				new[] { "tracks-light-artillery" }, new[] { "diesel" }, "light", "field-artillery-mount", "field-artillery", new[] { "field-artillery-he" },
				new Dictionary<string, string> { { "Armament", "155mm" } })
			{
				Faction = "allies", PreserveWeaponTemplates = true, ExtraPrerequisites = ", weap, ~allies",
				Summary = "Feldartillerie: 155mm, Frontwaffe und Munitionsexplosion. Basispreis vor Panzer-Doktrinrabatt."
			},
			new CustomVehicleAssembly("designer-prism-hull", "Prismtank", "prismtank", "Turreted",
				new[] { "designer-prism-gear" }, new[] { "diesel" }, "light", "designer-prism-mount", "designer-prism-weapon", new[] { "designer-prism-payload" },
				new Dictionary<string, string> { { "Armament", "PrisTLaser" } })
			{
				Faction = "allies", PreserveWeaponTemplates = true, ExtraPrerequisites = ", radar, ~promotion.prism_tank",
				Summary = "Prism-Tank: Strahl samt Sekundaerstrahlen und Prism-Tech. Freischaltung/Radar noetig; Preis vor Doktrinrabatt."
			},
			new CustomVehicleAssembly("soviet-heavy-hull", "Heavy_Tank", "heavytank", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "soviet-twin-mount", "medium-cannon", new[] { "medium-tank-shell" },
				new Dictionary<string, string> { { "Armament", "125mm" } })
			{
				Faction = "soviet", ExtraPrerequisites = ", ~vehicles.russia, radar, ~!upg.heavy_tank",
				Summary = "Schwerer Panzer: volle 125-mm-Zwillingskanone, Rueckstoss und Wrack. Radar erforderlich."
			},
			new CustomVehicleAssembly("t34-hull", "T-34", "t34", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "light-cannon-mount", "light-cannon", new[] { "light-tank-shell" },
				new Dictionary<string, string> { { "Armament@primary", "30mm" }, { "Armament@primary-cluster", "30mm.Cluster_Upgrade" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				Summary = "T-34: normale/Cluster-Kanone bedingt geerbt. Nordkoreanischer Rumpf fuer das eigene Sowjet-Roster."
			},
			new CustomVehicleAssembly("heavy-tesla", "TTNK.RA2", "ttnk.ra2", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "twin-coil", "tesla", new[] { "discharge" },
				new Dictionary<string, string> { { "Armament", "TTankZapMK2" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", dome, ~vehicles.russia, ~!promotion.tesla_arc",
				Summary = "Tesla-Panzer: Doppelentladung, Schussverzoegerung und Animation geerbt. Radar noetig; Arc-Variante separat."
			},
			new CustomVehicleAssembly("designer-flak-hull", "FTRK", "ftrk", "Turreted",
				new[] { "designer-flak-gear" }, new[] { "diesel-light" }, "light", "designer-flak-mount", "designer-flak-weapon", new[] { "designer-flak-payload" },
				new Dictionary<string, string> { { "Armament@AA", "FLAK-23-AA" }, { "Armament@AG", "FLAK-23-AG" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", ~vehicles.soviet, ~!promotion.flak_track.barrage",
				Summary = "Flak-Laster: unabhaengige Boden-/Luftwaffen mit eigenen Projektilen, Reichweiten und Streuung."
			},
			ScrinAssembly("gunwalker", "GUNW", "scrin-walker-gear", null,
				new Dictionary<string, string> { { "Armament@PRIMARY", "GunWalkerZap" }, { "Armament@SECONDARY", "GunWalkerZapAA" } },
				", wsph, ~scrin, ~!upg.gunwalker", "Gun Walker: getrennte Boden-/Luftwaffen, Frontangriff und originale Laufanimation."),
			ScrinAssembly("seeker", "SEEK", "scrin-hover-gear", "Turreted",
				new Dictionary<string, string> { { "Armament@PRIMARY", "PlasmaDiscs" } },
				", ~seeker, ~!upg.seeker", "Seeker: Plasmascheiben und echtes LightHover-Verhalten; Wasserrisiken bei EMP/Fahrertod bleiben."),
			ScrinAssembly("corrupter", "CORR", "scrin-walker-gear", null,
				new Dictionary<string, string> { { "Armament@PRIMARY", "CorrupterSpew" } },
				", ~scrin, ~!upg.corrupter", "Corrupter: vollstaendiger Spew-Angriff, Frontausrichtung, Lauf-/Schussanimation und Todesexplosion."),
			ScrinAssembly("devourer", "DEVO", "scrin-hover-gear", "TurretedFloating",
				new Dictionary<string, string> { { "Armament", "DevourerLaser" } },
				", radar, ~traveler, ~!upg.devourer", "Devourer: Laser-Salve, schwebender Turm und LightHover; originales schnelles Rumpfdrehen bleibt."),
			NodCombatAssembly("apc", "APC2", "apc2", "tracks-standard", "diesel", "heavy", "vehicles, infantry.any, ~!promotion.apc_vulcan",
				new Dictionary<string, string> { { "Armament", "M60mgTD" } }, "APC: Original-MG, fuenf Infanterieplaetze, Ladesperre und besatzungsabhaengige Eroberbarkeit."),
			NodCombatAssembly("bike", "BIKE", "recon_bike", "wheels-light", "diesel-light", "light", "vehicles, ~!upg.reconbike",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BikeRockets" }, { "Armament@SECONDARY", "BikeRocketsAA" } }, "Recon Bike: getrennte Boden-/Luftraketen und originales Frontangriffsverhalten."),
			NodCombatAssembly("beam", "Beam_Cannon", "beam_cannon", "light-tracks", "diesel", "light", "tmpl, vehicles, ~promotion.beam_cannon",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BeamCannon" }, { "Armament@1", "BeamCannonBoosted1" },
					{ "Armament@2", "BeamCannonBoosted2" }, { "Armament@3", "BeamCannonBoosted3" }, { "Armament@4", "BeamCannonBoosted4" },
					{ "Armament@5", "BeamCannonBoosted5" }, { "Armament@TERTIARY", "BeamCannonVisual" }, { "Armament@charge", "BeamCannonCharge" } },
				"Beam Cannon: alle sechs Strahlstaerken, Visualisierung und Verstaerkung von Kanonen/Obelisken bleiben erhalten."),
			NodCombatAssembly("flame", "FTNK", "devils_tongue", "tracks-standard", "diesel", "heavy", "tier2, vehicles, ~!promotion.flame_tank.pdl, ~!promotion.flame_tank.reflector",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BigFlamer" } }, "Devil's Tongue: Originalflammen, Todesexplosion und Black-Hand-Kostenmodifikator."),
			NodCombatAssembly("heavy-flame", "HFTK", "heavy_flame_tank", "designer-heavy-tracks", "diesel", "heavy", "radar, vehicles, ~!upg.flametank",
				new Dictionary<string, string> { { "Armament@PRIMARY", "HeavyFlameTankFlamer" }, { "Armament@FF", "HeavyFlameTankFlamerFF" } }, "Heavy Flame Tank: beide Flammenkanaele, Animation, Schleifensound und schwere Ketten."),
			NodCombatAssembly("howitzer", "HOWI", "howi", "tracks-light-artillery", "diesel", "light", "vehicles",
				new Dictionary<string, string> { { "Armament", "155mmTDM" } }, "Howitzer: Originalgeschuetz, Turm und leichte Ketten; langsame Rumpfdrehung bleibt."),
			NodCombatAssembly("specter", "SPEC", "spec", "tracks-light-artillery", "diesel", "light", "tmpl, vehicles",
				new Dictionary<string, string> { { "Armament", "155mmSpec" } }, "Specter: Tarnung, automatische Aufstellung, Originalgeschuetz und Aufstellanimation."),
			NodCombatAssembly("stealth", "STNK", "stealth_tank", "light-tracks", "diesel", "light", "tier2, vehicles, ~!promotion.stealth_tank.ap, ~!promotion.stealth_tank.scrin, ~!promotion.stealth_tank.explosive_rockets",
				new Dictionary<string, string> { { "Armament@PRIMARY", "StnkMissile" }, { "Armament@SECONDARY", "StnkMissile.AA" } }, "Stealth Tank: Boden-/Luftraketen, normale/verbesserte Tarnung und Schadenstarnschwelle."),
			NodCombatAssembly("chemical", "TTRK", "ttrk", "wheels-light", "diesel-light", "light", "vehicles, tmpl",
				new Dictionary<string, string> { { "Armament@PRIMARY", "DemoTruckTargeting" } }, "Chemical Truck: Original-Selbstzerstoerung, Entschaerfung und Giftwolke; keine normale Schadenskanone."),
			NodCombatAssembly("microwave", "WTNK", "mwtnk", "light-tracks", "diesel", "light", "tmpl, vehicles",
				new Dictionary<string, string> { { "Armament@PRIMARY", "MicrowaveZap" }, { "Armament@PRIMARYSOUND", "MicrowaveZapSound" } }, "Microwave Tank: Deaktivierung, separater Soundkanal, Zielausschluesse und Muzzle-Effekt."),
			ChinaCombatAssembly("inferno", "charty", "charty", "light-tracks", "light", "chweap, radar, ~vehicles.china, ~!vehicles.chinainf",
				new Dictionary<string, string> { { "Armament", "CHInfernoCannon" }, { "Armament@Upgraded", "CHInfernoCannon.Black_Napalm" } },
				"Inferno Cannon: normales/Black-Napalm-Geschoss, Feuersturm und originale Frontausrichtung."),
			ChinaCombatAssembly("crawler", "chcrawl2", "chcrawl", "tracks-standard", "heavy", "chweap, ~!upg.crawler",
				new Dictionary<string, string>(), "Heavy Troop Crawler: sechs Plaetze, Originalbesatzung und Schiessscharten; keine zusaetzliche Bordkanone."),
			ChinaCombatAssembly("nuke", "chnukecann", "chnukecann", "tracks-standard", "heavy", "tier3, ~chweap, ~nuke_cannon.access, ~!upg.nuke_cannon",
				new Dictionary<string, string> { { "Armament@PRIMARY", "CHNukeCannon" }, { "Armament@AIM", "NukeCannonDummyAim" } },
				"Nuke Cannon: Aufstellung, Munitionszyklus, abschiessbare Granate und Nuklearexplosion bleiben erhalten."),
			ChinaCombatAssembly("bixi", "Bixi", "bixi", "light-tracks", "light", "tier3, ~chweap, ~promotion.bixi_dragon",
				new Dictionary<string, string> { { "Armament", "BixiLauncher" } }, "Bixi Dragon: zwei echte Raketenakteure, Nachladen, Zielpause und Originalanimation.")
		}.Concat(CreateStockCombatAssemblies()).ToArray();

		public bool UsesStockArmaments(CustomFactionProfile p) => Assembly(p).InheritStockWeapons;

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

		public string PreviewActor(CustomFactionProfile p) => Assembly(p).Actor;

		public bool IsNativeHover(CustomFactionProfile p) => Assembly(p).NativeHover;

		public string WeaponSummary(CustomFactionProfile p)
		{
			var a = Assembly(p);
			if (a.Summary != null)
			{
				var slots = N(Part(p, "chassis"), "carrier_slots");
				var used = Part(p, "carrier")["slots_required"] == null ? 1 : N(Part(p, "carrier"), "slots_required");
				return (slots > 1 ? $"Turmplaetze: {used:0}/{slots:0} belegt. " : "") + a.Summary;
			}
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
