using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.MapGeneration
{
	enum PlannedTerrain : byte
	{
		Land,
		Mountain,
		Water,
		ShallowWater,
	}

	[Flags]
	enum PlannedFeature : ushort
	{
		None = 0,
		Home = 1,
		Expansion = 2,
		Enclosed = 4,
		Entrance = 8,
		Resource = 16,
		ResourceGenerator = 32,
		TechBuilding = 64,
		Spawn = 128,
		BuildClearance = 256,
		VacantHome = 512,
		Bottleneck = 1024,
		LaunchShore = 2048,
		CliffShore = 4096,
		ContestedCenter = 8192,
		RichResource = 16384,
		DestructibleBridge = 32768,
	}

	readonly struct PlanCheckpoint
	{
		public readonly PlanPoint Point;
		public readonly int Hierarchy;

		public PlanCheckpoint(PlanPoint point, int hierarchy)
		{
			Point = point;
			Hierarchy = hierarchy;
		}
	}

	readonly struct PlanPoint
	{
		public readonly int X;
		public readonly int Y;

		public PlanPoint(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	sealed class MapPlan
	{
		readonly PlannedTerrain[] terrain;
		readonly PlannedFeature[] features;
		readonly sbyte[] homeOwners;

		public int Width { get; }
		public int Height { get; }
		public int Seed { get; internal set; }
		public int GenerationAttempt { get; internal set; }
		public int SymmetrySlotCount { get; internal set; }
		public bool RequireClosedPlateaus { get; internal set; }
		public List<PlanPoint> Spawns { get; } = new();
		public List<PlannedRubberduckBridge> Bridges { get; } = new();
		public List<PlanPoint> ExpansionCenters { get; } = new();
		public List<PlanCheckpoint> StrategicCheckpoints { get; } = new();
		public Dictionary<MPos, PlateauSurface> PlateauSurfaces { get; } = new();
		public List<PlannedPlateau> Plateaus { get; } = new();
		public HashSet<(MPos Point, int Direction)> NativeCliffFaces { get; } = new();
		public List<(MPos Point, int Slot)> NativeCliffPieces { get; } = new();
		public List<string> ValidationMessages { get; } = new();

		public MapPlan(int width, int height)
		{
			Width = width;
			Height = height;
			terrain = new PlannedTerrain[width * height];
			features = new PlannedFeature[width * height];
			homeOwners = new sbyte[width * height];
			Array.Fill(homeOwners, (sbyte)-1);
		}

		internal MapPlan Clone()
		{
			var copy = new MapPlan(Width, Height)
			{
				Seed = Seed, GenerationAttempt = GenerationAttempt,
				SymmetrySlotCount = SymmetrySlotCount, RequireClosedPlateaus = RequireClosedPlateaus,
			};
			Array.Copy(terrain, copy.terrain, terrain.Length);
			Array.Copy(features, copy.features, features.Length);
			Array.Copy(homeOwners, copy.homeOwners, homeOwners.Length);
			copy.Bridges.AddRange(Bridges);
			copy.Spawns.AddRange(Spawns); copy.ExpansionCenters.AddRange(ExpansionCenters);
			copy.StrategicCheckpoints.AddRange(StrategicCheckpoints); copy.Plateaus.AddRange(Plateaus);
			foreach (var pair in PlateauSurfaces) copy.PlateauSurfaces.Add(pair.Key, pair.Value);
			copy.NativeCliffFaces.UnionWith(NativeCliffFaces); copy.NativeCliffPieces.AddRange(NativeCliffPieces);
			copy.ValidationMessages.AddRange(ValidationMessages);
			return copy;
		}

		public PlannedTerrain TerrainAt(int x, int y)
		{
			return terrain[Index(x, y)];
		}

		public void SetTerrain(int x, int y, PlannedTerrain value)
		{
			terrain[Index(x, y)] = value;
		}

		public PlannedFeature FeaturesAt(int x, int y)
		{
			return features[Index(x, y)];
		}

		public bool HasFeature(int x, int y, PlannedFeature feature)
		{
			return (features[Index(x, y)] & feature) != 0;
		}

		public void AddFeature(int x, int y, PlannedFeature feature)
		{
			features[Index(x, y)] |= feature;
		}

		public void RemoveFeature(int x, int y, PlannedFeature feature)
		{
			features[Index(x, y)] &= ~feature;
		}

		public int HomeOwnerAt(int x, int y)
		{
			return homeOwners[Index(x, y)];
		}

		public void SetHomeOwner(int x, int y, int player)
		{
			homeOwners[Index(x, y)] = (sbyte)player;
			AddFeature(x, y, PlannedFeature.Home);
		}

		public bool Contains(int x, int y)
		{
			return x >= 0 && y >= 0 && x < Width && y < Height;
		}

		public bool IsTraversable(int x, int y)
		{
			if (!Contains(x, y))
				return false;

			var value = TerrainAt(x, y);
			return value == PlannedTerrain.Land || value == PlannedTerrain.ShallowWater;
		}

		int Index(int x, int y)
		{
			if (!Contains(x, y))
				throw new ArgumentOutOfRangeException($"Cell {x},{y} is outside the map.");

			return x + y * Width;
		}
	}
}
