using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CreateCliffFireProbeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--create-cliff-fire-probe";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 4;
		[Desc("Create a review-only cliff combat/stance fixture.", "OUTPUT RESULT baseline|fixed")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var baseline = args[3].StartsWith("baseline", StringComparison.Ordinal);
			if (!mod.DefaultRules.Actors["world"].HasTraitInfo<OpenRA.Mods.CA.Traits.CliffFireControlInfo>())
				throw new InvalidDataException("Production World must enable CliffFireControl; fixed fixtures do not inject it.");
			var probeWeapon = args[3].Contains("radiation") ? "RadBeamWeapon" : args[3].Contains("laserca") ? "PrisLaserAim" : args[3].Contains("plasma") ? "LeecherBeam" : args[3].Contains("rail") ? "IonZap" :
				args[3].Contains("area") ? "BeamCannon" : args[3].Contains("tesla") ? "ThorBlitz" :
				args[3].Contains("electric") ? "StormColumnZap" : args[3].Contains("pulse") ? "SonicPulse" : "RiftCannon";
			if (File.Exists(args[1])) throw new InvalidDataException("Use a fresh output path.");
			using var map = new Map(mod, (DefaultTerrain)mod.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 226, 226);
			map.SetBounds(new PPos(1, 1), new PPos(224, 224));
			map.Title = "[REVIEW ONLY] Cliff fire " + args[3]; map.Author = "YMCA"; map.RequiresMod = "ca";
			var players = new MapPlayers(map.Rules, 2);
			players.Players["Multi0"].Faction = "scrin";
			players.Players["Multi0"].Enemies = new[] { "Multi1" };
			players.Players["Multi1"].Enemies = new[] { "Multi0" };
			players.Players["Multi1"].Playable = false;
			map.PlayerDefinitions = players.ToMiniYaml();
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			var actors = new List<MiniYamlNode>();
			var surfaces = new Dictionary<CPos, PlateauSurface>();
			void Hill(CPos center, int radius, bool hole = false)
			{
				for (var x = -radius; x <= radius; x++) for (var y = -radius; y <= radius; y++)
					if (!hole || x != 0 || y != 0) surfaces[center + new CVec(x, y)] = new PlateauSurface(4, 0);
			}
			Hill(new CPos(90, 0), 3);
			Hill(new CPos(110, 0), 1);
			Hill(new CPos(130, 0), 1);
			Hill(new CPos(150, 0), 1, true);
			Hill(new CPos(170, 0), 1);
			Hill(new CPos(110, args[3].Contains("ssm") ? 40 : 20), 1);
			Hill(new CPos(170, 20), 3);
			RubberduckPlateauRenderer.ComposeNative(map, surfaces, actors, new HashSet<(CPos, int)>(), Array.Empty<(CPos, int)>(), "Rock", true);
			void Add(string id, string type, CPos cell, string owner, UnitStance stance = UnitStance.HoldFire)
			{
				var actor = new ActorReference(type) { new OwnerInit(owner), new LocationInit(cell) };
				var actorInfo = mod.DefaultRules.Actors[type.StartsWith("cliff.probe.", StringComparison.Ordinal) ? "channeler" : type];
				if (actorInfo.HasTraitInfo<AutoTargetInfo>()) actor.Add(new StanceInit(actorInfo.TraitInfo<AutoTargetInfo>(), stance));
				actors.Add(new MiniYamlNode(id, actor.Save()));
			}
			Add("Spawn", "mpspawn", new CPos(80, 0), "Neutral");
			Add("Laser", "cliff.probe.shooter", new CPos(90, 0), "Multi0");
			Add("LaserTarget", "mcv", new CPos(84, 0), "Multi1");
			var combat = args[3].Contains("ssm", StringComparison.Ordinal) ? "ssm.multi" : args[3].Contains("cannon", StringComparison.Ordinal) ? "mtnk" : args[3].Contains("infantry", StringComparison.Ordinal) ? "mercenary" : args[3].Contains("missile", StringComparison.Ordinal) ? "cliff.probe.rocket" : "channeler";
			var shift = combat is "channeler" or "ssm.multi" ? 0 : 1;
			Add("Defend", combat, new CPos(107 + shift, 0), "Multi0", UnitStance.Defend);
			Add("DefendTarget", "mcv", new CPos(113 - shift, 0), "Multi1");
			Add("Aggressive", combat, new CPos(127 + shift, 0), "Multi0", UnitStance.AttackAnything);
			Add("AggressiveTarget", "mcv", new CPos(133 - shift, 0), "Multi1");
			Add("Trapped", combat, new CPos(150, 0), "Multi0", UnitStance.AttackAnything);
			Add("TrappedTarget", "mcv", new CPos(154, 0), "Multi1");
			Add("Hold", combat, new CPos(167 + shift, 0), "Multi0", UnitStance.HoldFire);
			Add("HoldTarget", "mcv", new CPos(173 - shift, 0), "Multi1");
			Add("Return", combat, new CPos(107 + shift, args[3].Contains("ssm") ? 40 : 20), "Multi0", UnitStance.ReturnFire);
			Add("ReturnTarget", "mcv", new CPos(113 - shift, args[3].Contains("ssm") ? 40 : 20), "Multi1");
			var edgeUnit = combat == "mtnk" ? "mtnk" : "channeler";
			Add("EdgeLow", edgeUnit, new CPos(combat == "mtnk" ? 164 : 161, 20), "Multi0", UnitStance.Defend);
			Add("EdgeHigh", edgeUnit, new CPos(167, 20), "Multi1", UnitStance.Defend);
			Add("FarTarget", "mcv", new CPos(175, 30), "Multi1");
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString($@"
World:
	-SpawnStartingUnits:
	-NukularDoubleStartingUnits:
	CliffFireProbe:
		ResultPath: {Path.GetFullPath(args[2]).Replace('\\', '/')}
		Baseline: {baseline}
{(baseline ? "\t-CliffFireControl:" : "")}
Player:
	-ConquestVictoryConditions:
	Shroud:
		FogCheckboxEnabled: false
		FogCheckboxLocked: true
		ExploredMapCheckboxEnabled: true
		ExploredMapCheckboxLocked: true
cliff.probe.shooter:
	Inherits: Channeler
	CliffShotProbe:
	Armament:
		Weapon: {probeWeapon}
cliff.probe.rocket:
	Inherits: Channeler
	Armament:
		Weapon: Dragon
", "cliff-fire-probe"));
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
			using (var output = ZipFileLoader.Create(args[1])) map.Save(output);
			using var input = File.OpenRead(args[1]);
			using var zip = new ZipFileLoader.ReadOnlyZipFile(input, args[1]);
			using var check = new Map(mod, zip);
			if (check.InvalidCustomRules) throw new InvalidDataException("Invalid probe rules.", check.InvalidCustomRulesException);
			Console.WriteLine(args[1]);
		}
	}
}
