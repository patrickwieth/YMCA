using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class MapPlanGameplayValidator
	{
		public static void Validate(MapPlan plan, MapGenerationOptions options)
		{
			if (plan.Spawns.Count != options.Players)
				throw new InvalidOperationException($"Expected {options.Players} spawns, but generated {plan.Spawns.Count}.");

			var occupiedActorCells = new HashSet<(int X, int Y)>();
			for (var player = 0; player < plan.Spawns.Count; player++)
			{
				var spawn = plan.Spawns[player];
				if (!plan.Contains(spawn.X, spawn.Y) || !plan.IsTraversable(spawn.X, spawn.Y))
					throw new InvalidOperationException($"Player {player + 1} spawn is not on traversable terrain.");
				if (!plan.HasFeature(spawn.X, spawn.Y, PlannedFeature.Spawn))
					throw new InvalidOperationException($"Player {player + 1} spawn is missing its Spawn feature.");
				if (!occupiedActorCells.Add((spawn.X, spawn.Y)))
					throw new InvalidOperationException($"Multiple actors occupy spawn cell {spawn.X},{spawn.Y}.");
			}

			var resourceCells = new List<PlanPoint>();
			var techBuildings = 0;
			var resourceGenerators = 0;
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					var features = plan.FeaturesAt(x, y);
					var hasResource = (features & (PlannedFeature.Resource | PlannedFeature.RichResource)) != 0;
					if (hasResource)
						resourceCells.Add(new PlanPoint(x, y));

					if ((features & PlannedFeature.ResourceGenerator) != 0)
					{
						resourceGenerators++;
						ValidateActorCell(plan, occupiedActorCells, x, y, "resource generator");
					}

					if ((features & PlannedFeature.TechBuilding) != 0)
					{
						techBuildings++;
						ValidateActorCell(plan, occupiedActorCells, x, y, "Tech Building");
					}

					if (hasResource && !plan.IsTraversable(x, y))
						throw new InvalidOperationException($"Resource at {x},{y} is not on traversable terrain.");
				}

			if (options.FiniteResources && resourceGenerators != 0)
				throw new InvalidOperationException("Finite-resources map contains resource generators.");
			if (options.Resources == ResourceFieldLayout.None && (resourceCells.Count != 0 || resourceGenerators != 0))
				throw new InvalidOperationException("Resources=None map contains resources.");
			if (options.TechBuildings == TechBuildingDensity.None && techBuildings != 0)
				throw new InvalidOperationException("TechBuildings=None map contains Tech Buildings.");
			if (options.Mode == GeneratedMapMode.Strategic)
				ValidateStrategicCheckpoints(plan);
			else if (plan.StrategicCheckpoints.Count != 0)
				throw new InvalidOperationException("Non-Strategic map contains Strategic checkpoints.");

			plan.ValidationMessages.Add($"Gameplay structure validated: {plan.Spawns.Count} unique traversable starts and no actor overlaps.");
			if (resourceCells.Count > 0)
				AddResourceDistanceReport(plan, resourceCells);
			else
				plan.ValidationMessages.Add("No resource fields requested or generated.");
			plan.ValidationMessages.Add($"Economy actors: {resourceGenerators} resource generators and {techBuildings} Tech Buildings.");
		}

		static void ValidateStrategicCheckpoints(MapPlan plan)
		{
			var expectedCount = Math.Min(plan.Width, plan.Height) switch
			{
				<= 96 => 3,
				<= 144 => 4,
				_ => 5,
			};
			if (plan.StrategicCheckpoints.Count != expectedCount)
				throw new InvalidOperationException(
					$"Expected {expectedCount} Strategic checkpoints, but generated {plan.StrategicCheckpoints.Count}.");

			for (var hierarchy = 0; hierarchy < expectedCount; hierarchy++)
			{
				var matches = plan.StrategicCheckpoints.Where(candidate => candidate.Hierarchy == hierarchy).ToArray();
				if (matches.Length != 1)
					throw new InvalidOperationException($"Strategic checkpoint hierarchy {hierarchy} is not unique.");
				var checkpoint = matches[0];
				if (!plan.Contains(checkpoint.Point.X, checkpoint.Point.Y) ||
					plan.TerrainAt(checkpoint.Point.X, checkpoint.Point.Y) != PlannedTerrain.Land)
					throw new InvalidOperationException($"Strategic checkpoint hierarchy {hierarchy} is not on land.");
			}

		}

		static void ValidateActorCell(
			MapPlan plan, HashSet<(int X, int Y)> occupiedActorCells, int x, int y, string actorDescription)
		{
			if (!plan.IsTraversable(x, y))
				throw new InvalidOperationException($"{actorDescription} at {x},{y} is not on traversable terrain.");
			if (!occupiedActorCells.Add((x, y)))
				throw new InvalidOperationException($"Multiple actors occupy {x},{y}.");
		}

		static void AddResourceDistanceReport(MapPlan plan, List<PlanPoint> resources)
		{
			var nearestDistances = new List<double>();
			foreach (var spawn in plan.Spawns)
			{
				var nearestSquared = resources.Min(resource =>
				{
					var dx = resource.X - spawn.X;
					var dy = resource.Y - spawn.Y;
					return dx * dx + dy * dy;
				});
				nearestDistances.Add(Math.Sqrt(nearestSquared));
			}

			var minimum = nearestDistances.Min();
			var maximum = nearestDistances.Max();
			var average = nearestDistances.Average();
			plan.ValidationMessages.Add(string.Format(
				CultureInfo.InvariantCulture,
				"Nearest-resource distance across starts: min {0:0.0}, average {1:0.0}, max {2:0.0} cells.",
				minimum, average, maximum));
		}
	}
}
