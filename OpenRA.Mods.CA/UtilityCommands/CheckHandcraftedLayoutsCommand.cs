using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckHandcraftedLayoutsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-handcrafted-layouts";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;

		[Desc("LAYOUT_DIRECTORY OUTPUT.json", "Validate original-to-isometric cells against the native engine geometry; no runtime map installation.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var reports = new List<object>();
			foreach (var file in Directory.GetFiles(args[1], "*.layout.json").OrderBy(f => f))
			{
				var layout = JObject.Parse(File.ReadAllText(file));
				var size = layout["target_map_size"].Values<int>().ToArray();
				var bounds = layout["target_enclosing_bounds"].Values<int>().ToArray();
				var translation = layout["cell_translation"].Values<int>().ToArray();
				using var map = new Map(utility.ModData, (DefaultTerrain)utility.ModData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], size[0], size[1]);
				map.SetBounds(new PPos(bounds[0], bounds[1]), new PPos(bounds[0] + bounds[2] - 1, bounds[1] + bounds[3] - 1));
				if (map.Grid.Type != MapGridType.RectangularIsometric)
					throw new InvalidDataException("Expected native isometric map grid.");

				var cells = new HashSet<CPos>();
				var original = new HashSet<CPos>();
				foreach (var row in (JArray)layout["cells"])
				{
					var old = new CPos((int)row[0], (int)row[1]);
					var cell = new CPos((int)row[2], (int)row[3]);
					var memory = new MPos((int)row[4], (int)row[5]);
					if (!cells.Add(cell) || !original.Add(old) || memory.ToCPos(map) != cell || cell.ToMPos(map) != memory || !map.Contains(cell))
						throw new InvalidDataException("Invalid native mapping or bounds: " + cell);
					if (cell != old + new CVec(translation[0], translation[1]))
						throw new InvalidDataException("Logical coordinate translation changed.");
					var center = map.CenterOfCell(cell);
					if (center.X != 724 * (cell.X - cell.Y + 1) || center.Y != 724 * (cell.X + cell.Y + 1) || center.Z != 0)
						throw new InvalidDataException("Native world geometry differs from audited conversion.");
				}

				var edges = 0;
				foreach (var old in original)
					for (var x = -1; x <= 1; x++)
						for (var y = -1; y <= 1; y++)
						{
							if (x == 0 && y == 0)
								continue;

							var next = old + new CVec(x, y);
							var before = original.Contains(next);
							if (cells.Contains(next + new CVec(translation[0], translation[1])) != before)
								throw new InvalidDataException("Native neighbour topology differs from original.");
							if (before)
								edges++;
						}

				if (edges != (int)layout["verified_directed_neighbour_edges"])
					throw new InvalidDataException("Edge inventory mismatch.");

				var missingActors = new HashSet<string>();
				foreach (var actor in (JArray)layout["actors"])
				{
					var type = ((string)actor["type"]).ToLowerInvariant();
					if (!utility.ModData.DefaultRules.Actors.ContainsKey(type))
						missingActors.Add(type);
					if (actor["target_cell"] == null)
						continue;

					var p = actor["target_cell"].Values<int>().ToArray();
					var uv = actor["target_memory"].Values<int>().ToArray();
					var cell = new CPos(p[0], p[1]);
					if (new MPos(uv[0], uv[1]).ToCPos(map) != cell || uv[0] < 0 || uv[1] < 0 || uv[0] >= size[0] || uv[1] >= size[1])
						throw new InvalidDataException("Actor outside converted backing: " + actor["id"]);
				}

				reports.Add(new
				{
					Map = (string)layout["slug"], Cells = cells.Count, NeighbourEdges = edges,
					NativeGridRoundtrip = true, NativeFlatWorldCoordinates = true,
					MissingCurrentActorTypes = missingActors.OrderBy(a => a).ToArray(),
					Scope = "Geometry only. Terrain rendering, actor footprints and live locomotion remain to validate before release.",
				});
				Console.WriteLine($"PASS native layout: {layout["slug"]}, {cells.Count} cells, {edges} edges; missing actor types: {string.Join(", ", missingActors)}");
			}

			if (reports.Count != 3)
				throw new InvalidDataException("Expected exactly three handcrafted layouts.");
			var output = Path.GetFullPath(args[2]);
			Directory.CreateDirectory(Path.GetDirectoryName(output));
			File.WriteAllText(output, JsonConvert.SerializeObject(reports, Formatting.Indented));
		}
	}
}
