using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--export-allied-selection-geometry", "Export resolved Allied selection and idle PNG registration on flat isometric terrain.")]
	public sealed class ExportAlliedSelectionGeometryCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-allied-selection-geometry";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var grid = utility.ModData.Manifest.Get<MapGrid>();
			if (grid.Type != MapGridType.RectangularIsometric)
				throw new InvalidDataException("This diagnostic requires the isometric grid.");
			var sequences = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Sequences, null)
				.ToDictionary(n => n.Key, n => n.Value);
			var report = new List<object>();
			foreach (var name in new[] { "fact", "proc", "weap", "pbox", "pris", "powr", "apwr", "tent", "gap", "orep", "pdox", "weat", "atek", "hpad", "dome", "fix", "syrd", "afld.allies" })
			{
				var actor = utility.ModData.DefaultRules.Actors[name];
				var building = actor.TraitInfo<BuildingInfo>();
				var selection = actor.TraitInfo<IsometricSelectableInfo>();
				var bounds = selection.DecorationBounds ?? selection.Bounds;
				var height = selection.DecorationHeight >= 0 ? selection.DecorationHeight : selection.Height;
				var cells = building.FootprintTiles(CPos.Zero, FootprintCellType.Occupied)
					.Concat(building.FootprintTiles(CPos.Zero, FootprintCellType.OccupiedUntargetable)).ToArray();
				if (cells.Length == 0)
					cells = building.OccupiedTiles(CPos.Zero).ToArray();
				var xMin = cells.Min(c => c.X);
				var xMax = cells.Max(c => c.X);
				var yMin = cells.Min(c => c.Y);
				var yMax = cells.Max(c => c.Y);
				var bodyCells = new[] { xMax - xMin + 1, yMax - yMin + 1 };
				int[] left, right, top, bottom;
				if (bounds != null)
				{
					// Mirror IsometricSelectable.Bounds integer divisions, including odd widths.
					var ox = bounds.Length >= 4 ? bounds[2] * grid.TileSize.Width / grid.TileScale : 0;
					var oy = bounds.Length >= 4 ? bounds[3] * grid.TileSize.Height / grid.TileScale : 0;
					left = new[] { ox - bounds[0] * grid.TileSize.Width / (2 * grid.TileScale), oy };
					right = new[] { left[0] + bounds[0] * grid.TileSize.Width / grid.TileScale, oy };
					top = new[] { ox, oy - bounds[1] * grid.TileSize.Height / (2 * grid.TileScale) };
					bottom = new[] { ox, top[1] + bounds[1] * grid.TileSize.Height / grid.TileScale };
				}
				else
				{
					if (selection.DecorationFootprint != CVec.Zero)
					{
						xMax = xMin + selection.DecorationFootprint.X - 1;
						yMax = yMin + selection.DecorationFootprint.Y - 1;
					}
					var extraDepth = selection.DecorationFootprintExtraDepth;
					// Flat Map.CenterOfCell + Building.CenterOffset, relative to actor screen center.
					var actorX = 724 + 362 * (building.Dimensions.X - building.Dimensions.Y) + building.LocalCenterOffset.X;
					var actorY = 724 + 362 * (building.Dimensions.X + building.Dimensions.Y - 2) + building.LocalCenterOffset.Y - building.LocalCenterOffset.Z;
					int[] Project(int x, int y, int dx, int dy) => new[]
					{
						(int)Math.Round((float)(724 * (x - y + 1) + dx) * grid.TileSize.Width / grid.TileScale) -
							(int)Math.Round((float)actorX * grid.TileSize.Width / grid.TileScale),
						(int)Math.Round((float)(724 * (x + y + 1) + dy) * grid.TileSize.Height / grid.TileScale) -
							(int)Math.Round((float)actorY * grid.TileSize.Height / grid.TileScale)
					};
					left = Project(xMin, yMax, -768 - extraDepth, extraDepth);
					right = Project(xMax, yMin, 768, 0);
					top = Project(xMin, yMin, 0, -768);
					bottom = Project(xMax, yMax, -extraDepth, 768 + extraDepth);
				}

				var baseTop = top[1];
				int[] Raised(int[] point) => new[] { point[0], point[1] - height };

				var image = (actor.TraitInfo<RenderSpritesInfo>().Image ?? name.Split('.')[0]).ToLowerInvariant();
				var nodes = sequences[image].ToDictionary();
				var values = nodes.TryGetValue("Defaults", out var defaults) ? defaults.ToDictionary() : new Dictionary<string, MiniYaml>();
				foreach (var value in nodes["idle"].ToDictionary())
					values[value.Key] = value.Value;
				string Get(string key, string fallback) => values.TryGetValue(key, out var value) ? value.Value : fallback;
				report.Add(new
				{
					Actor = name, Image = image, File = Get("Filename", null),
					Anchor = Get("Anchor", "Center"), Offset = Get("Offset", "0,0"), Scale = Get("Scale", "1"),
					BaseTop = baseTop, Height = height, SelectionTop = baseTop - height,
					BodyCells = bodyCells,
					DecorationFootprint = new[] { selection.DecorationFootprint.X, selection.DecorationFootprint.Y },
					DecorationFootprintExtraDepth = selection.DecorationFootprintExtraDepth,
					GroundCorners = new[] { top, left, bottom, right },
					SelectionPolygon = new[] { Raised(top), Raised(left), left, bottom, right, Raised(right) },
					PreserveConfirmedBox = new[] { "pbox", "syrd", "proc" }.Contains(name)
				});
			}

			Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
		}
	}
}
