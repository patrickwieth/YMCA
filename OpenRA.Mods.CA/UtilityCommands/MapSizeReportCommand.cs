using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class MapSizeReportCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--map-size-report";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;

		[Desc("OUTPUT.json", "Report current Tactical tournament auto-size settings without generating or changing maps.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var rows = new List<object>();
			foreach (MapGenerationPreset preset in Enum.GetValues(typeof(MapGenerationPreset)))
				foreach (var players in new[] { 2, 4 })
				{
					// Use exactly the option path used by GenerateTournamentMapCommand.
					var options = DebugMapGeneratorCommand.ParseOptions(new[] { "tournament", ".", "mode=Tactical",
						"preset=" + preset, "seed=1", "players=" + players, "teams=2", "size=auto" });
					rows.Add(new
					{
						Preset = preset.ToString(), Name = preset.DisplayName(), Players = players,
						PlayableWidth = options.Width, PlayableHeight = options.Height,
						RectangleCells = options.Width * options.Height,
						Resources = options.Resources.ToString(), options.FiniteResources,
						TechBuildings = options.TechBuildings.ToString(),
					});
				}

			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			File.WriteAllText(output, JsonConvert.SerializeObject(new
			{
				Scope = "Current Tactical tournament defaults; bounds rectangles, not traversable-land area or travel time.",
				Grid = utility.ModData.Manifest.Get<MapGrid>().Type.ToString(),
				Rows = rows,
			}, Formatting.Indented));
			Console.WriteLine("Map size report: " + output);
		}
	}
}
