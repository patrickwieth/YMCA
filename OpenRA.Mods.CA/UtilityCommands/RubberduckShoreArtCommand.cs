using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckShoreArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-shore-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3;
		[Desc("SOURCE OUTPUT", "Audit authored ground-edge sockets and export reproducible shoreline compositions without terrain changes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var root = Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);
			var output = Path.GetFullPath(args[2]);
			if (output.Equals(root, StringComparison.OrdinalIgnoreCase) || output.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
				throw new ArgumentException("Derived artwork must not overwrite the source tree.");
			Directory.CreateDirectory(output);
			var topologies = new SortedSet<int>();
			for (var e = 0; e < 16; e++)
				for (var d = 0; d < 16; d++)
				{
					var key = RubberduckShoreTopology.Normalize(e, d);
					var rotated = RubberduckShoreTopology.Normalize(((e << 1) | (e >> 3)) & 15, ((d << 1) | (d >> 3)) & 15);
					var expected = (((key & 15) << 1 | (key & 15) >> 3) & 15) | ((((key >> 4) << 1 | (key >> 4) >> 3) & 15) << 4);
					if (rotated != expected || RubberduckShoreTopology.Normalize(key & 15, key >> 4) != key)
						throw new InvalidDataException("Shore classification lost rotation or normalization parity.");
					topologies.Add(key);
				}
			if (topologies.Count != 47) throw new InvalidDataException("Expected the complete 47-case corner/edge topology.");
			var provenance = new StringBuilder("Derived intersection of authored RGBA ground-edge pieces; not original complete 47-case artwork, not invented foam. No terrain/collision modifications. No ruin assets. Frames 40..47 are half-cell wedges, excluded from full-cell coast composition to preserve visible land centers.\n");
			var report = new StringBuilder("material\tshaded\ttopology\tvariant\tdonors\tcenter-alpha\n");
			var atlasPixels = new byte[1024 * 3840 * 4];
			var materials = new[] { ("grass", 0, "grass-a"), ("grass", 2, "grass-b"), ("sand", 0, "sand"), ("dirt", 0, "dirt-a"), ("dirt", 2, "dirt-b") };
			var sources = new Dictionary<string, Png>();
			foreach (var name in new[] { "grass", "sand", "dirt" })
			{
				var path = Path.Combine(root, "grassland", name + "_tiles.png");
				using var input = File.OpenRead(path);
				var png = new Png(input);
				if (png.Type != SpriteFrameType.Rgba32 || png.Width != 1024 || png.Height != (name == "sand" ? 768 : 1536))
					throw new InvalidDataException("Unexpected authored material atlas: " + path);
				sources.Add(name, png);
				provenance.AppendLine($"grassland/{name}_tiles.png SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}");
			}

			var cases = 0;
			for (var material = 0; material < materials.Length; material++)
				for (var shaded = 0; shaded < 2; shaded++)
				{
					var (name, block, label) = materials[material];
					var source = sources[name];
					var frames = new byte[48][];
					for (var f = 0; f < 48; f++)
					{
						frames[f] = new byte[128 * 64 * 4];
						var index = (block + shaded) * 48 + f;
						var destination = (material * 2 + shaded) * 48 + f;
						for (var y = 0; y < 64; y++)
						{
							Array.Copy(source.Data, ((index / 8 * 64 + y) * 1024 + index % 8 * 128) * 4, frames[f], y * 128 * 4, 128 * 4);
							Array.Copy(frames[f], y * 128 * 4, atlasPixels, ((destination / 8 * 64 + y) * 1024 + destination % 8 * 128) * 4, 128 * 4);
						}
					}
					var gallery = new byte[1024 * 1536 * 4];
					var cell = 0;
					foreach (var topology in topologies)
						for (var variant = 0; variant < 4; variant++, cell++)
						{
							var pixels = RubberduckShoreTopology.Compose(frames, topology, variant);
							var exclusion = Enumerable.Range(0, 128 * 64).Select(p => pixels[p * 4 + 3] == 0).ToArray();
							var covered = new byte[128 * 64];
							foreach (var rectangle in RubberduckMaterialLayer.ExclusionRectangles(exclusion))
								for (var y = rectangle.Y; y < rectangle.Bottom; y++)
									for (var x = rectangle.X; x < rectangle.Right; x++) covered[y * 128 + x]++;
							for (var p = 0; p < covered.Length; p++)
								if (covered[p] != (exclusion[p] ? 0 : 1)) throw new InvalidDataException("Shore-fringe clipping overlapped or lost pixels.");
							var centerAlpha = pixels[(32 * 128 + 64) * 4 + 3];
							if (centerAlpha < 128) throw new InvalidDataException($"Shore erases a land cell center: {label}/{shaded}/{topology}/{variant}.");
							var donors = RubberduckShoreTopology.SourceFrames(topology, variant);
							if (donors.Length == 0) donors = new[] { variant };
							for (var p = 0; p < pixels.Length; p += 4)
								if (!donors.Any(d => Enumerable.Range(0, 4).All(c => frames[d][p + c] == pixels[p + c])))
									throw new InvalidDataException("Composition invented a non-source RGBA pixel.");
							for (var y = 0; y < 64; y++) Array.Copy(pixels, y * 128 * 4, gallery, ((cell / 8 * 64 + y) * 1024 + cell % 8 * 128) * 4, 128 * 4);
							report.AppendLine($"{label}\t{shaded}\t{topology}\t{variant}\t{string.Join(",", donors)}\t{centerAlpha}");
							cases++;
						}
					new Png(gallery, SpriteFrameType.Rgba32, 1024, 1536).Save(Path.Combine(output, $"gallery-{label}-{shaded}.png"));
				}
			var atlas = new Png(atlasPixels, SpriteFrameType.Rgba32, 1024, 3840);
			atlas.EmbeddedData.Add("FrameSize", "128,64"); atlas.EmbeddedData.Add("FrameAmount", "480");
			var result = Path.Combine(output, "ground-edge-sources.png"); atlas.Save(result);
			provenance.AppendLine("Blocks: grass-a normal/shaded, grass-b normal/shaded, sand normal/shaded, dirt-a normal/shaded, dirt-b normal/shaded. Each 48 frames; source RGBA is unchanged.");
			provenance.AppendLine($"ground-edge-sources.png SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(result)))}");
			File.WriteAllText(Path.Combine(output, "provenance.txt"), provenance.ToString());
			File.WriteAllText(Path.Combine(output, "shore-sockets.tsv"), report.ToString());
			Console.WriteLine($"PASS: {cases} compositions, 47 topologies, ten material/lighting blocks; exact source RGBA samples, majority-opaque land centers and exact fringe clipping coverage.");
		}
	}
}
