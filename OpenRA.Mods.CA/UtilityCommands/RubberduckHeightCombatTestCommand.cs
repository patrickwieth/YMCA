using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckHeightCombatTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-height-combat-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2 && args.Length <= 4 && args.Skip(2).All(a => a == "fog=true" || a.StartsWith("unit=", StringComparison.Ordinal));
		[Desc("OUTPUT [fog=true] [unit=challenger_tank]", "Create sixteen real weapon/turret tests: flat, uphill, downhill and blocked, four directions.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			ValidateMissileAltitude(utility.ModData);
			var fog = args.Contains("fog=true");
			var unit = (args.Skip(2).SingleOrDefault(a => a.StartsWith("unit=", StringComparison.Ordinal)) ?? "unit=challenger_tank").Substring(5).ToLowerInvariant();
			Directory.CreateDirectory(args[1]);
			var logs = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes"); Directory.CreateDirectory(logs);
			var result = Path.Combine(logs, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(args[1])))) + ".log");
			File.WriteAllText(result, ""); File.WriteAllText(Path.Combine(args[1], "runtime-results-path.txt"), result);
			using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 98, 114);
			var unitInfo = map.Rules.Actors[unit];
			var parentDefinition = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Rules, null)
				.Single(n => n.Key.Equals(unit, StringComparison.OrdinalIgnoreCase));
			var parent = parentDefinition.Key;
			var weaponNames = unitInfo.TraitInfos<ArmamentInfo>().Select(a => a.Weapon).ToHashSet(StringComparer.OrdinalIgnoreCase);
			var weaponParents = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Weapons, null)
				.Where(n => weaponNames.Contains(n.Key)).ToArray();
			var aliases = weaponParents.Select((n, i) => (n.Key, Alias: "calibration.height.weapon" + i)).ToDictionary(p => p.Key, p => p.Alias, StringComparer.OrdinalIgnoreCase);
			// Derive a new weapon instead of partially merging an existing definition with
			// duplicate unlabelled Inherits keys. All original projectile/damage data stays inherited.
			map.WeaponDefinitions = new MiniYaml("", weaponParents.Select(n => new MiniYamlNode(aliases[n.Key], new MiniYaml("", new[]
				{
					new MiniYamlNode("Inherits", n.Key), new MiniYamlNode("Warhead@HeightTrace", "HeightImpactTrace"),
				}))));
			var armamentRules = string.Concat(parentDefinition.Value.Nodes.Where(n => n.Key == "Armament" || n.Key.StartsWith("Armament@", StringComparison.Ordinal))
				.Select(n => $"\t{n.Key}:\n\t\tWeapon: {aliases[n.Value.Nodes.Single(c => c.Key == "Weapon").Value.Value]}\n"));
			var image = unitInfo.TraitInfo<RenderSpritesInfo>().Image ?? unit;
			var voxels = unitInfo.TraitInfoOrDefault<RenderVoxelsInfo>();
			var voxelRules = voxels == null ? "" : $"\tRenderVoxels:\n\t\tImage: {voxels.Image ?? unit}\n";
			map.Title = "Rubberduck height combat assertions " + unit; map.Author = "YMCA diagnostics"; map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(96, 112));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1000, 0);
			var middle = new MPos(48, 64).ToCPos(map);
			var actors = new List<MiniYamlNode>();
			void Actor(string id, string type, CPos cell, string owner)
			{
				if (!map.Contains(cell)) throw new InvalidDataException("Combat actor outside playable bounds: " + id);
				actors.Add(new MiniYamlNode(id, new ActorReference(type) { new LocationInit(cell), new OwnerInit(owner) }.Save()));
			}
			Actor("Spawn0", "mpspawn", middle + new CVec(-22, 22), "Neutral");
			Actor("Spawn1", "mpspawn", middle + new CVec(22, -22), "Neutral");
			var lifecycle = fog ? $"\tHeightVisionLifecycleProbe:\n\t\tResultPath: {result}\n" : "";
			var rules = new StringBuilder($"World:\n{lifecycle}\tTerrainCalibrationView:\n\t\tScreenshotTicks: -1\n\t\tCenter: {middle}\n\t\tZoomScale: 0.25\n\t\tMinimumZoomScale: 0.1\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: {(fog ? "true" : "false")}\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: {(fog ? "false" : "true")}\n\t\tExploredMapCheckboxLocked: true\n");
			var directions = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };
			var surfaces = new Dictionary<CPos, PlateauSurface>();
			for (var scenario = 0; scenario < 4; scenario++)
				for (var direction = 0; direction < 4; direction++)
				{
					var center = middle + new CVec((scenario - 2) * 10, (direction - 2) * 10);
					var delta = directions[direction];
					var start = center - delta * 2; var end = center + delta * 2;
					if (scenario == 1 || scenario == 2)
					{
						var roof = scenario == 1 ? start : end;
						// Edge perch: a three-cell-deep roof would itself obstruct a flat tank shell.
						surfaces[roof] = new PlateauSurface(4, 0, false);
					}
					else if (scenario == 3)
						for (var t = -2; t <= 2; t++)
							surfaces[center + new CVec(-delta.Y, delta.X) * t] = new PlateauSurface(4, 0, true);
					var suffix = scenario + "-" + direction;
					var source = "calibration.height-source-" + suffix;
					var target = "calibration.height-target-" + suffix;
					rules.AppendLine($"{source}:\n\tInherits: {parent}\n\tRenderSprites:\n\t\tImage: {image}\n{voxelRules}{armamentRules}\tAutoTarget:\n\t\tEnabledCondition: false\n\tHealth:\n\t\tHP: 1000000\n\tHeightCombatProbe:\n\t\tBarrierCenter: {center}\n\t\tBarrierTangent: {new CVec(-delta.Y, delta.X)}\n\t\tCheckVisibility: {fog}\n\t\tTargetType: {target}\n\t\tExpectDamage: {(scenario != 3 ? "true" : "false")}\n\t\tResultPath: {result}\n{target}:\n\tInherits: Challenger_Tank\n\tAutoTarget:\n\t\tEnabledCondition: false\n\tHealth:\n\t\tHP: 1000000\n");
					Actor("Source" + suffix, source, start, "Multi0"); Actor("Target" + suffix, target, end, "Creeps");
				}
			RubberduckPlateauRenderer.Apply(map, surfaces, actors);
			map.ActorDefinitions = actors; map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "height-combat"));
			var path = Path.Combine(args[1], "rubberduck-height-combat.oramap");
			using (var package = ZipFileLoader.Create(path)) map.Save(package);
			using var saved = File.OpenRead(path); using var zip = new ZipFileLoader.ReadOnlyZipFile(saved, path);
			using var reload = new Map(utility.ModData, zip);
			if (reload.InvalidCustomRules) throw new InvalidDataException("Invalid combat fixture.", reload.InvalidCustomRulesException);
			Console.WriteLine(path);
		}

		static void ValidateMissileAltitude(ModData modData)
		{
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 82, 98);
			map.SetBounds(new PPos(1, 17), new PPos(80, 96));
			foreach (var c in map.AllCells) map.Tiles[c] = new TerrainTile(1000, 0);
			var cell = new MPos(40, 56).ToCPos(map);
			var checks = 0;
			for (byte height = 0; height <= 4; height++)
				for (ushort ramp = 0; ramp <= 4; ramp++)
				{
					map.Tiles[cell] = new TerrainTile(ramp == 0 ? (ushort)1000 : (ushort)(14000 + ramp), 0);
					map.Height[cell] = height;
					foreach (var offset in new[] { WVec.Zero, new WVec(200, 0, 0), new WVec(-200, 0, 0), new WVec(0, 200, 0), new WVec(0, -200, 0) })
					{
						var point = map.CenterOfCell(cell) + offset + new WVec(0, 0, 3000);
						var expected = height * 724 + map.Grid.Ramps[map.Ramp[cell]].HeightOffset(offset.X, offset.Y);
						if (Projectiles.MissileCA.TerrainAltitude(map, point) != expected)
							throw new InvalidDataException("Missile lookahead does not match physical terrain height.");
						checks++;
					}
				}
			Console.WriteLine($"PASS: {checks} missile altitude samples, five heights and all four ramp directions.");
			map.Tiles[cell] = new TerrainTile(1000, 0); map.Height[cell] = 0;
			var targets = new[] { new CVec(4, 0), new CVec(0, 4), new CVec(-4, 0), new CVec(0, -4) }.Select(d => cell + d).ToArray();
			foreach (var target in targets) map.Height[target] = 4;
			var visible = TerrainOverrides.RubberduckShroudProjection.CellsInPhysicalRange(map, map.CenterOfCell(cell), WDist.Zero, new WDist(6 * 1024), -1).ToHashSet();
			foreach (var target in targets)
			{
				var uv = target.ToMPos(map);
				if (!map.ProjectedCellsCovering(uv).Any(visible.Contains)) throw new InvalidDataException("Directional raised-terrain sight failure.");
				foreach (var roof in map.ProjectedCellsCovering(uv))
					for (var v = roof.V; v <= uv.V; v++)
						if (!visible.Contains(new PPos(roof.U, v))) throw new InvalidDataException("Visible cliff has a shroud hole below its roof.");
			}
			if (TerrainOverrides.RubberduckShroudProjection.CellsInPhysicalRange(map, map.CenterOfCell(cell), WDist.Zero, WDist.Zero, -1).Any())
				throw new InvalidDataException("Zero-range source reveals terrain.");
			if (map.Rules.Actors["challenger_tank"].TraitInfos<RevealsShroudInfo>().Any(i => !(i is TerrainOverrides.RevealsShroudInfo)))
				throw new InvalidDataException("RevealsShroud YAML compatibility override was not resolved.");
			if (map.Rules.Actors.Values.SelectMany(a => a.TraitInfos<CreatesShroudInfo>()).Any(i => !(i is TerrainOverrides.CreatesShroudInfo)))
				throw new InvalidDataException("CreatesShroud YAML compatibility override was not resolved.");
			var source = map.CenterOfCell(cell);
			var range = new WDist(6 * 1024);
			var cached = TerrainOverrides.RubberduckShroudProjection.CachedCells(map, source, WDist.Zero, range, -1);
			if (!visible.SetEquals(cached) || !ReferenceEquals(cached, TerrainOverrides.RubberduckShroudProjection.CachedCells(map, source, WDist.Zero, range, -1)))
				throw new InvalidDataException("Physical sight cache changed coverage or failed to reuse a range.");
			var changed = targets[0];
			var originalHeight = map.Height[changed];
			map.Height[changed] = 0;
			var refreshed = TerrainOverrides.RubberduckShroudProjection.CachedCells(map, source, WDist.Zero, range, -1);
			if (ReferenceEquals(cached, refreshed) || !refreshed.ToHashSet().SetEquals(TerrainOverrides.RubberduckShroudProjection.CellsInPhysicalRange(map, source, WDist.Zero, range, -1)))
				throw new InvalidDataException("Physical sight cache did not invalidate after a height edit.");
			map.Height[changed] = originalHeight;
			for (var i = 0; i < 4200; i++)
			{
				var origin = map.CenterOfCell(new MPos(1 + i % 70, 17 + i / 70).ToCPos(map));
				var value = TerrainOverrides.RubberduckShroudProjection.CachedCells(map, origin, WDist.Zero, new WDist(1024), -1);
				if (!value.ToHashSet().SetEquals(TerrainOverrides.RubberduckShroudProjection.CellsInPhysicalRange(map, origin, WDist.Zero, new WDist(1024), -1)))
					throw new InvalidDataException("Physical sight cache eviction changed coverage.");
			}
			for (var i = 0; i < 600; i++)
			{
				var origin = map.CenterOfCell(new MPos(25 + i % 30, 45 + i / 30).ToCPos(map));
				var wide = new WDist(30 * 1024);
				var value = TerrainOverrides.RubberduckShroudProjection.CachedCells(map, origin, WDist.Zero, wide, -1);
				if (!value.ToHashSet().SetEquals(TerrainOverrides.RubberduckShroudProjection.CellsInPhysicalRange(map, origin, WDist.Zero, wide, -1)))
					throw new InvalidDataException("Large physical range cache changed coverage.");
			}
			if (TerrainOverrides.RubberduckShroudProjection.CachedRangeCount(map) > 4096 || TerrainOverrides.RubberduckShroudProjection.CachedCellCount(map) > 1024 * 1024 || !visible.SetEquals(cached))
				throw new InvalidDataException("Physical sight cache is unbounded or mutated a retained source.");
			Console.WriteLine("PASS: raised-target sight in four directions, zero-range exclusion, bounded cache parity/invalidation and reveal/jammer YAML compatibility.");
		}
	}
}
