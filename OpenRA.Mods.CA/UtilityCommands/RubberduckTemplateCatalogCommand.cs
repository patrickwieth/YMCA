using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// Source inspection only. Rectangular grids are candidate crop grids, not socket declarations.
	sealed class RubberduckTemplateCatalogCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-template-catalog";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("SOURCE OUTPUT", "Catalog all grassland source sheets, including small/ and alternate exports, without modifying source or production art.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var source = Path.GetFullPath(args[1]);
			var output = Path.GetFullPath(args[2]);
			var relative = Path.GetRelativePath(source, output);
			if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				throw new ArgumentException("Catalog output must be outside the original source directory.");
			Directory.CreateDirectory(output);
			var inventory = new StringBuilder("sheet\twidth\theight\tgrid-width\tgrid-height\tremainder-rows\tfile-sha256\trgba-sha256\n");
			var frames = new StringBuilder("sheet\tframe\tx\ty\twidth\theight\tnonzero-alpha\topaque-near-black\ttranslucent-near-black\trgba-sha256\n");
			var matches = new Dictionary<string, List<string>>();
			var frameHashes = new Dictionary<(string Sheet, int Frame), string>();
			var sheets = new Dictionary<string, (int Width, int Height, string Hash)>();
			var originals = new Dictionary<string, string>();
			var index = new StringBuilder("<!doctype html><meta charset='utf-8'><title>Rubberduck source catalog</title><style>body{background:#222;color:#eee;font:16px sans-serif}a{color:#9cf}img{max-width:100%}</style><h1>Original grassland source catalog</h1><p>Numbered grids are diagnostic overlays only. Crops retain source RGBA. No grids imply verified sockets. Water is a spatial patch, not an animation. Ruin excluded.</p>");
			foreach (var path in Directory.GetFiles(Path.Combine(source, "grassland"), "*.png", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
			{
				var name = Path.GetRelativePath(source, path).Replace('\\', '/');
				var key = name.Replace('/', '_').Replace(".png", "");
				var bytes = File.ReadAllBytes(path);
				var fileHash = Hash(bytes); originals.Add(path, fileHash);
				using var stream = new MemoryStream(bytes);
				var png = new Png(stream);
				if (png.Type != SpriteFrameType.Rgba32) throw new InvalidDataException("Expected RGBA source: " + name);
				var small = name.Contains("/small/");
				var scale = small ? 2 : 1;
				var width = 128; var height = 64;
				if (name.Contains("grassland_1x1") || name.EndsWith("rock_cliffs.png", StringComparison.Ordinal) || name.Contains("sheet_walls_1_")) height = 256;
				else if (name.Contains("grassland_2x2")) { width = 256; height = 512; }
				else if (name.Contains("sheet_walls_2_")) { width = 256; height = 256; }
				else if (name.Contains("sheet_walls_3_")) { width = 384; height = 320; }
				else if (name.Contains("water_v")) { width = png.Width; height = png.Height; }
				width /= scale; height /= scale;
				if (png.Width % width != 0) throw new InvalidDataException("Candidate grid has incomplete columns: " + name);
				var rgbaHash = Hash(png.Data);
				sheets.Add(name, (png.Width, png.Height, rgbaHash));
				inventory.AppendLine($"{name}\t{png.Width}\t{png.Height}\t{width}\t{height}\t{png.Height % height}\t{fileHash}\t{rgbaHash}");
				var directory = Path.Combine(output, key); Directory.CreateDirectory(directory);
				var annotated = (byte[])png.Data.Clone();
				var columns = png.Width / width;
				for (var y = 0; y + height <= png.Height; y += height)
					for (var x = 0; x < png.Width; x += width)
					{
						var id = y / height * columns + x / width;
						var data = new byte[width * height * 4];
						for (var row = 0; row < height; row++) Array.Copy(png.Data, ((y + row) * png.Width + x) * 4, data, row * width * 4, width * 4);
						var nonzero = 0; var opaque = 0; var translucent = 0;
						for (var p = 0; p < data.Length; p += 4)
							if (data[p + 3] != 0)
							{
								nonzero++;
								if (Math.Max(data[p], Math.Max(data[p + 1], data[p + 2])) <= 8)
								{ if (data[p + 3] == 255) opaque++; else translucent++; }
							}
						var hash = Hash(data);
						frameHashes.Add((name, id), hash);
						frames.AppendLine($"{name}\t{id}\t{x}\t{y}\t{width}\t{height}\t{nonzero}\t{opaque}\t{translucent}\t{hash}");
						if (nonzero != 0)
						{
							var identity = $"{width}x{height}:{hash}";
							if (!matches.TryGetValue(identity, out var list)) matches.Add(identity, list = new List<string>());
							list.Add($"{name}#{id}@{x},{y}");
							var file = Path.Combine(directory, id.ToString("D4") + ".png");
							SaveExact(new Png(data, SpriteFrameType.Rgba32, width, height), file);
						}
						for (var i = 0; i < width; i++) Mark(annotated, png.Width, x + i, y, 255, 80, 30);
						for (var i = 0; i < height; i++) Mark(annotated, png.Width, x, y + i, 255, 80, 30);
						Label(annotated, png.Width, x + 2, y + 3, id);
					}
				if (png.Height % height != 0)
				{
					var rows = png.Height % height;
					var tail = new byte[png.Width * rows * 4];
					Array.Copy(png.Data, png.Data.Length - tail.Length, tail, 0, tail.Length);
					SaveExact(new Png(tail, SpriteFrameType.Rgba32, png.Width, rows), Path.Combine(directory, "unassigned-tail.png"));
				}
				var grid = key + "-numbered.png";
				SaveExact(new Png(annotated, SpriteFrameType.Rgba32, png.Width, png.Height), Path.Combine(output, grid));
				index.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(name)}</h2><p>{png.Width}x{png.Height}; candidate grid {width}x{height}; remainder {png.Height % height} rows. <a href='{Uri.EscapeDataString(key)}/'>Original crops</a></p><a href='{Uri.EscapeDataString(grid)}'><img loading='lazy' src='{Uri.EscapeDataString(grid)}'></a>");
			}
			var packing = new StringBuilder("material\tvariant\tseparate-sheet\tseparate-frame\tcombined-frame\trgba-identical\n");
			foreach (var material in new[] { "grass", "dirt", "sand" })
				for (var variant = 0; variant < (material == "sand" ? 2 : 4); variant++)
					foreach (var group in new[] { (Name: "tiles", Count: 48, Offset: 0), (Name: "cliff_trans", Count: 32, Offset: 48) })
						for (var frame = 0; frame < group.Count; frame++)
						{
							var separate = variant * group.Count + frame;
							var combined = variant * 80 + group.Offset + frame;
							if (frameHashes[($"grassland/{material}_{group.Name}.png", separate)] != frameHashes[($"grassland/{material}_tiles_w_trans.png", combined)])
								throw new InvalidDataException($"Original material packing changed: {material}/{group.Name}/{separate}.");
							packing.AppendLine($"{material}\t{variant}\t{group.Name}\t{separate}\t{combined}\ttrue");
						}
			File.WriteAllText(Path.Combine(output, "verified-material-packing.tsv"), packing.ToString());
			index.AppendLine("<p><a href='verified-material-packing.tsv'>800 verified material frame correspondences</a>: each combined variant contains the 48 ground frames followed by the 32 cliff transition frames. Grass/dirt have four variants, sand two.</p>");
			File.WriteAllText(Path.Combine(output, "index.html"), index.ToString());
			File.WriteAllText(Path.Combine(output, "sheets.tsv"), inventory.ToString());
			File.WriteAllText(Path.Combine(output, "frames.tsv"), frames.ToString());
			File.WriteAllText(Path.Combine(output, "identical-frames.tsv"), "dimensions-and-rgba-hash\tsource-crops\n" +
				string.Join("\n", matches.Where(p => p.Value.Count > 1).OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "\t" + string.Join(";", p.Value))));
			File.WriteAllText(Path.Combine(output, "identical-sheets.tsv"), "source-sheets\n" + string.Join("\n", sheets.GroupBy(p => p.Value)
				.Where(g => g.Count() > 1).Select(g => string.Join(";", g.Select(p => p.Key)))));
			foreach (var pair in originals)
				if (Hash(File.ReadAllBytes(pair.Key)) != pair.Value) throw new InvalidDataException("Source changed during catalog creation.");
			File.WriteAllText(Path.Combine(output, "scope.txt"), "Read-only grassland catalog, including small/. Source hashes verified before/after. Exact RGBA crops roundtrip checked. Empty frames listed but not exported. Remainder rows exported separately, not silently discarded. Candidate rectangular grids do not establish complete sprites, orientation or socket compatibility. No production files changed.\n");
			Console.WriteLine($"Catalog: {sheets.Count} source sheets; {output}");
		}

		static void SaveExact(Png png, string path)
		{
			if (File.Exists(path))
			{
				using var existing = File.OpenRead(path);
				var decoded = new Png(existing);
				if (decoded.Type == png.Type && decoded.Width == png.Width && decoded.Height == png.Height && decoded.Data.SequenceEqual(png.Data)) return;
			}
			png.Save(path);
			using var check = File.OpenRead(path);
			if (!new Png(check).Data.SequenceEqual(png.Data)) throw new InvalidDataException("Catalog PNG changed decoded pixels.");
		}

		static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
		static void Mark(byte[] data, int width, int x, int y, byte r, byte g, byte b)
		{
			var p = (y * width + x) * 4;
			data[p] = r; data[p + 1] = g; data[p + 2] = b; data[p + 3] = 255;
		}
		static void Label(byte[] data, int width, int x, int y, int number)
		{
			var digits = new[] { "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001", "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111" };
			foreach (var ch in number.ToString(System.Globalization.CultureInfo.InvariantCulture))
			{
				for (var dy = 0; dy < 10; dy++)
					for (var dx = 0; dx < 8; dx++)
					{
						var on = dx < 6 && digits[ch - '0'][dy / 2 * 3 + dx / 2] == '1';
						Mark(data, width, x + dx, y + dy, on ? (byte)255 : (byte)0, on ? (byte)255 : (byte)0, on ? (byte)255 : (byte)0);
					}
				x += 8;
			}
		}
	}
}
