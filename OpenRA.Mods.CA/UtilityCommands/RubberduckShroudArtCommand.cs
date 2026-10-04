using System;
using System.IO;
using System.Security.Cryptography;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckShroudArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-shroud-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2 || args.Length == 3 && args[2] == "fog";

		static double Coverage(int mask, double u, double v)
		{
			u = Math.Clamp(u, 0, 1); v = Math.Clamp(v, 0, 1);
			var value = ((mask & 1) != 0 ? (1 - u) * (1 - v) : 0) +
				((mask & 2) != 0 ? u * (1 - v) : 0) +
				((mask & 4) != 0 ? u * v : 0) + ((mask & 8) != 0 ? (1 - u) * v : 0);
			var t = Math.Clamp((value - 0.25) * 2, 0, 1);
			return t * t * (3 - 2 * t);
		}

		[Desc("OUTPUT [fog]", "Generate cell-aligned procedural Rubberduck shroud/fog masks without changing visibility or original artwork.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(output);
			var name = args.Length == 3 ? "fog" : "shroud";
			var sourcePath = "ca|bits/misc/overlays/" + name + "-iso.png";
			using var stream = utility.ModData.DefaultFileSystem.Open(sourcePath);
			using var bytes = new MemoryStream();
			stream.CopyTo(bytes);
			var original = bytes.ToArray();
			using var input = new MemoryStream(original);
			var source = new Png(input);
			if (source.Width != 1024 || source.Height != 384 || source.Type != SpriteFrameType.Rgba32)
				throw new InvalidDataException("Expected the 48-frame 128x64 RGBA source atlas.");
			var sourceOpacity = source.Data[(32 * 1024 + 64) * 4 + 3];
			// RGBA sprites bypass ShroudPalette's indexed fog alpha (128). Copying the
			// opaque source alpha made explored-but-unseen terrain completely black.
			var opacity = name == "fog" ? (byte)128 : sourceOpacity;
			if (name == "shroud" && opacity != 255) throw new InvalidDataException("Full shroud must be opaque.");
			for (var mask = 0; mask < 16; mask++)
				for (var x = 0; x <= 32; x++)
					for (var y = 0; y <= 32; y++)
					{
						var a = Coverage(mask, x / 32.0, y / 32.0);
						var rotated = (mask << 1 | mask >> 3) & 15;
						if (a < 0 || a > 1 || Math.Abs(a - Coverage(rotated, 1 - y / 32.0, x / 32.0)) > 0.000001)
							throw new InvalidDataException("Shroud coverage failed rotation/range validation.");
						if (Coverage(0, x / 32.0, y / 32.0) != 0 || Coverage(15, x / 32.0, y / 32.0) != 1)
							throw new InvalidDataException("Fully visible/hidden coverage changed.");
						for (var bit = 0; bit < 4; bit++)
							if (Coverage(mask | 1 << bit, x / 32.0, y / 32.0) < a)
								throw new InvalidDataException("Hiding a corner reduced opacity.");
					}
			for (var left = 0; left < 16; left++)
				for (var right = 0; right < 16; right++)
					if (((left >> 1 & 1) == (right & 1)) && ((left >> 2 & 1) == (right >> 3 & 1)))
						for (var step = 0; step <= 32; step++)
							if (Math.Abs(Coverage(left, 1, step / 32.0) - Coverage(right, 0, step / 32.0)) > 0.000001)
								throw new InvalidDataException("Adjacent shroud fields disagree at their shared edge.");
			// Keep the engine's existing sequence frame/index correspondence.
			var masks = new[] { 12, 9, 6, 3, 11, 7, 13, 14, 4, 8, 2, 1 };
			var pixels = new byte[1024 * 384 * 4];
			for (var frame = 0; frame < 48; frame++)
			{
				var mask = frame < 16 ? 15 : masks[frame < 32 ? (frame - 16) / 4 : 4 + (frame - 32) / 2];
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 128; x++)
					{
						var u = (x + 0.5) / 128 + (y + 0.5) / 64 - 0.5;
						var v = (y + 0.5) / 64 - (x + 0.5) / 128 + 0.5;
						// Continue the corner field outside the diamond, rather than
						// opaque rectangle corners that occlude adjacent visible cells.
						pixels[((frame / 8 * 64 + y) * 1024 + frame % 8 * 128 + x) * 4 + 3] =
							(byte)Math.Round(opacity * Coverage(mask, u, v));
					}
			}
			var result = new Png(pixels, SpriteFrameType.Rgba32, 1024, 384);
			result.EmbeddedData["FrameSize"] = "128,64";
			result.EmbeddedData["FrameAmount"] = "48";
			var path = Path.Combine(output, name + "-seams.png");
			result.Save(path);
			using var check = File.OpenRead(path);
			var reload = new Png(check);
			for (var i = 0; i < pixels.Length; i++)
				if (reload.Data[i] != pixels[i]) throw new InvalidDataException("PNG roundtrip changed RGBA.");
			File.WriteAllLines(Path.Combine(output, name + "-seams-provenance.txt"), new[]
			{
				"Procedural overlay, NOT native/unwarped artist artwork. Original files unchanged.",
				"Reference opacity/layout source: " + sourcePath,
				"Reference SHA256: " + Convert.ToHexString(SHA256.HashData(original)),
				"Full opacity: " + opacity + " (source alpha " + sourceOpacity + "; RGBA fog uses the standard indexed fog palette's 128 alpha); 48 frames of 128x64; existing sequence frame/index mapping retained.",
				"Positive black-corner bilinear field in CPos-aligned u/v; smoothstep from 0.25 to 0.75. Clamp u/v outside the diamond to continue its edge field.",
				"Validated all 16 masks for quarter-turn symmetry, range, monotone visibility and matching shared edges; exact RGBA PNG roundtrip. Runtime/shroud screenshots required before acceptance."
			});
			Console.WriteLine(path);
		}
	}
}
