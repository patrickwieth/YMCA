using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// Deliberately separate from production export: these are calibration fixtures,
	// not a claim that the current cliff templates are correctly anchored.
	sealed class RubberduckTerrainLabCommand : IUtilityCommand
	{
		static readonly (string Name, ushort Template)[] Cliffs =
		{
			("high_sw", 12000), ("high_se", 12020),
			("high_s_outer", 12040), ("high_s_inner", 12060),
			("high_e_outer", 12080), ("high_w_outer", 12100),
			("high_e_inner", 12120), ("high_w_inner", 12130),
			("high_ne_outer", 12140), ("high_nw_outer", 12150),
			("high_n_outer", 12160),
		};

		string IUtilityCommand.Name => "--rubberduck-terrain-lab";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;

		[Desc("OUTPUT", "Audit Rubberduck sprite frames and export isolated height calibration maps (not production maps).")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(output);
			var report = new StringBuilder("# Rubberduck terrain calibration\n\n");
			var frameReport = new StringBuilder("sheet\tframe\twidth\theight\toffset\talpha-bounds\n");
			report.AppendLine("Atlas frames are row-major, eight columns, at native resolution with a checkerboard background.");
			report.AppendLine("Bounds are measured from alpha, not inferred from filenames. Offsets come from embedded PNG metadata.");
			report.AppendLine("Sidecar YAML alone is not used by PngSheetLoader. No source assets are modified.\n");
			report.AppendLine("| Sheet | Frame count | Frame size | Offset | Frame 0 alpha bounds (inclusive) |");
			report.AppendLine("|---|---:|---|---|---|");
			const string root = "ca|bits/terrain/rubberduck/";
			var sheets = Cliffs.Select(cliff => "cliffs/" + cliff.Name).Concat(new[]
			{
				"grass_cliff_trans", "sand_cliff_trans", "dirt_cliff_trans",
				"rock_cliffs", "wall_cliffs", "wall_cliff_columns",
				"grass_a_atomic_overlay", "sand_atomic_overlay", "water_v01", "water_v02",
				"grass_shore", "sand_over_water_shore",
			});
			foreach (var sheet in sheets)
			{
				var filename = root + sheet + ".png";
				if (!utility.ModData.DefaultFileSystem.Exists(filename))
				{
					report.AppendLine($"| {sheet} | MISSING | | | |");
					continue;
				}

				using var stream = utility.ModData.DefaultFileSystem.Open(filename);
				if (!new PngSheetLoader().TryParseSprite(stream, filename, out var frames, out _))
					throw new InvalidDataException($"Not a PNG sheet: {filename}");
				if (frames.Length == 0 || frames.Any(frame => frame.Type != SpriteFrameType.Rgba32))
					throw new InvalidDataException($"Expected RGBA frames: {filename}");
				for (var index = 0; index < frames.Length; index++)
				{
					var frame = frames[index];
					frameReport.AppendLine($"{sheet}\t{index}\t{frame.Size.Width}\t{frame.Size.Height}\t{frame.Offset}\t{AlphaBounds(frame)}");
				}
				var first = frames[0];
				report.AppendLine($"| {sheet} | {frames.Length} | {first.Size} | {first.Offset} | {AlphaBounds(first)} |");
				SaveAtlas(frames, Path.Combine(output, sheet.Replace('/', '-') + ".png"));
			}

			report.AppendLine("\n## Height fixtures\n");
			report.AppendLine("Three maps use height 2, 4, and 6. At TileSize 128x64 one height step projects to 32 pixels.");
			report.AppendLine("The plateau is a CPos-aligned square, not an MPos bitmap rectangle. Outline templates are explicit probes.");
			report.AppendLine("Raw atomics replace their ground tile here intentionally: gaps reveal missing underlays and bad anchors.");
			report.AppendLine("Do not use these fixtures for gameplay. No cliffs or shores are enabled in the production exporter.");
			foreach (var height in new byte[] { 2, 4, 6 })
				ExportFixture(utility.ModData, output, height);
			RubberduckCliffCalibrationSprites.Export(utility.ModData, Path.Combine(output, "calibration-cliffs.png"),
				Cliffs.Select(cliff => cliff.Name).ToArray());
			RubberduckCliffCalibrationSprites.ExportTerrainFaces(utility.ModData, Path.Combine(output, "terrain-cliff-faces.png"));
			RubberduckCliffCalibrationSprites.ExportShoreProbes(utility.ModData, Path.Combine(output, "calibration-shore.png"));
			ExportFixture(utility.ModData, output, 4, true);
			ExportFixture(utility.ModData, output, 4, true, true);
			RubberduckShoreCalibration.Export(utility.ModData, output);
			report.AppendLine("\n## Connected shore fixtures\n");
			report.AppendLine("Island, bay, lake and two disconnected-saddle fixtures use shared land vertices, 16 sand/water masks and 16 grass/sand masks.");
			report.AppendLine("Mask edges, rotation, range and full-land connectivity are checked. Partial shore cells remain Water; full sand/grass is walkable.");
			report.AppendLine("These are map-local rendering experiments, not production coastlines or restored surf artwork.");
			File.WriteAllText(Path.Combine(output, "README.md"), report.ToString());
			File.WriteAllText(Path.Combine(output, "frames.tsv"), frameReport.ToString());
			Console.WriteLine($"Terrain calibration assets and reload-checked fixtures: {output}");
		}

		static string AlphaBounds(ISpriteFrame frame)
		{
			var minX = frame.Size.Width;
			var minY = frame.Size.Height;
			var maxX = -1;
			var maxY = -1;
			for (var y = 0; y < frame.Size.Height; y++)
				for (var x = 0; x < frame.Size.Width; x++)
					if (frame.Data[(y * frame.Size.Width + x) * 4 + 3] != 0)
					{
						minX = Math.Min(minX, x);
						minY = Math.Min(minY, y);
						maxX = Math.Max(maxX, x);
						maxY = Math.Max(maxY, y);
					}
			return maxX < 0 ? "empty" : $"{minX},{minY} .. {maxX},{maxY}";
		}

		static void SaveAtlas(ISpriteFrame[] frames, string path)
		{
			const int columns = 8;
			var cellWidth = frames.Max(frame => frame.Size.Width) + 4;
			var cellHeight = frames.Max(frame => frame.Size.Height) + 4;
			var width = columns * cellWidth;
			var height = ((frames.Length + columns - 1) / columns) * cellHeight;
			var data = new byte[width * height * 4];
			for (var y = 0; y < height; y++)
				for (var x = 0; x < width; x++)
				{
					var index = (y * width + x) * 4;
					var shade = (byte)(((x / 8 + y / 8) % 2 == 0) ? 48 : 80);
					data[index] = data[index + 1] = data[index + 2] = shade;
					data[index + 3] = 255;
				}
			for (var i = 0; i < frames.Length; i++)
			{
				var frame = frames[i];
				for (var y = 0; y < frame.Size.Height; y++)
					for (var x = 0; x < frame.Size.Width; x++)
					{
						var source = (y * frame.Size.Width + x) * 4;
						var dest = ((i / columns * cellHeight + y + 2) * width + i % columns * cellWidth + x + 2) * 4;
						var alpha = frame.Data[source + 3];
						for (var channel = 0; channel < 3; channel++)
							data[dest + channel] = (byte)((frame.Data[source + channel] * alpha + data[dest + channel] * (255 - alpha)) / 255);
					}
			}
			new Png(data, SpriteFrameType.Rgba32, width, height).Save(path);
		}

		static void ConfigureNormalizedSprites(Map map)
		{
			var rules = new List<MiniYamlNode>
			{
				new MiniYamlNode("^CalibrationSprite", new MiniYaml("", new[]
				{
					new MiniYamlNode("AlwaysVisible", ""),
					new MiniYamlNode("Immobile", new MiniYaml("", new[] { new MiniYamlNode("OccupiesSpace", "false") })),
					new MiniYamlNode("RenderSprites", ""),
					new MiniYamlNode("WithSpriteBody", ""),
					new MiniYamlNode("BodyOrientation", new MiniYaml("", new[] { new MiniYamlNode("QuantizedFacings", "1") })),
				})),
			};
			var sequences = new List<MiniYamlNode>();
			for (var i = 0; i < Cliffs.Length + 13; i++)
			{
				var shore = i > Cliffs.Length;
				var frame = shore ? i - Cliffs.Length - 1 : i;
				var name = shore ? "calibration.shore" + frame : "calibration.cliff" + frame;
				rules.Add(new MiniYamlNode(name, new MiniYaml("", new[] { new MiniYamlNode("Inherits", "^CalibrationSprite") })));
				sequences.Add(new MiniYamlNode(name, new MiniYaml("", new[]
				{
					new MiniYamlNode("idle", new MiniYaml("", new[]
					{
						new MiniYamlNode("Filename", shore ? "calibration-shore.png" : "calibration-cliffs.png"),
						new MiniYamlNode("Start", frame.ToString()),
						new MiniYamlNode("ZOffset", i == Cliffs.Length ? "-16384" : "0"),
						new MiniYamlNode("Length", "1"),
					})),
				})));
			}
			map.RuleDefinitions = map.RuleDefinitions.WithNodesAppended(rules);
			map.SequenceDefinitions = new MiniYaml("", sequences);
		}

		static void ExportFixture(ModData modData, string output, byte height, bool normalized = false, bool notched = false)
		{
			var terrain = (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"];
			using var map = new Map(modData, terrain, 66, 82);
			map.Title = $"Rubberduck Calibration Height {height}" + (normalized ? " Normalized" : " Raw") + (notched ? " Notch" : "");
			map.Author = "YMCA terrain calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(64, 80));
			var players = new MapPlayers(map.Rules, 2);
			for (var i = 0; i < 2; i++)
			{
				players.Players[$"Multi{i}"].Spawn = i + 1;
				players.Players[$"Multi{i}"].LockSpawn = true;
			}
			map.PlayerDefinitions = players.ToMiniYaml();
			map.RuleDefinitions = new MiniYaml("", new[]
			{
				new MiniYamlNode("World", new MiniYaml("", new[]
				{
					new MiniYamlNode("TerrainCalibrationView", new MiniYaml("", new[]
					{
						new MiniYamlNode("Center", new MPos(32, 48).ToCPos(map).ToString()),
					})),
				})),
				new MiniYamlNode("Player", new MiniYaml("", new[]
				{
					new MiniYamlNode("Shroud", new MiniYaml("", new[]
					{
						new MiniYamlNode("FogCheckboxEnabled", "false"),
						new MiniYamlNode("FogCheckboxLocked", "true"),
						new MiniYamlNode("ExploredMapCheckboxEnabled", "true"),
						new MiniYamlNode("ExploredMapCheckboxLocked", "true"),
					})),
				})),
			});
			foreach (var cell in map.AllCells)
			{
				map.Tiles[cell] = new TerrainTile(1000, 0);
				map.Height[cell] = 0;
			}

			var actors = new List<MiniYamlNode>();
			if (normalized)
				ConfigureNormalizedSprites(map);
			var center = new MPos(32, 48).ToCPos(map);
			bool Raised(int x, int y) => x >= -7 && x <= 7 && y >= -7 && y <= 7 && !(notched && x > 0 && y > 0);
			for (var y = -7; y <= 7; y++)
				for (var x = -7; x <= 7; x++)
				{
					if (!Raised(x, y))
						continue;
					var cell = center + new CVec(x, y);
					map.Height[cell] = height;
					var se = !Raised(x + 1, y);
					var sw = !Raised(x, y + 1);
					var nw = !Raised(x - 1, y);
					var ne = !Raised(x, y - 1);
					ushort tile = 1000;
					if (se && sw) tile = 12040;
					else if (nw && ne) tile = 12160;
					else if (se && ne) tile = 12080;
					else if (nw && sw) tile = 12100;
					else if (se) tile = 12020;
					else if (sw) tile = 12000;
					else if (nw) tile = 12150;
					else if (ne) tile = 12140;
					else if (!Raised(x + 1, y + 1)) tile = 12060;
					map.Tiles[cell] = new TerrainTile(normalized && tile != 1000 ? (ushort)3992 : tile, 0);
					if (normalized && tile != 1000)
					{
						var index = Array.FindIndex(Cliffs, cliff => cliff.Template == tile);
						var overlay = new ActorReference("calibration.cliff" + index)
						{
							new LocationInit(cell), new OwnerInit("Neutral"),
						};
						actors.Add(new MiniYamlNode("Cliff" + actors.Count, overlay.Save()));
						var substrate = new ActorReference("calibration.cliff" + Cliffs.Length)
						{
							new LocationInit(cell), new OwnerInit("Neutral"),
						};
						actors.Add(new MiniYamlNode("Substrate" + actors.Count, substrate.Save()));
						if (tile == 12080 || tile == 12100)
						{
							var cornerFace = new ActorReference(tile == 12080 ? "calibration.cliff1" : "calibration.cliff0")
							{
								new LocationInit(cell), new OwnerInit("Neutral"),
							};
							actors.Add(new MiniYamlNode("CornerFace" + actors.Count, cornerFace.Save()));
						}
					}
				}

			// Flat-water control strip. The normalized variant adds sand-mask probes.
			for (var v = 67; v < 76; v++)
				for (var u = 10; u < 54; u++)
					map.Tiles[new MPos(u, v)] = new TerrainTile(v == 67 ? (ushort)1020 : (ushort)1050, 0);
			if (normalized)
				for (var u = 10; u < 54; u++)
				{
					var shore = new ActorReference("calibration.shore2")
					{
						new LocationInit(new MPos(u, 68).ToCPos(map)), new OwnerInit("Neutral"),
					};
					actors.Add(new MiniYamlNode("Shore" + actors.Count, shore.Save()));
				}
			foreach (var uv in new[] { new MPos(24, 42), new MPos(40, 56) })
			{
				var spawn = new ActorReference("mpspawn") { new LocationInit(uv.ToCPos(map)), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Actor" + actors.Count, spawn.Save()));
			}
			map.ActorDefinitions = actors;
			var path = Path.Combine(output, $"rubberduck-calibration-height-{height}" + (normalized ? "-normalized" : "") + (notched ? "-notch" : "") + ".oramap");
			if (File.Exists(path)) File.Delete(path);
			using (var package = ZipFileLoader.Create(path))
			{
				if (normalized)
				{
					package.Update("calibration-cliffs.png", File.ReadAllBytes(Path.Combine(output, "calibration-cliffs.png")));
					package.Update("calibration-shore.png", File.ReadAllBytes(Path.Combine(output, "calibration-shore.png")));
				}
				map.Save(package);
			}
			using var stream = File.OpenRead(path);
			using var savedPackage = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var savedMap = new Map(modData, savedPackage);
			if (savedMap.Height[center] != height)
				throw new InvalidDataException("Height layer changed during package roundtrip.");
		}
	}
}
