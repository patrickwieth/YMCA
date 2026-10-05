using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	enum GeneratedMapMode
	{
		Tactical,
		Operational,
		Strategic,
	}

	enum MapArchetype
	{
		Strongholds,
		MountainValleys,
		OpenPlains,
		Islands,
	}

	enum ResourceFieldLayout
	{
		None,
		Glitter,
		Patches,
	}

	enum TechBuildingDensity
	{
		None,
		Sparse,
		Dense,
	}

	enum MapGenerationPreset
	{
		MountainValleys,
		TwoSides,
		HillFortress,
		BattleNexus,
		Arabia,
		WaterBasins,
		Continents,
		Archipelago,
		Migration,
	}

	sealed class MapGenerationOptions
	{
		public GeneratedMapMode Mode { get; set; } = GeneratedMapMode.Tactical;
		public MapGenerationPreset Preset { get; set; } = MapGenerationPreset.MountainValleys;
		public MapArchetype Archetype
		{
			get
			{
				switch (Preset)
				{
					case MapGenerationPreset.MountainValleys: return MapArchetype.MountainValleys;
					case MapGenerationPreset.Arabia:
					case MapGenerationPreset.WaterBasins:
						return MapArchetype.OpenPlains;
					case MapGenerationPreset.Continents:
					case MapGenerationPreset.Archipelago:
					case MapGenerationPreset.Migration:
						return MapArchetype.Islands;
					default: return MapArchetype.Strongholds;
				}
			}
		}
		public int Width { get; set; } = 128;
		public int Height { get; set; } = 128;
		public int Players { get; set; } = 8;
		public int Teams { get; set; } = 2;
		public int Seed { get; set; } = 1;
		public int ExpansionsPerPlayer { get; set; } = 1;
		public ResourceFieldLayout Resources { get; set; } = ResourceFieldLayout.Glitter;
		public bool FiniteResources { get; set; }
		public TechBuildingDensity TechBuildings { get; set; } = TechBuildingDensity.None;
		public bool MountainLines { get; set; }
		public bool WaterPatches { get; set; }

		public void Validate()
		{
			if (Width < 64 || Height < 64)
				throw new ArgumentException("Map dimensions must be at least 64 cells.");
			if ((Players < 2 || Players > 12) && Players != 16)
				throw new ArgumentException("Players must be between 2 and 12, or exactly 16.");
			if (ExpansionsPerPlayer < 0 || ExpansionsPerPlayer > 2)
				throw new ArgumentException("Expansions per player must be between 0 and 2.");
			if ((Preset == MapGenerationPreset.TwoSides || Preset == MapGenerationPreset.HillFortress) && Teams != 2)
				throw new ArgumentException("The selected Strongholds preset requires exactly two teams.");
			if (Mode == GeneratedMapMode.Strategic)
			{
				if (Teams != 2)
					throw new ArgumentException("Strategic mode requires exactly two teams.");
				if (Preset == MapGenerationPreset.BattleNexus || Preset == MapGenerationPreset.Continents ||
					Preset == MapGenerationPreset.Archipelago || Preset == MapGenerationPreset.Migration)
					throw new ArgumentException($"Preset {Preset} is not compatible with Strategic mode.");
			}

		}
	}
}
