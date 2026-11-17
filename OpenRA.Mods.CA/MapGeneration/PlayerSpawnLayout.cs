using System;
using System.Collections.Generic;
using OpenRA.Support;

namespace OpenRA.Mods.CA.MapGeneration
{
	static class PlayerSpawnLayout
	{
		public static List<PlanPoint> Create(MapPlan plan, int playerCount, int radius, MersenneTwister random)
		{
			var centers = new List<PlanPoint>();
			var symmetryOrder = SymmetryOrder(playerCount);
			var phase = symmetryOrder == 4 ? -Math.PI * 3 / 4 : random.NextFloat() * Math.PI * 2;
			for (var sector = 0; sector < symmetryOrder; sector++)
			{
				var angle = phase + sector * Math.PI * 2 / symmetryOrder;
				centers.Add(Project(plan, angle, radius, 1));
			}

			var intermediateCounts = IntermediateCounts(playerCount);
			for (var sector = 0; sector < intermediateCounts.Length; sector++)
			{
				var count = intermediateCounts[sector];
				for (var slot = 0; slot < count; slot++)
				{
					var fraction = (slot + 1.0) / (count + 1.0);
					var angle = phase + (sector + fraction) * Math.PI * 2 / symmetryOrder;
					var radialPosition = count == 1 ? 0.72 : 0.64 + slot * 0.16;
					centers.Add(Project(plan, angle, radius, radialPosition));
				}
			}

			return centers;
		}

		public static int SymmetryOrder(int playerCount)
		{
			switch (playerCount)
			{
				case 2: return 2;
				case 3: return 3;
				case 4: return 4;
				case 5: return 5;
				case 6: return 4;
				case 7: return 7;
				case 8: return 4;
				case 9:
				case 10:
					return 5;
				case 11:
				case 12:
				case 16:
					return 4;
				default:
					throw new ArgumentOutOfRangeException(nameof(playerCount));
			}
		}

		static int[] IntermediateCounts(int playerCount)
		{
			switch (playerCount)
			{
				case 6: return new[] { 1, 0, 1, 0 };
				case 8: return new[] { 1, 1, 1, 1 };
				case 9:
				case 10:
					return new[] { 1, 1, 1, 1, 1 };
				case 11:
				case 12:
					return new[] { 2, 2, 2, 2 };
				case 16:
					return new[] { 3, 3, 3, 3 };
				default: return Array.Empty<int>();
			}
		}

		static PlanPoint Project(MapPlan plan, double angle, int radius, double radialPosition)
		{
			var margin = radius + 3;
			var centerX = (plan.Width - 1) / 2.0;
			var centerY = (plan.Height - 1) / 2.0;
			var dx = Math.Cos(angle);
			var dy = Math.Sin(angle);
			var scaleX = ((plan.Width - 1) / 2.0 - margin) / Math.Max(0.0001, Math.Abs(dx));
			var scaleY = ((plan.Height - 1) / 2.0 - margin) / Math.Max(0.0001, Math.Abs(dy));
			var scale = Math.Min(scaleX, scaleY) * radialPosition;
			return new PlanPoint(
				(int)Math.Round(centerX + dx * scale),
				(int)Math.Round(centerY + dy * scale));
		}
	}
}
