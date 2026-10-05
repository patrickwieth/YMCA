using System;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class MapPlanGenerator
	{
		public static MapPlan Generate(MapGenerationOptions options)
		{
			options.Validate();
			if (options.Preset == MapGenerationPreset.MountainValleys && options.Mode == GeneratedMapMode.Strategic)
				return GenerateStrategicMountains(options);
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

		static MapPlan GenerateStrategicMountains(MapGenerationOptions options)
		{
			InvalidOperationException lastPlacementFailure = null;
			var attempt = 0;
			while (attempt < MountainValleysGenerator.MaximumAttempts)
			{
				var plan = new MountainValleysGenerator().Generate(options, attempt);
				try
				{
					StrategicCheckpointPlanner.Place(plan, options);
				}
				catch (InvalidOperationException e)
				{
					// Checkpoint clearance is part of topology feasibility. Retry a complete
					// deterministic layout instead of flattening a plateau or blocking its ramp.
					lastPlacementFailure = e;
					attempt = plan.GenerationAttempt + 1;
					continue;
				}
				MapPlanGameplayValidator.Validate(plan, options);
				return plan;
			}
			throw new InvalidOperationException("No Mountain Valleys layout fits the protected plateaus and Strategic checkpoint route.", lastPlacementFailure);
		}
	}
}
