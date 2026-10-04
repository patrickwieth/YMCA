using System;
using System.IO;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.MapGeneration
{
	sealed class MapPlanDebugRenderer
	{
		const int Scale = 4;

		readonly struct DebugColor
		{
			public readonly byte R;
			public readonly byte G;
			public readonly byte B;
			public readonly byte A;

			public DebugColor(byte r, byte g, byte b, byte a = 255)
			{
				R = r;
				G = g;
				B = b;
				A = a;
			}
		}

		static readonly DebugColor Land = new(83, 125, 62);
		static readonly DebugColor Mountain = new(82, 76, 69);
		static readonly DebugColor Water = new(48, 105, 160);
		static readonly DebugColor ShallowWater = new(78, 164, 184);
		static readonly DebugColor Expansion = new(177, 158, 69);
		static readonly DebugColor VacantHome = new(145, 145, 145);
		static readonly DebugColor Enclosed = new(39, 62, 38);
		static readonly DebugColor Entrance = new(225, 52, 180);
		static readonly DebugColor Resource = new(232, 194, 45);
		static readonly DebugColor RichResource = new(42, 135, 232);
		static readonly DebugColor ResourceGenerator = new(239, 112, 32);
		static readonly DebugColor TechBuilding = new(238, 238, 238);
		static readonly DebugColor Bottleneck = new(190, 72, 54);
		static readonly DebugColor ContestedCenter = new(139, 91, 62);
		static readonly DebugColor LaunchShore = new(68, 205, 214);
		static readonly DebugColor CliffShore = new(45, 48, 58);
		static readonly DebugColor DestructibleBridge = new(184, 144, 92);
		static readonly DebugColor StrategicCheckpoint = new(255, 80, 220);
		static readonly DebugColor Unreachable = new(173, 40, 40);
		static readonly DebugColor[] PlayerColors =
		{
			new(225, 54, 54),
			new(69, 125, 230),
			new(64, 190, 102),
			new(225, 191, 48),
			new(164, 88, 210),
			new(43, 191, 196),
			new(230, 125, 42),
			new(222, 92, 161),
		};

		public void SaveAll(MapPlan plan, string directory)
		{
			Directory.CreateDirectory(directory);
			var suffix = $"-players-{plan.Spawns.Count}-seed-{plan.Seed}";
			Save(plan, Path.Combine(directory, $"01-regions{suffix}.png"), RegionColor);
			Save(plan, Path.Combine(directory, $"02-terrain-plan{suffix}.png"), TerrainColor);
			Save(plan, Path.Combine(directory, $"03-connectivity{suffix}.png"), ConnectivityColor(plan));
			Save(plan, Path.Combine(directory, $"04-resources{suffix}.png"), ResourceColor);
			SaveFinal(plan, directory);
		}

		public void SaveFinal(MapPlan plan, string directory)
		{
			Directory.CreateDirectory(directory);
			var suffix = $"-players-{plan.Spawns.Count}-seed-{plan.Seed}";
			Save(plan, Path.Combine(directory, $"final-plan{suffix}.png"), CompositeColor);
			if (plan.Plateaus.Count > 0)
			{
				var surfaces = new StringBuilder("x\ty\theight\tramp\tblocked\n");
				foreach (var pair in plan.PlateauSurfaces)
					surfaces.AppendLine($"{pair.Key.U}\t{pair.Key.V}\t{pair.Value.Height}\t{pair.Value.Ramp}\t{pair.Value.Blocked}");
				File.WriteAllText(Path.Combine(directory, "plateau-surfaces.tsv"), surfaces.ToString());
				var routes = new StringBuilder("approach-x\tapproach-y\tlanding-x\tlanding-y\tramp\n");
				foreach (var plateau in plan.Plateaus)
					routes.AppendLine($"{plateau.Approach.X}\t{plateau.Approach.Y}\t{plateau.Landing.X}\t{plateau.Landing.Y}\t{plateau.Ramp}");
				File.WriteAllText(Path.Combine(directory, "plateau-routes.tsv"), routes.ToString());
				var native = new StringBuilder("x\ty\tsource-slot\n");
				foreach (var piece in plan.NativeCliffPieces)
					native.AppendLine($"{piece.Point.U}\t{piece.Point.V}\t{piece.Slot}");
				File.WriteAllText(Path.Combine(directory, "native-cliff-pieces.tsv"), native.ToString());
			}
		}

		static Func<MapPlan, int, int, DebugColor> ConnectivityColor(MapPlan plan)
		{
			var reachable = MountainValleysGenerator.FindReachable(plan, plan.Spawns[0]);
			return (p, x, y) =>
			{
				if (p.TerrainAt(x, y) == PlannedTerrain.Mountain)
					return Mountain;
				if (p.TerrainAt(x, y) == PlannedTerrain.Water)
					return Water;
				if (!reachable[x + y * p.Width])
					return Unreachable;
				if (p.HasFeature(x, y, PlannedFeature.Bottleneck))
					return Bottleneck;
				if (p.HasFeature(x, y, PlannedFeature.Entrance))
					return Entrance;
				return Land;
			};
		}

		static DebugColor TerrainColor(MapPlan plan, int x, int y)
		{
			if (plan.PlateauSurfaces.TryGetValue(new MPos(x, y), out var surface))
				return surface.Blocked ? Mountain : surface.Ramp != 0 ? new DebugColor(240, 150, 50) : new DebugColor(140, 175, 105);
			return plan.TerrainAt(x, y) switch
			{
				PlannedTerrain.Mountain => Mountain,
				PlannedTerrain.Water => Water,
				PlannedTerrain.ShallowWater => ShallowWater,
				_ => Land,
			};
		}

		static DebugColor RegionColor(MapPlan plan, int x, int y)
		{
			if (plan.TerrainAt(x, y) == PlannedTerrain.Mountain)
				return Mountain;
			if (plan.TerrainAt(x, y) == PlannedTerrain.Water)
				return Water;
			if (plan.HasFeature(x, y, PlannedFeature.Spawn))
				return new DebugColor(255, 255, 255);
			if (plan.HasFeature(x, y, PlannedFeature.Bottleneck))
				return Bottleneck;
			if (plan.HasFeature(x, y, PlannedFeature.ContestedCenter))
				return ContestedCenter;
			if (plan.HasFeature(x, y, PlannedFeature.LaunchShore))
				return LaunchShore;
			if (plan.HasFeature(x, y, PlannedFeature.CliffShore))
				return CliffShore;
			if (plan.HasFeature(x, y, PlannedFeature.Entrance))
				return Entrance;
			if (plan.HasFeature(x, y, PlannedFeature.Expansion))
				return Expansion;
			if (plan.HasFeature(x, y, PlannedFeature.VacantHome))
				return VacantHome;
			if (plan.HasFeature(x, y, PlannedFeature.Enclosed))
				return Enclosed;

			var owner = plan.HomeOwnerAt(x, y);
			return owner >= 0 ? PlayerColors[owner % PlayerColors.Length] : Land;
		}

		static DebugColor ResourceColor(MapPlan plan, int x, int y)
		{
			if (plan.TerrainAt(x, y) != PlannedTerrain.Land)
				return TerrainColor(plan, x, y);
			if (plan.HasFeature(x, y, PlannedFeature.TechBuilding))
				return TechBuilding;
			if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
				return ResourceGenerator;
			if (plan.HasFeature(x, y, PlannedFeature.RichResource))
				return RichResource;
			if (plan.HasFeature(x, y, PlannedFeature.Resource))
				return Resource;
			return Land;
		}

		static DebugColor CompositeColor(MapPlan plan, int x, int y)
		{
			foreach (var checkpoint in plan.StrategicCheckpoints)
				if (checkpoint.Point.X == x && checkpoint.Point.Y == y)
					return StrategicCheckpoint;
			if (plan.HasFeature(x, y, PlannedFeature.DestructibleBridge))
				return DestructibleBridge;
			if (plan.TerrainAt(x, y) != PlannedTerrain.Land)
				return TerrainColor(plan, x, y);
			if (plan.HasFeature(x, y, PlannedFeature.Spawn))
				return new DebugColor(255, 255, 255);
			if (plan.HasFeature(x, y, PlannedFeature.TechBuilding))
				return TechBuilding;
			if (plan.HasFeature(x, y, PlannedFeature.ResourceGenerator))
				return ResourceGenerator;
			if (plan.HasFeature(x, y, PlannedFeature.Bottleneck))
				return Bottleneck;
			if (plan.HasFeature(x, y, PlannedFeature.RichResource))
				return RichResource;
			if (plan.HasFeature(x, y, PlannedFeature.ContestedCenter))
				return ContestedCenter;
			if (plan.HasFeature(x, y, PlannedFeature.LaunchShore))
				return LaunchShore;
			if (plan.HasFeature(x, y, PlannedFeature.CliffShore))
				return CliffShore;
			if (plan.HasFeature(x, y, PlannedFeature.Entrance))
				return Entrance;
			if (plan.HasFeature(x, y, PlannedFeature.Resource))
				return Resource;
			if (plan.HasFeature(x, y, PlannedFeature.Expansion))
				return Expansion;
			if (plan.HasFeature(x, y, PlannedFeature.VacantHome))
				return VacantHome;
			if (plan.HasFeature(x, y, PlannedFeature.Enclosed))
				return Enclosed;

			var owner = plan.HomeOwnerAt(x, y);
			return owner >= 0 ? PlayerColors[owner % PlayerColors.Length] : Land;
		}

		static void Save(MapPlan plan, string path, Func<MapPlan, int, int, DebugColor> colorFor)
		{
			var width = plan.Width * Scale;
			var height = plan.Height * Scale;
			var data = new byte[width * height * 4];
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var color = colorFor(plan, x, y);
					for (var sy = 0; sy < Scale; sy++)
						for (var sx = 0; sx < Scale; sx++)
						{
							var index = ((x * Scale + sx) + (y * Scale + sy) * width) * 4;
							data[index] = color.R;
							data[index + 1] = color.G;
							data[index + 2] = color.B;
							data[index + 3] = color.A;
						}
				}

			new Png(data, SpriteFrameType.Rgba32, width, height).Save(path);
		}
	}
}
