using System;
using System.IO;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class PrepareLibertyShellmapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--prepare-liberty-shellmap";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("DIRECTORY", "Export a landscaped Liberty Island using the native terrain and shoreline renderer.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var plan = new MapPlan(128, 128) { Seed = 1886 };
			for (var y = 0; y < plan.Height; y++)
				for (var x = 0; x < plan.Width; x++)
				{
					// Align the engineered island banks with native diamond tile axes.
					// Screen-grid ellipses produce alternating half-cell beach spikes.
					var dx = x + (((y + 17) & 1) == 1 ? 0.5 : 0) - 66.5;
					var a = Math.Abs(dx + (y - 62.0) / 2);
					var b = Math.Abs(-dx + (y - 62.0) / 2);
					// Compact rounded rectangle in native cell axes, with a narrow beach.
					var qa = Math.Max(a - 2.8, 0);
					var qb = Math.Max(b - 2.8, 0);
					var edge = Math.Sqrt(qa * qa + qb * qb);
					plan.SetTerrain(x, y, edge < 1.05 ? PlannedTerrain.Land : edge < 1.85 ? PlannedTerrain.ShallowWater : PlannedTerrain.Water);
				}
			var options = new MapGenerationOptions { Width = 128, Height = 128, Players = 2, Teams = 2,
				Preset = MapGenerationPreset.Archipelago, Seed = 1886, Resources = ResourceFieldLayout.None,
				TechBuildings = TechBuildingDensity.None, ExpansionsPerPlayer = 0 };
			Console.WriteLine(new RubberduckMapExporter(utility.ModData).Export(plan, options, Path.GetFullPath(args[1])));
		}
	}
}
