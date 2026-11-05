using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class RubberduckMapExporter
	{
		const string Tileset = "RUBBERDUCK-TEMPERATE";
		const ushort GrassA = 1000;
		const ushort Sand = 1020;
		const ushort Dirt = 1030;
		const ushort WaterA = 1050;
		const ushort MountainInterior = 3992;
		const byte TiberiumResource = 3;
		const byte BlueTiberiumResource = 4;

		readonly ModData modData;

		public RubberduckMapExporter(ModData modData)
		{
			this.modData = modData;
		}

		internal static void ValidateTwoSidesCoasts(MapPlan plan, MapGenerationOptions options)
		{
			// Preflight the actual physical coast planner before accepting a layout.
			// In particular, a clipped boundary must not isolate a last land cell.
			var mod = Game.ModData;
			var height = mod.Manifest.Get<MapGrid>().MaximumTerrainHeight;
			using var map = new Map(mod, (DefaultTerrain)mod.DefaultTerrainInfo[Tileset], plan.Width + 2, plan.Height + height + 2);
			map.SetBounds(new PPos(1, 1 + height), new PPos(plan.Width, plan.Height + height));
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(GrassA, 0);
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var uv = ToMapPosition(x, y, height);
					var tile = plan.TerrainAt(x, y) switch
					{
						PlannedTerrain.Land => GrassA,
						PlannedTerrain.Water => WaterA,
						PlannedTerrain.ShallowWater => Sand,
						_ => throw new InvalidOperationException("Unexpected raised terrain in Two Sides coast preflight."),
					};
					map.Tiles[uv] = new TerrainTile(tile, 0);
					if (plan.HasFeature(x, y, PlannedFeature.RichResource)) map.Resources[uv] = new ResourceTile(BlueTiberiumResource, 3);
					else if (plan.HasFeature(x, y, PlannedFeature.Resource)) map.Resources[uv] = new ResourceTile(TiberiumResource, 12);
				}
			var actors = CreateActors(map, plan, options, height).ToList();
			RubberduckGeneratedRockCoasts.Apply(map, plan, actors, (x, y) => ToMapPosition(x, y, height).ToCPos(map), true);
		}

		public string Export(MapPlan plan, MapGenerationOptions options, string outputDirectory, bool temporary = false)
		{
			if (!(modData.DefaultTerrainInfo[Tileset] is DefaultTerrain terrainInfo))
				throw new InvalidDataException($"Tileset '{Tileset}' does not use the expected terrain parser.");

			Directory.CreateDirectory(outputDirectory);
			// Rule trait loaders still resolve custom objects through the active global mod.
			Game.ModData = modData;
			var maximumHeight = modData.Manifest.Get<MapGrid>().MaximumTerrainHeight;
			using (var map = new Map(modData, terrainInfo, plan.Width + 2, plan.Height + maximumHeight + 2))
			{
				map.Title = $"{options.Mode} {options.Custom?.Description ?? options.Preset.DisplayName()} {options.Players}P {options.Seed}";
				map.Author = "YMCA procedural map generator";
				map.RequiresMod = "ca";
				map.Categories = new[] { "Conquest", options.Mode.ToString() };
				map.SetBounds(new PPos(1 + plan.PlayableInset, 1 + maximumHeight + plan.PlayableInset),
					new PPos(plan.Width - plan.PlayableInset, plan.Height + maximumHeight - plan.PlayableInset));
				map.PlayerDefinitions = CreatePlayers(map, options).ToMiniYaml();

				// The reserved projection border also needs complete ground tiles.
				foreach (var cell in map.AllCells)
					map.Tiles[cell] = new TerrainTile(GrassA, 0);
				var random = new MersenneTwister(unchecked(plan.Seed * 7919 + 17));
				var raisedCells = BuildRaisedMask(plan);
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
					{
						var uv = ToMapPosition(x, y, maximumHeight);
						map.Tiles[uv] = raisedCells[x, y]
							? new TerrainTile(MountainInterior, 0)
							: PickTerrainTile(plan, terrainInfo, x, y, maximumHeight, random, options.Preset == MapGenerationPreset.HillFortress,
								options.Preset == MapGenerationPreset.BattleNexus, options.Custom);
						map.Height[uv] = 0;
						if (plan.HasFeature(x, y, PlannedFeature.RichResource))
							map.Resources[uv] = new ResourceTile(BlueTiberiumResource, 3);
						else if (plan.HasFeature(x, y, PlannedFeature.Resource))
							map.Resources[uv] = new ResourceTile(TiberiumResource, 12);
					}

				ApplyAutoTile(map, plan, maximumHeight);
				var actors = CreateActors(map, plan, options, maximumHeight).ToList();
				// Render planned mountains in every preset, not only Mountain Valleys.
				// Otherwise Arabia/Fortress/Nexus export invisible, dirt-looking barriers.
				if (raisedCells.Cast<bool>().Any(raised => raised) || plan.PlateauSurfaces.Count != 0)
				{
					var mountains = new List<CPos>();
					for (var y = 0; y < plan.Height; y++)
						for (var x = 0; x < plan.Width; x++)
							if (raisedCells[x, y] && !plan.PlateauSurfaces.ContainsKey(new MPos(x, y)))
								mountains.Add(ToMapPosition(x, y, maximumHeight).ToCPos(map));
					if (options.Preset == MapGenerationPreset.MountainValleys && mountains.Count != 0)
						throw new InvalidDataException("Mountain Valleys contains an unplanned rock wall; legacy rendering is forbidden.");
					foreach (var cell in mountains)
						if (map.Tiles[cell].Type != MountainInterior || map.Resources[cell].Type != 0)
							throw new InvalidDataException("Mountain rendering may only elevate existing blocked, resource-free terrain.");
					RubberduckMountainRenderer.Apply(map, mountains, actors);
					if (plan.PlateauSurfaces.Count > 0)
					{
						if ((maximumHeight & 1) != 0) throw new InvalidDataException("Plateau export requires even projection-border parity.");
						var surfaces = plan.PlateauSurfaces.ToDictionary(
							pair => ToMapPosition(pair.Key.U, pair.Key.V, maximumHeight).ToCPos(map), pair => pair.Value);
						if (plan.RequireClosedPlateaus)
						{
							var closed = new RubberduckClosedCliffSelection(map, surfaces);
							if (surfaces.Any(p => RubberduckClosedCliffSelection.Elevated(p.Value) && !closed.Cells.Contains(p.Key)))
								throw new InvalidDataException("Committed closed plateau lost its reserved contour before export.");
						}
						var nativeFaces = plan.NativeCliffFaces.Select(face =>
							(Cell: ToMapPosition(face.Point.U, face.Point.V, maximumHeight).ToCPos(map), face.Direction)).ToHashSet();
						var nativePieces = plan.NativeCliffPieces.Select(piece =>
							(Cell: ToMapPosition(piece.Point.U, piece.Point.V, maximumHeight).ToCPos(map), piece.Slot));
						RubberduckPlateauRenderer.ComposeNative(map, surfaces, actors, nativeFaces, nativePieces, "NativeCliff", true);
					}
				}
				var coastSegments = RubberduckGeneratedRockCoasts.Apply(map, plan, actors,
					(x, y) => ToMapPosition(x, y, maximumHeight).ToCPos(map), options.Preset == MapGenerationPreset.TwoSides);
				if (coastSegments != 0) Console.WriteLine($"Flat rock coast: {coastSegments} protected-topology segments.");
				RubberduckBridgeExporter.Apply(map, plan, actors, maximumHeight);
				map.ActorDefinitions = actors;

				var modePrefix = options.Mode == GeneratedMapMode.Tactical
					? ""
					: options.Mode.ToString().ToLowerInvariant() + "-";
				var filename = temporary ? TemporaryGeneratedMapFiles.NewFilename() :
					$"{modePrefix}{PresetName(options.Preset)}-players-{options.Players}-seed-{options.Seed}.oramap";
				var path = Path.Combine(outputDirectory, filename);
				if (File.Exists(path))
				{
					if (temporary) throw new IOException("Temporary map filename already exists: " + path);
					File.Delete(path);
				}
				using (var package = ZipFileLoader.Create(path))
					map.Save(package);

				using (var stream = File.OpenRead(path))
				using (var package = new ZipFileLoader.ReadOnlyZipFile(stream, path))
				using (var validationMap = new Map(modData, package))
				{
					if (validationMap.InvalidCustomRules)
						throw new InvalidDataException("Exported map rules are invalid.", validationMap.InvalidCustomRulesException);
					foreach (var cell in map.AllCells)
						if (validationMap.Tiles[cell].Type != map.Tiles[cell].Type || validationMap.Height[cell] != map.Height[cell] ||
							validationMap.Tiles[cell].Index != map.Tiles[cell].Index || validationMap.Ramp[cell] != map.Ramp[cell])
							throw new InvalidDataException("Terrain changed during export roundtrip.");
				}
				return path;
			}
		}

		static MapPlayers CreatePlayers(Map map, MapGenerationOptions options)
		{
			var players = new MapPlayers(map.Rules, options.Players);
			if (options.Mode != GeneratedMapMode.Strategic && options.Preset != MapGenerationPreset.TwoSides)
				return players;

			var firstTeamPlayers = (options.Players + 1) / 2;
			for (var index = 0; index < options.Players; index++)
			{
				var player = players.Players[$"Multi{index}"];
				player.Team = index < firstTeamPlayers ? 1 : 2;
				player.LockTeam = true;
				player.Spawn = index + 1;
				player.LockSpawn = true;
			}
			return players;
		}

		static TerrainTile PickTerrainTile(
			MapPlan plan, DefaultTerrain terrainInfo, int x, int y, int maximumHeight, MersenneTwister random, bool centeredDirt, bool fullCenterDirt, CustomMapSettings custom)
		{
			ushort template;
			switch (plan.TerrainAt(x, y))
			{
				case PlannedTerrain.Water:
					template = WaterA;
					break;
				case PlannedTerrain.ShallowWater:
					// The actor supplies the intact crossing. Destruction must expose
					// actual water, not the old indestructible sand causeway.
					template = plan.HasFeature(x, y, PlannedFeature.DestructibleBridge) ? WaterA :
						custom?.HasCenter == true && plan.HasFeature(x, y, PlannedFeature.ContestedCenter) ? (custom.CenterBuildable ? GrassA : Dirt) : Sand;
					break;
				case PlannedTerrain.Mountain:
					template = MountainInterior;
					break;
				default:
					if (plan.HasFeature(x, y, PlannedFeature.DestructibleBridge))
						template = GrassA;
					else if (custom != null && plan.HasFeature(x, y, PlannedFeature.ContestedCenter))
						template = custom.HasCenter && !custom.CenterBuildable ? Dirt : GrassA;
					else if (plan.HasFeature(x, y, PlannedFeature.ContestedCenter) &&
						(fullCenterDirt || (centeredDirt ? IsCenteredDirtPatch(plan, x, y) : IsDirtPatch(plan.Seed, x, y))))
						template = Dirt;
					else
						template = GrassA;
					break;
			}

			var count = terrainInfo.Templates[template].TilesCount;
			return new TerrainTile(template, (byte)random.Next(Math.Max(1, count)));
		}

		static bool IsCenteredDirtPatch(MapPlan plan, int x, int y)
		{
			// A compact copy of the already centered contested ellipse, not a
			// seed-phased noise slice that can leave only its far right edge.
			var cx = (plan.Width - 1) / 2.0; var cy = (plan.Height - 1) / 2.0;
			return plan.HasFeature((int)Math.Round(cx + (x - cx) / 0.65),
				(int)Math.Round(cy + (y - cy) / 0.65), PlannedFeature.ContestedCenter);
		}

		static bool IsDirtPatch(int seed, int x, int y)
		{
			// Low-frequency deterministic noise produces broad readable patches
			// instead of independent one-cell dirt confetti.
			var value = Math.Sin((x + seed * 0.31) * 0.17) +
				Math.Cos((y - seed * 0.23) * 0.14) +
				0.6 * Math.Sin((x + y + seed) * 0.08);
			return value > 0.55;
		}

		static bool[,] BuildRaisedMask(MapPlan plan)
		{
			var raised = new bool[plan.Width, plan.Height];
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
					raised[x, y] = plan.TerrainAt(x, y) == PlannedTerrain.Mountain;
			return raised;
		}

		static void ApplyAutoTile(Map map, MapPlan plan, int maximumHeight)
		{
			if (!map.Rules.Actors["world"].HasTraitInfo<AutoTileInfo>())
				return;

			var autoTile = new AutoTile(map.Rules.Actors["world"].TraitInfo<AutoTileInfo>());
			var updates = new List<(MPos Position, ushort Template)>();
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var uv = ToMapPosition(x, y, maximumHeight);
					var current = map.Tiles[uv];
					if (!autoTile.IsAutoTileTemplate(current.Type))
						continue;
					var resolved = autoTile.ResolveTemplate(map, uv.ToCPos(map));
					// Native runtime/editor joins replace rectangular grass/sand and grass/dirt masks.
					if (resolved != current.Type && !RubberduckShoreTopology.IsLegacyGrassSand(resolved) && !RubberduckShoreTopology.IsLegacyGrassDirt(resolved))
						updates.Add((uv, resolved));
				}

			foreach (var update in updates)
				map.Tiles[update.Position] = new TerrainTile(update.Template, 0);
		}

		static IReadOnlyCollection<MiniYamlNode> CreateActors(
			Map map, MapPlan plan, MapGenerationOptions options, int maximumHeight)
		{
			var actors = new List<MiniYamlNode>();
			foreach (var spawn in plan.Spawns)
				AddActor(actors, map, "mpspawn", spawn, maximumHeight);
			if (options.Mode == GeneratedMapMode.Operational)
			{
				var bottlenecks = new List<PlanPoint>();
				for (var y = 0; y < plan.Height; y++)
					for (var x = 0; x < plan.Width; x++)
						if (plan.HasFeature(x, y, PlannedFeature.Bottleneck))
							bottlenecks.Add(new PlanPoint(x, y));
				foreach (var spawn in plan.Spawns)
				{
					var rallyPoint = bottlenecks.Count == 0
						? spawn
						: bottlenecks.OrderBy(point =>
							(point.X - spawn.X) * (point.X - spawn.X) + (point.Y - spawn.Y) * (point.Y - spawn.Y)).First();
					AddActor(actors, map, "army.rallypoint", rallyPoint, maximumHeight);
				}
			}
			foreach (var checkpoint in plan.StrategicCheckpoints)
				AddCheckpoint(actors, map, checkpoint, maximumHeight);

			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					if (plan.HasFeature(x, y, PlannedFeature.TechBuilding))
						AddActor(actors, map, "oilb", new PlanPoint(x, y), maximumHeight);
					if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
						AddActor(actors, map, options.Preset == MapGenerationPreset.BattleNexus && plan.HasFeature(x, y, PlannedFeature.ContestedCenter)
							? "splitblue" : "split2", new PlanPoint(x, y), maximumHeight);
				}
			return actors;
		}

		static void AddCheckpoint(
			List<MiniYamlNode> actors, Map map, PlanCheckpoint checkpoint, int maximumHeight)
		{
			var location = ToMapPosition(checkpoint.Point.X, checkpoint.Point.Y, maximumHeight).ToCPos(map);
			var actor = new ActorReference("fcom.checkpoint")
			{
				new LocationInit(location),
				new OwnerInit("Neutral"),
				new HierarchyInit(checkpoint.Hierarchy),
			};
			actors.Add(new MiniYamlNode($"Actor{actors.Count}", actor.Save()));
		}

		static void AddActor(List<MiniYamlNode> actors, Map map, string type, PlanPoint point, int maximumHeight)
		{
			var location = ToMapPosition(point.X, point.Y, maximumHeight).ToCPos(map);
			var actor = new ActorReference(type)
			{
				new LocationInit(location),
				new OwnerInit("Neutral"),
			};
			actors.Add(new MiniYamlNode($"Actor{actors.Count}", actor.Save()));
		}

		static MPos ToMapPosition(int x, int y, int maximumHeight)
		{
			return new MPos(x + 1, y + maximumHeight + 1);
		}

		static string PresetName(MapGenerationPreset preset)
		{
			var name = preset.DisplayName();
			var result = new List<char>();
			for (var i = 0; i < name.Length; i++)
			{
				if (i > 0 && char.IsUpper(name[i]))
					result.Add('-');
				result.Add(char.ToLowerInvariant(name[i]));
			}
			return new string(result.ToArray());
		}
	}
}
