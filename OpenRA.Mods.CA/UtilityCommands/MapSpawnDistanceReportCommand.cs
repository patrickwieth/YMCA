using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class MapSpawnDistanceReportCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--map-spawn-distance-report";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("OUTPUT.json", "Measure current auto-sized Tactical layouts, seeds 1/42/43, for 2 and 4 players; horizontal world distances, not routes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rows = new List<object>();
			foreach (MapGenerationPreset preset in Enum.GetValues(typeof(MapGenerationPreset)))
			foreach (var players in new[] { 2, 4 })
			foreach (var seed in new[] { 1, 42, 43 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "report", ".", "mode=Tactical", $"preset={preset}",
					$"players={players}", "teams=2", "size=auto", $"seed={seed}" });
				try
				{
					var plan = MapPlanGenerator.Generate(options);
					var border = utility.ModData.Manifest.Get<MapGrid>().MaximumTerrainHeight;
					using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], plan.Width + 2, plan.Height + border + 2);
					var starts = plan.Spawns.Select(p => new MPos(p.X + 1, p.Y + border + 1).ToCPos(map)).ToArray();
					var pairs = new List<object>();
					for (var i = 0; i < starts.Length; i++)
					for (var j = i + 1; j < starts.Length; j++)
					{
						var d = map.CenterOfCell(starts[i]) - map.CenterOfCell(starts[j]);
						var paths = StartBalanceMetrics.Distances(plan, plan.Spawns[i]);
						var destination = plan.Spawns[j];
						pairs.Add(new { A = i + 1, B = j + 1, Cells = Math.Sqrt((double)d.X * d.X + (double)d.Y * d.Y) / 1024,
							CardinalGroundSteps = paths[destination.X + destination.Y * plan.Width] });
					}
					rows.Add(new { Preset = preset.DisplayName(), Players = players, Seed = seed, plan.GenerationAttempt,
						Width = plan.Width - 2 * plan.PlayableInset, Height = plan.Height - 2 * plan.PlayableInset,
						Starts = starts.Select(c => new { c.X, c.Y }).ToArray(), Pairs = pairs });
				}
				catch (Exception e) { rows.Add(new { Preset = preset.DisplayName(), Players = players, Seed = seed, Error = e.Message }); }
				File.WriteAllText(args[1], JsonConvert.SerializeObject(rows, Formatting.Indented));
				Console.WriteLine($"Measured {preset} {players}P seed {seed}");
			}
		}
	}
}
