using System;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Experimental, map-local composites. Original source sheets remain untouched.
	static class RubberduckCliffCalibrationSprites
	{
		public static void Export(ModData modData, string path, string[] names)
		{
			const int frameWidth = 256;
			const int frameHeight = 320;
			var width = frameWidth * (names.Length + 1);
			var data = new byte[width * frameHeight * 4];
			var grass = Frames(modData, "grass_a_base")[0];
			// Keep substrate separate from rock: a later neighbor must never paint
			// its grass backing over an already drawn cliff face.
			for (var y = 128; y >= 0; y -= 32)
				Blit(data, width, frameHeight, grass.Data, grass.Size.Width, grass.Size.Height,
					names.Length * frameWidth + 64, y);
			for (var i = 0; i < names.Length; i++)
			{
				var x = i * frameWidth;
				var frames = Frames(modData, "cliffs/" + names[i]);
				if (names[i] == "high_n_outer")
				{
					// The corner straddles the first two source frames. Preserve both
					// halves and center on their shared x=128 seam.
					Blit(data, width, frameHeight, frames[0].Data, 128, 192, x, -18);
					Blit(data, width, frameHeight, frames[1].Data, 128, 192, x + 128, -18);
				}
				else
				{
					var shift = names[i] == "high_ne_outer" || names[i] == "high_nw_outer" ? -116 : 0;
					Blit(data, width, frameHeight, frames[0].Data, 128, 192, x + 64, shift);
				}
			}

			var png = new Png(data, SpriteFrameType.Rgba32, width, frameHeight);
			png.EmbeddedData.Add("FrameSize", $"{frameWidth},{frameHeight}");
			png.EmbeddedData.Add("FrameAmount", (names.Length + 1).ToString());
			// Top of the frame is the upper diamond's top, 32px above its center.
			png.EmbeddedData.Add("Offset", "0,128");
			png.Save(path);
		}

		public static void ExportTerrainFaces(ModData modData, string path)
		{
			const int width = 128 * 18;
			const int height = 256;
			var pixels = new byte[width * height * 4];
			var grass = Frames(modData, "grass_a_base")[0];
			var faces = new[] { Frames(modData, "cliffs/high_sw")[0], Frames(modData, "cliffs/high_se")[0] };
			for (var mask = 0; mask < 16; mask++)
				for (var side = 0; side < 2; side++)
					for (var x = 0; x < 64; x++)
					{
						// Undo the source face's 40px horizontal lean before fitting it
						// to a vertical cell-edge quad. Independent column stretching
						// distorts the rock into long vertical streaks.
						var source = faces[side].Data;
						var u = 0.08 + 0.84 * x / 63.0;
						var dx = side == 0 ? x : 64 + x;
						var frontTop = side == 0 ? 32 + x / 2 : 63 - x / 2;
						var backTop = side == 0 ? 31 - x / 2 : x / 2;
						for (var y = 0; y < 128; y++)
						{
							var v = 0.04 + 0.92 * y / 127.0;
							var sx = (int)(side == 0 ? 55 + 70 * u - 40 * v : 2 + 70 * u + 40 * v);
							var sy = (int)(32 * (side == 0 ? u : 1 - u) + 156 * v);
							var s = (sy * 128 + sx) * 4;
							// Irregular silhouettes can cut into the fitted quad. Extend
							// the nearest opaque pixel horizontally, never transparent RGB.
							for (var radius = 1; source[s + 3] < 250 && radius <= 24; radius++)
								foreach (var candidate in new[] { sx - radius, sx + radius })
									if (candidate >= 0 && candidate < 128 && source[(sy * 128 + candidate) * 4 + 3] >= 250)
									{
										s = (sy * 128 + candidate) * 4;
										break;
									}
							if (source[s + 3] < 250) throw new InvalidDataException("Cliff sampling quad leaves artwork.");
							if ((mask & (1 << side)) != 0)
							{
								var d = ((frontTop + y) * width + mask * 128 + dx) * 4;
								Array.Copy(source, s, pixels, d, 3);
								pixels[d + 3] = 255;
							}
							if (y < 3 && (mask & (4 << side)) != 0)
							{
								var d = ((backTop + y) * width + mask * 128 + dx) * 4;
								Array.Copy(source, s, pixels, d, 3);
								pixels[d + 3] = 255;
							}
						}
					}
			for (var y = 128; y >= 0; y -= 32)
				Blit(pixels, width, height, grass.Data, 128, 64, 16 * 128, y);
			// Upper ground needs normal depth ordering so units behind a plateau
			// are not painted over its top by the low-priority substrate layer.
			Blit(pixels, width, height, grass.Data, 128, 64, 17 * 128, 0);
			// The base sheet is rectangular, not an alpha-masked diamond. Its
			// corners must not cover the faces of neighboring elevated cells.
			for (var y = 0; y < 64; y++)
				for (var x = 0; x < 128; x++)
					if (Math.Abs(x + 0.5 - 64) / 64 + Math.Abs(y + 0.5 - 32) / 32 > 1)
						pixels[(y * width + 17 * 128 + x) * 4 + 3] = 0;
			var png = new Png(pixels, SpriteFrameType.Rgba32, width, height);
			png.EmbeddedData.Add("FrameSize", "128,256");
			png.EmbeddedData.Add("FrameAmount", "18");
			png.EmbeddedData.Add("Offset", "0,96");
			png.Save(path);
		}

		public static void ExportShoreProbes(ModData modData, string path)
		{
			var water = Frames(modData, "water_marker_v01")[0];
			var sand = Frames(modData, "sand_atomic_overlay");
			const int width = 128 * 12;
			var data = new byte[width * 64 * 4];
			for (var i = 0; i < 12; i++)
			{
				Blit(data, width, 64, water.Data, 128, 64, i * 128, 0);
				Blit(data, width, 64, sand[i].Data, 128, 64, i * 128, 0);
			}
			var png = new Png(data, SpriteFrameType.Rgba32, width, 64);
			png.EmbeddedData.Add("FrameSize", "128,64");
			png.EmbeddedData.Add("FrameAmount", "12");
			png.Save(path);
		}

		static ISpriteFrame[] Frames(ModData modData, string name)
		{
			var path = "ca|bits/terrain/rubberduck/" + name + ".png";
			using var stream = modData.DefaultFileSystem.Open(path);
			if (!new PngSheetLoader().TryParseSprite(stream, path, out var frames, out _))
				throw new InvalidDataException(path);
			foreach (var frame in frames)
				if (frame.Type != SpriteFrameType.Rgba32)
					throw new InvalidDataException("Expected RGBA calibration input: " + path);
			return frames;
		}

		static void Blit(byte[] target, int width, int height, byte[] source, int sourceWidth, int sourceHeight, int left, int top)
		{
			for (var y = 0; y < sourceHeight; y++)
				for (var x = 0; x < sourceWidth; x++)
				{
					if (left + x < 0 || left + x >= width || top + y < 0 || top + y >= height)
						continue;
					var s = (y * sourceWidth + x) * 4;
					var d = ((top + y) * width + left + x) * 4;
					var alpha = source[s + 3];
					var destAlpha = target[d + 3];
					var combined = alpha * 255 + destAlpha * (255 - alpha);
					if (combined == 0)
						continue;
					for (var c = 0; c < 3; c++)
						target[d + c] = (byte)((source[s + c] * alpha * 255 + target[d + c] * destAlpha * (255 - alpha)) / combined);
					target[d + 3] = (byte)(combined / 255);
				}
		}
	}
}
