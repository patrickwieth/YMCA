using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Native faces, convex front corner and rock-pile abutments stay unwarped.
	// Rear end masks are explicitly derived alpha transitions, not native end caps.
	static class RubberduckRockCoastArt
	{
		public static string Name(int direction, int variant, int end) => $"coast-native-{direction}-{variant}-{end}";

		public static Dictionary<string, byte[]> Export(Png source, string output, StringBuilder provenance)
		{
			var slots = new[] { 5, 0, 12, 8 };
			var offsets = new[] { "64,64", "-64,64", "-64,-64", "64,-64" };
			var images = new Dictionary<string, byte[]>();
			for (var direction = 0; direction < 4; direction++)
				for (var variant = 0; variant < 3; variant++)
					for (var end = 0; end < 3; end++)
					{
						var slot = slots[direction] + variant;
						var x = slot % 12 * 128; var y = slot / 12 * 256;
						var pixels = new byte[128 * 256 * 4];
						for (var row = 0; row < 256; row++)
							Array.Copy(source.Data, ((y + row) * source.Width + x) * 4, pixels, row * 128 * 4, 128 * 4);
						for (var row = 0; row < 256; row++)
							for (var column = 0; column < 128; column++)
							{
								var distance = end == 1 ? column : end == 2 ? 127 - column : 24;
								var t = Math.Clamp(distance / 24.0, 0, 1);
								var index = (row * 128 + column) * 4 + 3;
								pixels[index] = (byte)Math.Round(pixels[index] * t * t * (3 - 2 * t));
							}
						var crop = new Png(pixels, SpriteFrameType.Rgba32, 128, 256);
						crop.EmbeddedData.Add("FrameSize", "128,256"); crop.EmbeddedData.Add("FrameAmount", "1");
						crop.EmbeddedData.Add("Offset", offsets[direction]);
						var name = Name(direction, variant, end) + ".png";
						crop.Save(Path.Combine(output, name)); images.Add(name, File.ReadAllBytes(Path.Combine(output, name)));
						using var encoded = new MemoryStream(images[name]); var decoded = new Png(encoded);
						for (var row = 0; row < 256; row++)
							for (var column = 0; column < 128; column++)
							{
								var src = ((y + row) * source.Width + x + column) * 4;
								var dst = (row * 128 + column) * 4;
								for (var channel = 0; channel < 3; channel++)
									if (decoded.Data[dst + channel] != source.Data[src + channel]) throw new InvalidDataException("Coast RGB changed.");
								if (decoded.Data[dst + 3] > source.Data[src + 3] ||
									((end == 0 || (column >= 24 && column <= 103)) && decoded.Data[dst + 3] != source.Data[src + 3]) ||
									((end == 1 && column == 0 || end == 2 && column == 127) && decoded.Data[dst + 3] != 0))
									throw new InvalidDataException("Coast end mask violated native interior or endpoint alpha.");
							}
						provenance.AppendLine($"{name}: slot {slot}; rect {x},{y},128,256; offset {offsets[direction]}; " +
							(end == 0 ? "original unwarped crop" : $"derived alpha-only 24px {(end == 1 ? "left" : "right")} end transition; RGB unchanged"));
					}
			// Outer side corners share the bank anchor with both faces. Their
			// source roof apex must meet (+/-64,-64). Inner corners have a
			// different anchor and replace faces: do not reuse their offsets.
			foreach (var piece in new[] { (Slot: 24, Offset: "0,96"), (Slot: 25, Offset: "0,96"), (Slot: 26, Offset: "64,0"), (Slot: 27, Offset: "128,32"), (Slot: 28, Offset: "0,-64"), (Slot: 29, Offset: "0,-96"), (Slot: 30, Offset: "-64,0"), (Slot: 31, Offset: "-128,32") })
			{
				// The rear convex socket needs a solid abutment, not the source's
				// V-shaped rear inner rim. Use the native rock pile without warping it.
				var sourceSlot = piece.Slot == 28 ? 16 : piece.Slot;
				var x = sourceSlot % 12 * 128; var y = sourceSlot / 12 * 256;
				var pixels = new byte[128 * 256 * 4];
				for (var row = 0; row < 256; row++) Array.Copy(source.Data, ((y + row) * source.Width + x) * 4, pixels, row * 128 * 4, 128 * 4);
				var crop = new Png(pixels, SpriteFrameType.Rgba32, 128, 256);
				crop.EmbeddedData.Add("FrameSize", "128,256"); crop.EmbeddedData.Add("FrameAmount", "1"); crop.EmbeddedData.Add("Offset", piece.Offset);
				var name = $"coast-piece-{piece.Slot}.png"; crop.Save(Path.Combine(output, name)); images.Add(name, File.ReadAllBytes(Path.Combine(output, name)));
				provenance.AppendLine($"{name}: source slot {sourceSlot}; rect {x},{y},128,256; offset {piece.Offset}; native corner/abutment, no alpha mask");
			}
			for (var direction = 0; direction < 4; direction++)
			{
				var pixels = new byte[128 * 256 * 4];
				for (var row = 0; row < 256; row++) Array.Copy(source.Data, ((256 + row) * source.Width + 512) * 4, pixels, row * 128 * 4, 128 * 4);
				var crop = new Png(pixels, SpriteFrameType.Rgba32, 128, 256);
				crop.EmbeddedData.Add("FrameSize", "128,256"); crop.EmbeddedData.Add("FrameAmount", "1");
				var offset = new[] { "64,64", "-64,64", "-64,-32", "64,-32" }[direction]; crop.EmbeddedData.Add("Offset", offset);
				var name = $"coast-abutment-{direction}.png"; crop.Save(Path.Combine(output, name)); images.Add(name, File.ReadAllBytes(Path.Combine(output, name)));
				provenance.AppendLine($"{name}: original rock pile slot 16; source rect 512,256,128,256; offset {offset}; unwarped native abutment");
			}
			var rules = new StringBuilder("^RubberduckRockCoast:\n\tAlwaysVisible:\n\tImmobile:\n\t\tOccupiesSpace: false\n\tRenderSprites:\n\tWithSpriteBody:\n\tBodyOrientation:\n\t\tQuantizedFacings: 1\n");
			var sequences = new StringBuilder();
			foreach (var filename in images.Keys)
			{
				var actor = "terrain.rubberduck." + Path.GetFileNameWithoutExtension(filename);
				rules.AppendLine($"{actor}:\n\tInherits: ^RubberduckRockCoast");
				sequences.AppendLine($"{actor}:\n\tidle:\n\t\tFilename: bits/terrain/rubberduck/derived/coasts/{filename}");
			}
			File.WriteAllText(Path.Combine(output, "coast-rules.yaml"), rules.ToString());
			File.WriteAllText(Path.Combine(output, "coast-sequences.yaml"), sequences.ToString());
			return images;
		}
	}
}
