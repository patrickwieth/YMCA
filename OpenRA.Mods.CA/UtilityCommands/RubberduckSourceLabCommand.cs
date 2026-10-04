using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// Read original sheets, not the historically sliced mod copies. Ruin is deliberately excluded.
	sealed class RubberduckSourceLabCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-source-lab";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;

		[Desc("SOURCE OUTPUT", "Audit original Rubberduck grassland sheets and export a native-water rendering fixture.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var source = Path.GetFullPath(args[1]);
			var output = Path.GetFullPath(args[2]);
			var relativeOutput = Path.GetRelativePath(source, output);
			if (!Path.IsPathRooted(relativeOutput) && relativeOutput != ".." &&
				!relativeOutput.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				throw new ArgumentException("Output must be outside the read-only source directory.");
			Directory.CreateDirectory(output);
			var inventory = new StringBuilder("sheet\twidth\theight\tpixel-type\tsha256\n");
			foreach (var path in Directory.GetFiles(Path.Combine(source, "grassland"), "*.png").OrderBy(p => p, StringComparer.Ordinal))
			{
				using var stream = File.OpenRead(path);
				var png = new Png(stream);
				using var hash = SHA256.Create();
				inventory.AppendLine($"{Path.GetFileName(path)}\t{png.Width}\t{png.Height}\t{png.Type}\t{Convert.ToHexString(hash.ComputeHash(File.ReadAllBytes(path)))}");
			}
			File.WriteAllText(Path.Combine(output, "source-inventory.tsv"), inventory.ToString());

			var slots = new StringBuilder("sheet\tslot\tx\ty\twidth\theight\talpha-bounds-local\ttouches-slot-edge\n");
			foreach (var sheet in new[]
			{
				(Name: "rock_cliffs", Width: 128, Height: 256),
				(Name: "grassland_1x1", Width: 128, Height: 256),
				(Name: "grassland_2x2", Width: 256, Height: 512),
			})
			{
				var png = Load(Path.Combine(source, "grassland", sheet.Name + ".png"));
				if (png.Width % sheet.Width != 0 || png.Height % sheet.Height != 0)
					throw new InvalidDataException("Sheet does not fit the audited source-slot grid: " + sheet.Name);
				var annotated = (byte[])png.Data.Clone();
				var columns = png.Width / sheet.Width;
				for (var y = 0; y < png.Height; y += sheet.Height)
					for (var x = 0; x < png.Width; x += sheet.Width)
					{
						var minX = sheet.Width;
						var minY = sheet.Height;
						var maxX = -1;
						var maxY = -1;
						for (var sy = 0; sy < sheet.Height; sy++)
							for (var sx = 0; sx < sheet.Width; sx++)
								if (png.Data[((y + sy) * png.Width + x + sx) * 4 + 3] != 0)
								{
									minX = Math.Min(minX, sx); minY = Math.Min(minY, sy);
									maxX = Math.Max(maxX, sx); maxY = Math.Max(maxY, sy);
								}
						var bounds = maxX < 0 ? "empty" : $"{minX},{minY}..{maxX},{maxY}";
						var edges = (minX == 0 ? "L" : "") + (maxX == sheet.Width - 1 ? "R" : "") +
							(minY == 0 ? "T" : "") + (maxY == sheet.Height - 1 ? "B" : "");
						slots.AppendLine($"{sheet.Name}\t{y / sheet.Height * columns + x / sheet.Width}\t{x}\t{y}\t{sheet.Width}\t{sheet.Height}\t{bounds}\t{edges}");
					}
				for (var y = 0; y < png.Height; y++)
					for (var x = 0; x < png.Width; x++)
						if (x % sheet.Width == 0 || y % sheet.Height == 0)
						{
							var d = (y * png.Width + x) * 4;
							annotated[d] = 255; annotated[d + 1] = 80; annotated[d + 2] = 30; annotated[d + 3] = 255;
						}
				new Png(annotated, SpriteFrameType.Rgba32, png.Width, png.Height).Save(Path.Combine(output, sheet.Name + "-source-grid.png"));
				// Keep adjacent slots together for inspection. Edge contact alone does
				// not establish that a pair is one corner or a standalone terrain sprite.
				var left = sheet.Name == "grassland_2x2" ? 0 : 3 * sheet.Width;
				Crop(png, left, 0, sheet.Width * 2, sheet.Height).Save(Path.Combine(output, sheet.Name + "-paired-corner.png"));
			}
			File.WriteAllText(Path.Combine(output, "source-slots.tsv"), slots.ToString());

			foreach (var variant in new[] { "water_v01", "water_v02" })
				ExportWater(Load(Path.Combine(source, "grassland", variant + ".png")), Path.Combine(output, variant + "-diamonds.png"));
			var low = Load(Path.Combine(source, "grassland", "rock_cliffs.png"));
			var high = Load(Path.Combine(source, "grassland", "grassland_1x1.png"));
			foreach (var slot in new[] { 0, 1, 2, 5, 6, 7, 16, 17, 18, 19, 22, 23, 24, 26, 30 })
				Crop(high, slot % 12 * 128, slot / 12 * 256, 128, 256).Save(Path.Combine(output, $"high-slot-{slot}.png"));
			// Source slot 90 contains the plain horizontal plateau material seen
			// in the examples. Retain its complete silhouette and source margins.
			Crop(high, 768, 1792, 128, 128).Save(Path.Combine(output, "grassland-plateau-fill.png"));
			var art = new[]
			{
				Crop(low, 0, 0, 128, 256), Crop(low, 640, 0, 128, 256),
				Crop(low, 1024, 256, 128, 256), Crop(low, 1152, 256, 128, 256),
				Crop(high, 0, 0, 128, 256), Crop(high, 384, 0, 256, 256),
			};
			for (var i = 0; i < art.Length; i++)
			{
				art[i].EmbeddedData.Add("FrameSize", $"{art[i].Width},{art[i].Height}");
				art[i].EmbeddedData.Add("FrameAmount", "1");
				// Gallery foot anchors only: these do not assert a terrain height.
				art[i].EmbeddedData.Add("Offset", i < 4 ? "0,-32" : "0,-96");
				art[i].Save(Path.Combine(output, $"source-art-{i}.png"));
			}
			ExportWaterMap(utility.ModData, output);
			ExportWaterMap(utility.ModData, output, true);
			File.WriteAllText(Path.Combine(output, "README.md"),
				"# Original Rubberduck source audit\n\n" +
				"Only grassland/*.png was read. Ruin and source images are unchanged. Source-slot grids are not a declaration that every slot is a complete sprite.\n\n" +
				"Inspect adjacent-slot crops and edge-touch flags before importing individual frames. A paired crop does not by itself prove a complete corner; some pairs contain opposite-facing wall sections. The old 128x64 slicing cuts rock silhouettes vertically.\n\n" +
				"water_v01/v02 are 768x384 isometric 6x6 patches, not a rectangular 6x6 sprite sheet. Output frames use index x + 6*y and retain native pixels. All 36 diamonds per variant are checked for coverage.\n\n" +
				"The example JPGs show open cliff ends, low rock banks, flat terrain transitions and complete plateau/cave corners. Source grassland_1x1 slot 90 contains a plain plateau-fill diamond (crop exported separately). They do not establish a traversable height ramp or a heightmap. No RampType is inferred from an illustration.\n\n" +
				"The water fixture tests texture reconstruction. The cliff gallery shows unwarped low/high strips, candidate low endings and an adjacent high-face pair. Its flat heightmap and gallery foot anchors do not establish ramp geometry or walkability. Neither fixture is a gameplay map. Production cliffs/shores are not changed by this command.\n");
			Console.WriteLine("Original grassland audit and water fixture: " + output);
		}

		static Png Load(string path)
		{
			using var stream = File.OpenRead(path);
			var png = new Png(stream);
			if (png.Type != SpriteFrameType.Rgba32) throw new InvalidDataException("Expected RGBA source: " + path);
			return png;
		}

		static Png Crop(Png source, int x, int y, int width, int height)
		{
			var data = new byte[width * height * 4];
			for (var row = 0; row < height; row++)
				Array.Copy(source.Data, ((y + row) * source.Width + x) * 4, data, row * width * 4, width * 4);
			return new Png(data, SpriteFrameType.Rgba32, width, height);
		}

		static void ExportWater(Png source, string path)
		{
			if (source.Width != 768 || source.Height != 384) throw new InvalidDataException("Expected native 6x6 water diamond.");
			var data = new byte[768 * 384 * 4];
			var coverage = new byte[768 * 384];
			for (var y = 0; y < 6; y++)
				for (var x = 0; x < 6; x++)
				{
					var sx = 320 + 64 * (x - y);
					var sy = 32 * (x + y);
					var covered = 0;
					for (var py = 0; py < 64; py++)
						for (var px = 0; px < 128; px++)
						{
							var s = ((sy + py) * 768 + sx + px) * 4;
							var d = ((y * 64 + py) * 768 + x * 128 + px) * 4;
							Array.Copy(source.Data, s, data, d, 3);
							// Ground sheets can contain rectangular opaque frames, so
							// construct the actual isometric footprint rather than reuse their alpha.
							var insideDiamond = Math.Abs(px + 0.5 - 64) / 64 + Math.Abs(py + 0.5 - 32) / 32 <= 1;
							data[d + 3] = insideDiamond ? source.Data[s + 3] : (byte)0;
							if (insideDiamond) coverage[s / 4]++;
							if (data[d + 3] > 0) covered++;
						}
					if (covered < 3500) throw new InvalidDataException($"Incomplete water diamond {x},{y}: {covered} pixels.");
				}
			for (var y = 0; y < 384; y++)
				for (var x = 0; x < 768; x++)
				{
					var expected = Math.Abs(x + 0.5 - 384) / 384 + Math.Abs(y + 0.5 - 192) / 192 <= 1 ? 1 : 0;
					if (coverage[y * 768 + x] != expected)
						throw new InvalidDataException($"Water reconstruction has a gap or overlap at {x},{y}.");
				}
			var result = new Png(data, SpriteFrameType.Rgba32, 768, 384);
			result.EmbeddedData.Add("FrameSize", "128,64");
			result.EmbeddedData.Add("FrameAmount", "36");
			result.Save(path);
		}

		static void ExportWaterMap(ModData modData, string output, bool cliffGallery = false)
		{
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 66, 82);
			map.Title = cliffGallery ? "Rubberduck Original Cliff Gallery" : "Rubberduck Original Water 6x6";
			map.Author = "YMCA source calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(64, 80));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var center = new MPos(32, 48).ToCPos(map);
			var rules = new StringBuilder("World:\n\tTerrainCalibrationView:\n\t\tCenter: " + center + "\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n^SourceWater:\n\tAlwaysVisible:\n\tImmobile:\n\t\tOccupiesSpace: false\n\tRenderSprites:\n\tWithSpriteBody:\n\tBodyOrientation:\n\t\tQuantizedFacings: 1\n");
			var sequences = new StringBuilder();
			for (var frame = 0; frame < 36; frame++)
			{
				rules.AppendLine($"source.water{frame}:\n\tInherits: ^SourceWater");
				sequences.AppendLine($"source.water{frame}:\n\tidle:\n\t\tFilename: water_v01-diamonds.png\n\t\tStart: {frame}\n\t\tLength: 1\n\t\tZOffset: -16384");
			}
			if (cliffGallery)
				for (var i = 0; i < 6; i++)
				{
					rules.AppendLine($"source.art{i}:\n\tInherits: ^SourceWater");
					sequences.AppendLine($"source.art{i}:\n\tidle:\n\t\tFilename: source-art-{i}.png\n\t\tLength: 1");
				}
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "source-water-rules"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "source-water-sequences"));
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(cliffGallery ? (ushort)1000 : (ushort)1050, 0);
			var actors = new List<MiniYamlNode>();
			for (var y = -12; y < 12; y++)
				for (var x = -12; x < 12; x++)
				{
					if (cliffGallery) continue;
					var frame = (x + 12) % 6 + 6 * ((y + 12) % 6);
					var actor = new ActorReference("source.water" + frame) { new LocationInit(center + new CVec(x, y)), new OwnerInit("Neutral") };
					actors.Add(new MiniYamlNode("Water" + actors.Count, actor.Save()));
				}
			if (cliffGallery)
			{
				void AddArt(int frame, int x, int y)
				{
					var actor = new ActorReference("source.art" + frame) { new LocationInit(center + new CVec(x, y)), new OwnerInit("Neutral") };
					actors.Add(new MiniYamlNode("OriginalArt" + actors.Count, actor.Save()));
				}
				for (var i = 0; i < 6; i++)
				{
					AddArt(0, -5 + i, -4);
					AddArt(1, 4, -4 + i);
					AddArt(4, -5 + i, 5);
				}
				AddArt(2, 1, -4);
				AddArt(3, 4, 2);
				AddArt(5, 5, 6);
			}
			foreach (var uv in new[] { new MPos(8, 24), new MPos(58, 72) })
			{
				for (var y = -3; y <= 3; y++)
					for (var x = -3; x <= 3; x++) map.Tiles[uv.ToCPos(map) + new CVec(x, y)] = new TerrainTile(1000, 0);
				var spawn = new ActorReference("mpspawn") { new LocationInit(uv.ToCPos(map)), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, spawn.Save()));
			}
			map.ActorDefinitions = actors;
			var path = Path.Combine(output, cliffGallery ? "rubberduck-original-cliffs.oramap" : "rubberduck-original-water.oramap");
			if (File.Exists(path)) File.Delete(path);
			using (var package = ZipFileLoader.Create(path))
			{
				package.Update("water_v01-diamonds.png", File.ReadAllBytes(Path.Combine(output, "water_v01-diamonds.png")));
				if (cliffGallery)
					for (var i = 0; i < 6; i++) package.Update($"source-art-{i}.png", File.ReadAllBytes(Path.Combine(output, $"source-art-{i}.png")));
				map.Save(package);
			}
			using var file = File.OpenRead(path);
			using var saved = new ZipFileLoader.ReadOnlyZipFile(file, path);
			using var reopened = new Map(modData, saved);
			if (reopened.InvalidCustomRules) throw new InvalidDataException("Source fixture rules failed.", reopened.InvalidCustomRulesException);
		}
	}
}
