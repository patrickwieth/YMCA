using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class MapPlanGenerator
	{
		public static MapPlan Generate(MapGenerationOptions options)
		{
			options.Validate();
			if (options.Custom?.Topology == CustomTopology.OpenPlains)
			{
				options.MountainLines = options.Custom.Plains == PlainsSubtype.Rocky;
				options.WaterPatches = options.Custom.Plains == PlainsSubtype.Ponds;
			}
			if (options.Custom?.River == true && !options.Custom.BattleInTheCenter)
			{
				var rivers = new CustomRiverGenerator().Generate(options);
				StrategicCheckpointPlanner.Place(rivers, options);
				MapPlanGameplayValidator.Validate(rivers, options);
				return rivers;
			}
			if (options.Preset == MapGenerationPreset.TwoSides ||
				options.Preset == MapGenerationPreset.MountainValleys && options.Mode == GeneratedMapMode.Strategic)
				return GenerateProtectedLayout(options);
			var plan = options.Preset switch
			{
				MapGenerationPreset.TwoSides => new TwoSidesGenerator().Generate(options),
				MapGenerationPreset.HillFortress => new HillFortressGenerator().Generate(options),
				MapGenerationPreset.BattleNexus => new BattleNexusGenerator().Generate(options),
				MapGenerationPreset.Arabia or MapGenerationPreset.WaterBasins => new OpenPlainsGenerator().Generate(options),
				MapGenerationPreset.Continents or MapGenerationPreset.Archipelago or MapGenerationPreset.Migration => new IslandsGenerator().Generate(options),
				_ => new MountainValleysGenerator().Generate(options),
			};
			StrategicCheckpointPlanner.Place(plan, options);
			if (options.Preset != MapGenerationPreset.WaterBasins) RubberduckCoastTopology.Mark(plan, options);
			MapPlanGameplayValidator.Validate(plan, options);
			return plan;
		}

		static MapPlan GenerateProtectedLayout(MapGenerationOptions options)
		{
			InvalidOperationException lastPlacementFailure = null;
			var attempt = 0;
			var twoSides = options.Preset == MapGenerationPreset.TwoSides;
			var limit = twoSides ? TwoSidesGenerator.MaximumAttempts : MountainValleysGenerator.MaximumAttempts;
			while (attempt < limit)
			{
				var plan = twoSides ? new TwoSidesGenerator().Generate(options, attempt) :
					new MountainValleysGenerator().Generate(options, attempt);
				try
				{
					StrategicCheckpointPlanner.Place(plan, options);
					if (twoSides) RubberduckMapExporter.ValidateTwoSidesCoasts(plan, options);
				}
				catch (InvalidOperationException e)
				{
					// Checkpoints and boundary-reaching banks are topology constraints.
					// Retry a complete layout, never fill water or isolate land to make art fit.
					lastPlacementFailure = e;
					attempt = plan.GenerationAttempt + 1;
					continue;
				}
				if (twoSides && options.Custom?.HasCenter == true)
					for (var y = 0; y < plan.Height; y++) for (var x = 0; x < plan.Width; x++)
						if (plan.TerrainAt(x, y) == PlannedTerrain.ShallowWater && plan.HasFeature(x, y, PlannedFeature.Bottleneck))
							plan.AddFeature(x, y, PlannedFeature.ContestedCenter);
				MapPlanGameplayValidator.Validate(plan, options);
				return plan;
			}
			throw new InvalidOperationException($"No {options.Preset} layout fits the protected terrain/economy, coast and mode objectives.", lastPlacementFailure);
		}
	}
}
