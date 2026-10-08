using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

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
				{ "carrier", new[] { CanonicalCarrier(hull, carrier) } }, { "weapon", new[] { weapon } }, { "ammunition", ammunition }
			};
		}

		public static string CanonicalCarrier(string hull, string id) => (hull, id) switch
		{
			("designer-prism-hull", "designer-prism-mount") => "prism-turret",
			("designer-disruptor-hull", "designer-disruptor-mount") => "sonic-turret",
			("nod-combat-howitzer-hull", "integrated-mount") => "artillery-turret",
			("nod-combat-stealth-hull", "integrated-mount") => "missile-turret",
			("stock-gdrn-hull", "integrated-mount") => "mini-turret-mount",
			("stock-vulc-hull", "integrated-mount") => "dual-gatling-turret",
			_ => id
		};

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
				Summary = "Juggernaut: artillery and targeting helper; walking, aiming and firing animations retained."
			},
			new CustomVehicleAssembly("designer-mammoth-hull", "Mammoth", "mammoth", "Turreted@PRIMARY",
				new[] { "designer-heavy-tracks" }, new[] { "diesel-heavy" }, "heavy",
				"designer-mammoth-mount", "designer-mammoth-weapons", new[] { "designer-mammoth-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "130mmTD" }, { "Armament@SECONDARY", "MammothTusk" } })
			{
				PreserveWeaponTemplates = true,
				Summary = "Mammoth: twin cannon, anti-infantry/air missiles, regeneration and heavy tracks."
			},
			new CustomVehicleAssembly("designer-hmlrs-hull", "hmlrs", "hmlrs", "Turreted",
				new[] { "prototype-hover" }, new[] { "diesel" }, "light",
				"designer-hmlrs-mount", "designer-hmlrs-weapons", new[] { "designer-hmlrs-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "227mmH" }, { "Armament@SECONDARY", "227mmAAH" },
					{ "Armament@PRIMARY-UPG2", "227mm.upg" }, { "Armament@SECONDARY-UPG2", "227mmAA.upg" } })
			{
				PreserveWeaponTemplates = true, NativeHover = true, ExtraPrerequisites = ", ~promotion.hover_mlrs",
				Summary = "Native Hover MLRS: ground/AA and both upgrade channels. Requires Hover MLRS unlock."
			},
			new CustomVehicleAssembly("designer-disruptor-hull", "DISR", "disr", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy",
				"designer-disruptor-mount", "designer-disruptor-weapons", new[] { "designer-disruptor-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "SonicZap" }, { "Armament@SECONDARY", "SonicZapVisual" },
					{ "Armament@PRIMARYUPG", "SonicZap.UPG" }, { "Armament@SECONDARYUPG", "SonicZapVisual.UPG" } })
			{
				PreserveWeaponTemplates = true,
				Summary = "Disruptor: area sonic beam, visual beam and conditional upgrades; not a shell substitution."
			},
			new CustomVehicleAssembly("designer-mk2-hull", "MAMMOTHMK2", "mammothmk2", null,
				new[] { "designer-mk2-legs" }, new[] { "diesel-heavy" }, "heavy",
				"designer-mk2-mount", "designer-mk2-weapons", new[] { "designer-mk2-payload" },
				new Dictionary<string, string> { { "Armament@RAILGUN", "Railgun.MKII" }, { "Armament@MISSILES", "Dragon.MKII" },
					{ "Armament@AAMISSILES", "RedEye.MKII" } })
			{
				PreserveWeaponTemplates = true, VoxelImage = "mammothmk2",
				ExtraPrerequisites = ", ~promotion.mammoth_mkii, miss.gdi",
				Summary = "Mk II: railgun, ground missiles and AA; regenerating walker. Original unlock required."
			},
			new CustomVehicleAssembly("designer-titan-hull", "TITN", "titn", "Turreted@PRIMARY",
				new[] { "walker-heavy" }, new[] { "diesel" }, "heavy", "designer-titan-mount", "designer-titan-weapon", new[] { "designer-titan-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "TitanGun" }, { "Armament@SECONDARY", "TitanTusk" } })
			{
				PreserveWeaponTemplates = true, ExtraPrerequisites = ", ~!upg.titan",
				Summary = "Titan: sprite walker with cannon, missiles, regeneration and original turret/walking behavior."
			},
			new CustomVehicleAssembly("designer-slingshot-hull", "SLNG", "slng", "Turreted",
				new[] { "gdi-light-hover" }, new[] { "diesel-light" }, "light", "designer-slingshot-mount", "designer-slingshot-weapon", new[] { "designer-slingshot-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "SlingshotAA" } })
			{
				PreserveWeaponTemplates = true, NativeHover = true, ExtraPrerequisites = ", ~promotion.slingshot",
				Summary = "Slingshot: native light hover flak, air targets only; original unlock required."
			},
			new CustomVehicleAssembly("designer-marv-hull", "MARV", "marv", "Turreted",
				new[] { "tracks-superheavy" }, new[] { "diesel-heavy" }, "heavy", "designer-marv-mount", "designer-marv-weapon", new[] { "designer-marv-payload" },
				new Dictionary<string, string> { { "Armament@PRIMARY", "IonZap.Marv" } })
			{
				PreserveWeaponTemplates = true, ExtraPrerequisites = ", ~promotion.marv, miss.gdi",
				Summary = "MARV: triple ion weapon, regeneration and drive-over harvesting; original unlock required."
			},
			new CustomVehicleAssembly("nod-light-hull", "LTNK", "ltnk", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "light-cannon-mount", "light-cannon", new[] { "light-tank-shell" },
				new Dictionary<string, string> { { "Armament", "30mm" } })
			{
				Faction = "nod", PreserveWeaponTemplates = true,
				Summary = "Nod Light Tank: complete 30 mm cannon, recoil, driver and upgrade behavior."
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
				Summary = "Nod Artillery: 155mmTD, fixed forward gun, light tracked movement and ammunition explosion."
			},
			new CustomVehicleAssembly("designer-ssm-hull", "SSM", "ssm", "Turreted",
				new[] { "designer-ssm-gear" }, new[] { "diesel" }, "light", "designer-ssm-mount", "designer-ssm-weapons", new[] { "designer-ssm-payload" },
				new Dictionary<string, string> { { "Armament", "HonestJohn" } })
			{
				Faction = "nod", PreserveWeaponTemplates = true, ExtraPrerequisites = ", tmpl, ~promotion.ssm_launcher",
				Summary = "SSM: napalm rockets, ammunition storage, reload and variable rocket graphics. Unlock and Temple required."
			},
			new CustomVehicleAssembly("battlemaster", "chbattle", "chbattle", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel", "diesel-large" }, "heavy", "cannon-turret", "cannon", new[] { "shell" },
				new Dictionary<string, string> { { "Armament", "CHBattlemasterCannon" } })
			{
				Faction = "china", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", chweap, ~vehicles.china, ~!vehicles.chinainf, ~!upg.battlemaster",
				Summary = "Battlemaster: cannon, horde and nuclear upgrades retained. Base stats exclude situational bonuses."
			},
			new CustomVehicleAssembly("dragon-chassis", "chdragon", "chdragon", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel", "diesel-large" }, "heavy", "flame-turret", "dragon-flamer", new[] { "flame-fuel" },
				new Dictionary<string, string> { { "Armament", "CHDragonFlamer" }, { "Armament@Black_Napalm", "CHDragonFlamer.Black_Napalm" },
					{ "Armament@Firewall1", "CHDragonFirestorm" }, { "Armament@Firewall2", "CHDragonFirestorm2" } })
			{
				Faction = "china", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", ~vehicles.china, ~!promotion.dragon_tank.pdl, ~!promotion.dragon_tank.reflector",
				Summary = "Dragon: flamethrower, Black Napalm and both Firewall phases; original deployment/movement behavior."
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
				Summary = "Gatling: four ground/air spin-up stages each, with independent spin-up, target switching and cooldown."
			},
			new CustomVehicleAssembly("overlord-chassis", "choverlord", "choverlord", "Turreted",
				new[] { "tracks-superheavy" }, new[] { "diesel-heavy", "diesel-heavy-boost" }, "heavy", "heavy-twin-turret", "overlord-cannon", new[] { "heavy-shell" },
				new Dictionary<string, string> { { "Armament", "OverlordCannon" } })
			{
				Faction = "china", PreserveWeaponTemplates = true, AlternateTooltips = new[] { "Tooltip@Emperor" },
				ExtraPrerequisites = ", radar, ~vehicles.china, ~!vehicles.chinainf, ~!upg.overlord",
				Summary = "Overlord: twin cannon; Tank General uses Emperor graphics and takes 80% damage. Displayed HP is raw."
			},
			new CustomVehicleAssembly("allied-medium-hull", "Challenger_Tank", "Challenger_Tank", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "medium-cannon-mount", "medium-cannon", new[] { "medium-tank-shell" },
				new Dictionary<string, string> { { "Armament", "90mm" } })
			{
				Faction = "allies", ExtraPrerequisites = ", ~england",
				Summary = "Challenger: complete 90 mm cannon and original graphics. Base price excludes armored doctrine discount."
			},
			new CustomVehicleAssembly("designer-ranger-hull", "JEEP", "jeep", "Turreted",
				new[] { "wheels-light" }, new[] { "diesel-light" }, "light", "designer-ranger-mount", "scout-mg", new[] { "scout-mg-rounds" },
				new Dictionary<string, string> { { "Armament", "M60mg" } }, "scout-sensors")
			{
				Faction = "allies",
				ExtraPrerequisites = ", weap, ~allies, ~!promotion.infantry_doctrine, ~!promotion.armored_doctrine, ~!promotion.airforce_doctrine",
				Summary = "Ranger: M60mg and sensors (50 credits included). Like the original, buildable only before doctrine selection."
			},
			new CustomVehicleAssembly("field-artillery-hull", "ARTY", "arty", null,
				new[] { "tracks-light-artillery" }, new[] { "diesel" }, "light", "field-artillery-mount", "field-artillery", new[] { "field-artillery-he" },
				new Dictionary<string, string> { { "Armament", "155mm" } })
			{
				Faction = "allies", PreserveWeaponTemplates = true, ExtraPrerequisites = ", weap, ~allies",
				Summary = "Field Artillery: 155mm, forward gun and ammunition explosion. Base price excludes armored doctrine discount."
			},
			new CustomVehicleAssembly("designer-prism-hull", "Prismtank", "prismtank", "Turreted",
				new[] { "designer-prism-gear" }, new[] { "diesel" }, "light", "designer-prism-mount", "designer-prism-weapon", new[] { "designer-prism-payload" },
				new Dictionary<string, string> { { "Armament", "PrisTLaser" } })
			{
				Faction = "allies", PreserveWeaponTemplates = true, ExtraPrerequisites = ", radar, ~promotion.prism_tank",
				Summary = "Prism Tank: primary/secondary beams and Prism Tech. Unlock/radar required; price excludes doctrine discount."
			},
			new CustomVehicleAssembly("soviet-heavy-hull", "Heavy_Tank", "heavytank", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "soviet-twin-mount", "medium-cannon", new[] { "medium-tank-shell" },
				new Dictionary<string, string> { { "Armament", "125mm" } })
			{
				Faction = "soviet", ExtraPrerequisites = ", ~vehicles.russia, radar, ~!upg.heavy_tank",
				Summary = "Heavy Tank: complete twin 125 mm cannon, recoil and wreck. Radar required."
			},
			new CustomVehicleAssembly("t34-hull", "T-34", "t34", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "light-cannon-mount", "light-cannon", new[] { "light-tank-shell" },
				new Dictionary<string, string> { { "Armament@primary", "30mm" }, { "Armament@primary-cluster", "30mm.Cluster_Upgrade" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				Summary = "T-34: conditional standard/cluster cannon retained. North Korean hull admitted to the custom Soviet roster."
			},
			new CustomVehicleAssembly("heavy-tesla", "TTNK.RA2", "ttnk.ra2", "Turreted",
				new[] { "tracks-standard" }, new[] { "diesel" }, "heavy", "twin-coil", "tesla", new[] { "discharge" },
				new Dictionary<string, string> { { "Armament", "TTankZapMK2" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", dome, ~vehicles.russia, ~!promotion.tesla_arc",
				Summary = "Tesla Tank: twin discharge, firing delay and animation retained. Radar required; Arc variant is separate."
			},
			new CustomVehicleAssembly("designer-flak-hull", "FTRK", "ftrk", "Turreted",
				new[] { "designer-flak-gear" }, new[] { "diesel-light" }, "light", "designer-flak-mount", "designer-flak-weapon", new[] { "designer-flak-payload" },
				new Dictionary<string, string> { { "Armament@AA", "FLAK-23-AA" }, { "Armament@AG", "FLAK-23-AG" } })
			{
				Faction = "soviet", PreserveWeaponTemplates = true,
				ExtraPrerequisites = ", ~vehicles.soviet, ~!promotion.flak_track.barrage",
				Summary = "Flak Truck: independent ground/air weapons with their own projectiles, ranges and spread."
			},
			ScrinAssembly("gunwalker", "GUNW", "scrin-walker-gear", null,
				new Dictionary<string, string> { { "Armament@PRIMARY", "GunWalkerZap" }, { "Armament@SECONDARY", "GunWalkerZapAA" } },
				", wsph, ~scrin, ~!upg.gunwalker", "Gun Walker: separate ground/air weapons, frontal attack and original walking animation."),
			ScrinAssembly("seeker", "SEEK", "scrin-hover-gear", "Turreted",
				new Dictionary<string, string> { { "Armament@PRIMARY", "PlasmaDiscs" } },
				", ~seeker, ~!upg.seeker", "Seeker: plasma discs and native LightHover movement; water risks from EMP/driver death remain."),
			ScrinAssembly("corrupter", "CORR", "scrin-walker-gear", null,
				new Dictionary<string, string> { { "Armament@PRIMARY", "CorrupterSpew" } },
				", ~scrin, ~!upg.corrupter", "Corrupter: complete spew attack, forward facing, walking/firing animation and death explosion."),
			ScrinAssembly("devourer", "DEVO", "scrin-hover-gear", "TurretedFloating",
				new Dictionary<string, string> { { "Armament", "DevourerLaser" } },
				", radar, ~traveler, ~!upg.devourer", "Devourer: laser burst, floating turret and LightHover; original fast hull turning retained."),
			NodCombatAssembly("apc", "APC2", "apc2", "tracks-standard", "diesel", "heavy", "vehicles, infantry.any, ~!promotion.apc_vulcan",
				new Dictionary<string, string> { { "Armament", "M60mgTD" } }, "APC: original machine gun, five infantry seats, loading lock and crew-dependent capturability."),
			NodCombatAssembly("bike", "BIKE", "recon_bike", "wheels-light", "diesel-light", "light", "vehicles, ~!upg.reconbike",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BikeRockets" }, { "Armament@SECONDARY", "BikeRocketsAA" } }, "Recon Bike: separate ground/air missiles and original frontal attack behavior."),
			NodCombatAssembly("beam", "Beam_Cannon", "beam_cannon", "light-tracks", "diesel", "light", "tmpl, vehicles, ~promotion.beam_cannon",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BeamCannon" }, { "Armament@1", "BeamCannonBoosted1" },
					{ "Armament@2", "BeamCannonBoosted2" }, { "Armament@3", "BeamCannonBoosted3" }, { "Armament@4", "BeamCannonBoosted4" },
					{ "Armament@5", "BeamCannonBoosted5" }, { "Armament@TERTIARY", "BeamCannonVisual" }, { "Armament@charge", "BeamCannonCharge" } },
				"Beam Cannon: all six beam strengths, visuals and cannon/Obelisk boosting retained."),
			NodCombatAssembly("flame", "FTNK", "devils_tongue", "tracks-standard", "diesel", "heavy", "tier2, vehicles, ~!promotion.flame_tank.pdl, ~!promotion.flame_tank.reflector",
				new Dictionary<string, string> { { "Armament@PRIMARY", "BigFlamer" } }, "Devil's Tongue: original flames, death explosion and Black Hand cost modifier."),
			NodCombatAssembly("heavy-flame", "HFTK", "heavy_flame_tank", "designer-heavy-tracks", "diesel", "heavy", "radar, vehicles, ~!upg.flametank",
				new Dictionary<string, string> { { "Armament@PRIMARY", "HeavyFlameTankFlamer" }, { "Armament@FF", "HeavyFlameTankFlamerFF" } }, "Heavy Flame Tank: both flame channels, animation, looping sound and heavy tracks."),
			NodCombatAssembly("howitzer", "HOWI", "howi", "tracks-light-artillery", "diesel", "light", "vehicles",
				new Dictionary<string, string> { { "Armament", "155mmTDM" } }, "Howitzer: original gun, turret and light tracks; slow hull turning retained."),
			NodCombatAssembly("specter", "SPEC", "spec", "tracks-light-artillery", "diesel", "light", "tmpl, vehicles",
				new Dictionary<string, string> { { "Armament", "155mmSpec" } }, "Specter: cloak, automatic deployment, original gun and deployment animation."),
			NodCombatAssembly("stealth", "STNK", "stealth_tank", "light-tracks", "diesel", "light", "tier2, vehicles, ~!promotion.stealth_tank.ap, ~!promotion.stealth_tank.scrin, ~!promotion.stealth_tank.explosive_rockets",
				new Dictionary<string, string> { { "Armament@PRIMARY", "StnkMissile" }, { "Armament@SECONDARY", "StnkMissile.AA" } }, "Stealth Tank: ground/air missiles, standard/improved cloak and damage-based cloak threshold."),
			NodCombatAssembly("chemical", "TTRK", "ttrk", "wheels-light", "diesel-light", "light", "vehicles, tmpl",
				new Dictionary<string, string> { { "Armament@PRIMARY", "DemoTruckTargeting" } }, "Chemical Truck: original self-destruction, defusal and toxin cloud; not a conventional damage weapon."),
			NodCombatAssembly("microwave", "WTNK", "mwtnk", "light-tracks", "diesel", "light", "tmpl, vehicles",
				new Dictionary<string, string> { { "Armament@PRIMARY", "MicrowaveZap" }, { "Armament@PRIMARYSOUND", "MicrowaveZapSound" } }, "Microwave Tank: disabling effect, separate sound channel, target exclusions and muzzle effect."),
			ChinaCombatAssembly("inferno", "charty", "charty", "light-tracks", "light", "chweap, radar, ~vehicles.china, ~!vehicles.chinainf",
				new Dictionary<string, string> { { "Armament", "CHInfernoCannon" }, { "Armament@Upgraded", "CHInfernoCannon.Black_Napalm" } },
				"Inferno Cannon: standard/Black Napalm projectile, firestorm and original forward facing."),
			ChinaCombatAssembly("crawler", "chcrawl2", "chcrawl", "tracks-standard", "heavy", "chweap, ~!upg.crawler",
				new Dictionary<string, string>(), "Heavy Troop Crawler: six seats, original passengers and firing ports; no additional onboard gun."),
			ChinaCombatAssembly("nuke", "chnukecann", "chnukecann", "tracks-standard", "heavy", "tier3, ~chweap, ~nuke_cannon.access, ~!upg.nuke_cannon",
				new Dictionary<string, string> { { "Armament@PRIMARY", "CHNukeCannon" }, { "Armament@AIM", "NukeCannonDummyAim" } },
				"Nuke Cannon: deployment, ammunition cycle, interceptable shell and nuclear explosion retained."),
			ChinaCombatAssembly("bixi", "Bixi", "bixi", "light-tracks", "light", "tier3, ~chweap, ~promotion.bixi_dragon",
				new Dictionary<string, string> { { "Armament", "BixiLauncher" } }, "Bixi Dragon: two actual missile actors, reload, targeting pause and original animation.")
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
				throw new ArgumentException("Incompatible component.");
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

		public string TurretClass(CustomFactionProfile p) => Part(p, "carrier").Value<string>("turret_class") switch
		{
			"mini" => "Mini",
			"medium" => "Medium Turret",
			"medium-tank" => "Medium Tank Turret",
			"heavy-walker" => "Heavy Walker",
			"super-heavy" => "Super Heavy",
			_ => "Native mount (unclassified)"
		};

		public string WeaponSummary(CustomFactionProfile p)
		{
			var a = Assembly(p);
			if (a.Summary != null)
			{
				var slots = N(Part(p, "chassis"), "carrier_slots");
				var used = Part(p, "carrier")["slots_required"] == null ? 1 : N(Part(p, "carrier"), "slots_required");
				var module = Part(p, "carrier")["module_family"] != null ? Label(p.Parts["carrier"]) + ": " : "";
				return module + (slots > 1 || module.Length > 0 ? $"Turret slots: {used:0}/{slots:0} occupied. " : "") + a.Summary;
			}
			var range = N(Part(p, "weapon"), "range_cells");
			var burst = N(Part(p, "carrier"), "burst");
			if (a.Actor == "HMMV" || a.Actor == "BGGY")
			{
				var sensors = N((Newtonsoft.Json.Linq.JObject)catalog["components"]["scout-sensors"], "cost");
				return $"Machine gun: ground targets, range {range:0.##}, {burst:0}-round burst. Sensors included (+{sensors:0} credits).";
			}
			if (a.Actor == "MLRS")
			{
				var minimum = N(Part(p, "weapon"), "min_range_cells");
				var air = N(Part(p, "weapon"), "secondary_range_cells");
				return $"Rockets: ground {minimum:0.##}-{range:0.##} / air {air:0.##}, {burst:0}-round bursts; separate weapons.";
			}
			return (p.Parts["ammunition"] == "designer-he-shell" ? "HE cannon" : "Tank cannon") + $": range {range:0.##}, {burst:0} rounds per burst.";
		}
	}
}
