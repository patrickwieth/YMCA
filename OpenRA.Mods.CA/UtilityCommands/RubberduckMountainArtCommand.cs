using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckMountainArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-mountain-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("SOURCE OUTPUT", "Project original cliff planes onto the existing blocked mountain walls without changing their silhouettes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var sourceRoot = Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);
			var output = Path.GetFullPath(args[2]);
			if (output.Equals(sourceRoot, StringComparison.OrdinalIgnoreCase) || output.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("Derived artwork must not overwrite the source tree.");
			var sourcePath = Path.Combine(sourceRoot, "grassland", "grassland_1x1.png");
			using var stream = File.OpenRead(sourcePath);
			var atlas = new Png(stream);
			if (atlas.Width != 1536 || atlas.Height != 3840 || atlas.Type != SpriteFrameType.Rgba32)
				throw new InvalidDataException("Expected the original 12-column RGBA cliff atlas.");
			Directory.CreateDirectory(output);
			const int width = 1152;
			var pixels = new byte[width * 1280 * 4];
			var rules = new StringBuilder(); var sequences = new StringBuilder();
			var cornersX = new[] { 64.0, 128, 64, 0 }; var cornersY = new[] { 32.0, 64, 96, 64 };
			var samples = 0;
			for (var direction = 0; direction < 4; direction++)
			{
				var edge = PlateauTopology.Edge(direction);
				for (var variant = 0; variant < 9; variant++)
				{
					var slot = (direction == 0 ? 5 : direction == 1 ? 0 : direction == 2 ? 12 : 8) + variant % 3;
					var donor = new byte[128 * 256 * 4];
					for (var y = 0; y < 256; y++)
						Array.Copy(atlas.Data, ((slot / 12 * 256 + y) * atlas.Width + slot % 12 * 128) * 4, donor, y * 128 * 4, 128 * 4);
					var face = RubberduckPlateauRenderer.Face(direction, 4, 4, donor);
					for (var y = 0; y < 320; y++)
						for (var x = 0; x < 128; x++)
						{
							var p = (y * 128 + x) * 4;
							if (face[p + 3] == 0) continue;
							var t = (x + 0.5 - cornersX[edge.A]) / (cornersX[edge.B] - cornersX[edge.A]);
							var top = cornersY[edge.A] + t * (cornersY[edge.B] - cornersY[edge.A]);
							var depth = Math.Clamp((y + 0.5 - top) / 128, 0, 1);
							if (direction >= 2)
							{
								var material = RubberduckNativeCliffArtCommand.RearRockSample(atlas,
									(direction == 2 ? 5 : 0) + variant % 3, .2 + t * .6, .35 + variant / 3 * .05);
								Array.Copy(atlas.Data, material, face, p, 3); samples++; continue;
							}
							// Project the authored plane interior, not a repeated 24x80 strip.
							// Inset the vertical range to exclude the grass lip and soil apron.
							var u = 0.17 + t * 0.60 + variant / 3 * 0.03;
							var v = 0.05 + variant / 3 * 0.10 + depth * 0.65;
							var sx = direction == 0 ? 64 * u + 64 * v : direction == 1 ? 64 + 64 * u - 64 * v : direction == 2 ? 64 + 64 * u : 64 * u;
							var sy = direction == 0 ? 64 - 32 * u + 192 * v : direction == 1 ? 32 + 32 * u + 192 * v : direction == 2 ? 96 - 32 * u + 5 : 64 + 32 * u + 5;
							var ix = Math.Clamp((int)sx, 0, 127); var iy = Math.Clamp((int)sy, 0, 255);
							var sample = -1;
							for (var dy = 0; dy <= 16 && sample < 0; dy++)
								foreach (var row in new[] { iy - dy, iy + dy })
								{
									if (row < 0 || row >= 256 || sample >= 0) continue;
									for (var distance = 0; distance < 128 && sample < 0; distance++)
										foreach (var column in new[] { ix - distance, ix + distance })
										{
											if (column < 0 || column >= 128) continue;
											var q = (row * 128 + column) * 4;
											if (donor[q + 3] == 255 && Math.Max(donor[q], Math.Max(donor[q + 1], donor[q + 2])) > 8) { sample = q; break; }
										}
								}
							if (sample < 0) throw new InvalidDataException($"No opaque authored wall sample: slot {slot}, {ix},{iy}.");
							Array.Copy(donor, sample, face, p, 3); samples++;
						}
					var originalMask = RubberduckPlateauRenderer.Face(direction, 4, 4, donor);
					for (var p = 3; p < face.Length; p += 4)
						if (face[p] != originalMask[p]) throw new InvalidDataException("Mountain projection changed the wall silhouette.");
					for (var y = 0; y < 320; y++)
						Array.Copy(face, y * 128 * 4, pixels, ((direction * 320 + y) * width + variant * 128) * 4, 128 * 4);
				}
				rules.AppendLine($"terrain.rubberduck.mountainwall{direction}:\n\tInherits: ^RubberduckTerrainDecoration\n\t-WithSpriteBody:\n\tPlateauFaceBody:\n\t\tDirection: {direction}\n\t\tDropA: 4\n\t\tDropB: 4\n");
				sequences.AppendLine($"terrain.rubberduck.mountainwall{direction}:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/mountains/mountain-faces.png\n\t\tStart: {direction * 9}\n\t\tLength: 9\n");
			}
			var png = new Png(pixels, SpriteFrameType.Rgba32, width, 1280);
			png.EmbeddedData.Add("FrameSize", "128,320"); png.EmbeddedData.Add("FrameAmount", "36"); png.EmbeddedData.Add("Offset", "0,96");
			var imagePath = Path.Combine(output, "mountain-faces.png"); png.Save(imagePath);
			ExportCorners(atlas, output);
			ValidateRearMaterialChange(utility, "mountain-faces.png", output, 640);
			ValidateRearMaterialChange(utility, "mountain-corners.png", output, 640);
			sequences.AppendLine("terrain.rubberduck.mountaincorners:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/mountains/mountain-corners.png\n\t\tLength: 16\n");
			File.WriteAllText(Path.Combine(output, "rules.yaml"), rules.ToString());
			File.WriteAllText(Path.Combine(output, "sequences.yaml"), sequences.ToString().TrimEnd() + "\n");
			File.WriteAllText(Path.Combine(output, "provenance.txt"),
				$"Derived wall-plane projection, not untouched native cliff templates. Original silhouettes and shared depth clipping retained; no terrain/collision edits.\nSource grassland/grassland_1x1.png SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath)))}\n" +
				$"Donors: 5..7, 0..2, 12..14, 8..10. Plane-interior projection, three horizontal windows (17..77, 20..80, 23..83 percent) and three vertical windows (5..70, 15..80, 25..90 percent) sampling to exclude silhouette bevels, nearest opaque same-row relief samples (up to 16 rows of bounded fallback for empty relief rows). No ruin assets.\n{samples} sampled pixels; all 36 alpha masks match the established full-height wall geometry. Corner overlays use original slot 24/16 wall samples with a 12-pixel alpha fade inside the same wall mask; no silhouette expansion. Shared runtime/editor topology chooses convex/concave overlays.\nOutput SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(imagePath)))}\n" +
				$"Corner output SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, "mountain-corners.png"))))}\n" +
				"Rear-contact follow-up: rear faces and rear corner overlays sample exposed front stone (slots 0..2 and 5..7), not opaque backface matte. Original front RGBA, all alpha masks and transparent pixels are validated unchanged.\n");
			Console.WriteLine($"PASS: 36 projected mountain wall variants and 16 bounded corner overlays, unchanged wall masks: {output}");
		}

		static void ValidateRearMaterialChange(Utility utility, string filename, string output, int frontRows)
		{
			using var beforeStream = utility.ModData.DefaultFileSystem.Open("ca|bits/terrain/rubberduck/derived/mountains/" + filename);
			using var afterStream = File.OpenRead(Path.Combine(output, filename));
			var before = new Png(beforeStream); var after = new Png(afterStream);
			if (before.Width != after.Width || before.Height != after.Height) throw new InvalidDataException("Mountain atlas dimensions changed.");
			for (var p = 0; p < before.Data.Length; p++)
				if ((p % 4 == 3 || p / (before.Width * 4) < frontRows || after.Data[p / 4 * 4 + 3] == 0) && before.Data[p] != after.Data[p])
					throw new InvalidDataException("Rear material correction altered a front face, alpha mask or transparent pixel.");
			Console.WriteLine($"REAR MATERIAL PASS {filename}: front RGBA, all alpha and transparent pixels unchanged.");
		}

		static void ExportCorners(Png atlas, string output)
		{
			const int width = 512;
			var pixels = new byte[width * 1280 * 4];
			var cx = new[] { 64.0, 128, 64, 0 }; var cy = new[] { 32.0, 64, 96, 64 };
			for (var direction = 0; direction < 4; direction++)
				for (var end = 0; end < 2; end++)
					for (var kind = 0; kind < 2; kind++)
					{
						var slot = kind == 0 ? 24 : 16;
						var frame = (direction * 2 + end) * 2 + kind;
						var donor = new byte[128 * 256 * 4];
						for (var y = 0; y < 256; y++) Array.Copy(atlas.Data, ((slot / 12 * 256 + y) * atlas.Width + slot % 12 * 128) * 4, donor, y * 128 * 4, 512);
						var mask = RubberduckPlateauRenderer.Face(direction, 4, 4, donor);
						var edge = PlateauTopology.Edge(direction);
						for (var y = 0; y < 320; y++)
							for (var x = 0; x < 128; x++)
							{
								if (mask[(y * 128 + x) * 4 + 3] == 0) continue;
								var t = (x + 0.5 - cx[edge.A]) / (cx[edge.B] - cx[edge.A]);
								var distance = (end == 0 ? t : 1 - t) * 64;
								if (distance >= 12) continue;
								var top = cy[edge.A] + t * (cy[edge.B] - cy[edge.A]);
								var row = Math.Clamp((int)(96 + (y + 0.5 - top) / 128 * 128), 96, 224);
								var column = Math.Clamp((int)(64 + (direction % 2 == 0 ? distance : -distance)), 0, 127);
								var sample = -1;
								for (var radius = 0; radius < 128 && sample < 0; radius++)
									foreach (var sx in new[] { column - radius, column + radius })
										if (sx >= 0 && sx < 128 && donor[(row * 128 + sx) * 4 + 3] == 255) { sample = (row * 128 + sx) * 4; break; }
								if (sample < 0) throw new InvalidDataException("Missing authored corner wall sample.");
								var dest = (((frame / 4 * 320 + y) * width) + frame % 4 * 128 + x) * 4;
								if (direction >= 2)
								{
									var material = RubberduckNativeCliffArtCommand.RearRockSample(atlas, direction == 2 ? 5 : 0, .2 + t * .6, .35 + kind * .05);
									Array.Copy(atlas.Data, material, pixels, dest, 3);
								}
								else Array.Copy(donor, sample, pixels, dest, 3);
								pixels[dest + 3] = (byte)Math.Round(255 * (1 - distance / 12));
							}
					}
			var png = new Png(pixels, SpriteFrameType.Rgba32, width, 1280);
			png.EmbeddedData.Add("FrameSize", "128,320"); png.EmbeddedData.Add("FrameAmount", "16"); png.EmbeddedData.Add("Offset", "0,96");
			png.Save(Path.Combine(output, "mountain-corners.png"));
		}
	}
}
