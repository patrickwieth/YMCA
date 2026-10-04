using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	// This is a geometry-normalized import, not a claim that atlas slots are ready-made tiles.
	sealed class RubberduckNativeCliffArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-native-cliff-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 3 || args.Length == 4;

		[Desc("SOURCE OUTPUT [MAP]", "Build source-traceable, orientation-specific plateau wall artwork from original grassland sheets.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var source = Path.GetFullPath(args[1]);
			var output = Path.GetFullPath(args[2]);
			var relative = Path.GetRelativePath(source, output);
			if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
				throw new ArgumentException("Output must be outside the original source directory.");
			Directory.CreateDirectory(output);
			Png Load(string name)
			{
				using var stream = File.OpenRead(Path.Combine(source, "grassland", name));
				var png = new Png(stream);
				if (png.Type != SpriteFrameType.Rgba32 || png.Width != 1536 ||
					png.Height != (name == "grassland_1x1.png" ? 3840 : 2048))
					throw new InvalidDataException("Unexpected original cliff sheet: " + name);
				return png;
			}
			var high = Load("grassland_1x1.png");
			var low = Load("rock_cliffs.png");
			const int width = 1280;
			var data = new byte[width * 9600 * 4];
			var audit = new StringBuilder("frame\tdirection\tdrop-a\tdrop-b\tvariant\tsheet\tslot\n");
			var cornerX = new[] { 64.0, 128, 64, 0 };
			var cornerY = new[] { 32.0, 64, 96, 64 };
			for (var variant = 0; variant < 3; variant++)
				for (var direction = 0; direction < 4; direction++)
					for (var a = 0; a <= 4; a++)
						for (var b = 0; b <= 4; b++)
						{
							var frame = variant * 100 + direction * 25 + a * 5 + b;
							var southwest = direction == 1 || direction == 3;
							var rear = direction >= 2;
							var slot = rear ? (direction == 2 ? 12 : 8) + variant : southwest ? variant : 5 + variant;
							var sheet = rear ? low : high;
							audit.AppendLine($"{frame}\t{direction}\t{a}\t{b}\t{variant}\t{(rear ? "rock_cliffs.png" : "grassland_1x1.png")}\t{slot}");
							if (a + b == 0) continue;
							var edge = PlateauTopology.Edge(direction);
							for (var x = 0; x < 128; x++)
							{
								var t = (x + 0.5 - cornerX[edge.A]) / (cornerX[edge.B] - cornerX[edge.A]);
								if (t < 0 || t > 1) continue;
								var top = cornerY[edge.A] + t * (cornerY[edge.B] - cornerY[edge.A]);
								var bottom = top + Math.Min(direction >= 2 ? 2 : 128, 32 * (a + t * (b - a)));
								var u = 0.08 + 0.84 * (x % 64 + 0.5) / 64;
								if (rear)
								{
									// Sample the native rear silhouette, rather than slicing a front
									// face into a straight two-pixel wire. The small lip stays within
									// six pixels above/two below the existing geometric roof edge.
									var sx = (int)(128 * u);
									var sourceTop = direction == 2 ? 160 - sx / 2.0 : 96 + sx / 2.0;
									var rise = Math.Min(6, 32 * (a + t * (b - a)));
									for (var y = (int)Math.Ceiling(top - rise - 0.5); y < Math.Ceiling(bottom - 0.5); y++)
									{
										var sy = (int)Math.Round(sourceTop + 2 * (y + 0.5 - top));
										var sample = ((slot / 12 * 256 + sy) * sheet.Width + slot % 12 * 128 + sx) * 4;
										bool IsRim(int index) => sheet.Data[index + 3] >= 250 &&
											Math.Max(sheet.Data[index], Math.Max(sheet.Data[index + 1], sheet.Data[index + 2])) > 8;
										if (!IsRim(sample))
										{
											if (y + 0.5 < top) continue;
											// Retain a continuous two-pixel contact strip where the
											// source's leaning ends leave the sampled silhouette.
											var innerX = Math.Clamp(sx, 32, 95);
											var innerY = (int)(direction == 2 ? 160 - innerX / 2.0 : 96 + innerX / 2.0) + 8;
											sample = ((slot / 12 * 256 + innerY) * sheet.Width + slot % 12 * 128 + innerX) * 4;
											if (!IsRim(sample)) throw new InvalidDataException("Rear rim contact sample leaves native rock.");
										}
										var dest = ((frame / 10 * 320 + y) * width + frame % 10 * 128 + x) * 4;
										// The top outline contains pale earth. Retain its exact alpha,
										// but take the rim material from rock just inside that outline.
										var materialX = Math.Clamp(sx, 32, 95);
										var materialY = (int)Math.Round((direction == 2 ? 160 - materialX / 2.0 : 96 + materialX / 2.0) + 12 + 2 * (y + 0.5 - top));
										var material = ((slot / 12 * 256 + materialY) * sheet.Width + slot % 12 * 128 + materialX) * 4;
										if (!IsRim(material)) material = sample;
										Array.Copy(sheet.Data, material, data, dest, 3);
										data[dest + 3] = sheet.Data[sample + 3];
									}
									continue;
								}
								for (var y = (int)Math.Ceiling(top - 0.5); y < Math.Ceiling(bottom - 0.5); y++)
								{
									// A ramp exposes less of the same height-four wall. Crop its
									// material; do not shrink strata or switch sheets as the drop
									// decreases from four to three/two/one beside the ascent.
									var v = 0.04 + 0.88 * Math.Clamp((y + 0.5 - top) / 128, 0, 1);
									const int lean = 64;
									var sx = (int)Math.Round(southwest ? 64 + 64 * u - lean * v : 64 * u + lean * v);
									var sy = (int)Math.Round(32 + 32 * (southwest ? u : 1 - u) + 192 * v);
									sx = Math.Clamp(sx, 0, 127); sy = Math.Clamp(sy, 0, 255);
									var sample = ((slot / 12 * 256 + sy) * sheet.Width + slot % 12 * 128 + sx) * 4;
									bool IsRock(int index) => sheet.Data[index + 3] >= 250 &&
										Math.Max(sheet.Data[index], Math.Max(sheet.Data[index + 1], sheet.Data[index + 2])) > 8;
									// The source has near-black opaque matte pixels around some silhouettes.
									// They must not become black seams when fitting adjacent terrain faces.
									for (var radius = 1; !IsRock(sample) && radius <= 16; radius++)
										foreach (var candidate in new[] { sx - radius, sx + radius })
											if (candidate >= 0 && candidate < 128)
											{
												var index = ((slot / 12 * 256 + sy) * sheet.Width + slot % 12 * 128 + candidate) * 4;
												if (IsRock(index)) { sample = index; break; }
											}
									if (!IsRock(sample))
										throw new InvalidDataException($"Native face leaves its source silhouette: frame {frame}, {sx},{sy}.");
									var dest = ((frame / 10 * 320 + y) * width + frame % 10 * 128 + x) * 4;
									Array.Copy(sheet.Data, sample, data, dest, 3);
									data[dest + 3] = 255;
								}
							}
						}
			using (var installedStream = utility.ModData.DefaultFileSystem.Open("ca|bits/terrain/rubberduck/derived/native-cliff-faces.png"))
			{
				var installed = new Png(installedStream);
				if (installed.Width != width || installed.Height != 9600) throw new InvalidDataException("Unexpected installed face atlas.");
				for (var frame = 0; frame < 300; frame++)
				{
					var direction = frame % 100 / 25;
					var a = frame % 25 / 5; var b = frame % 5;
					var edge = PlateauTopology.Edge(direction);
					for (var x = 0; x < 128; x++)
					{
						var t = (x + 0.5 - cornerX[edge.A]) / (cornerX[edge.B] - cornerX[edge.A]);
						var top = cornerY[edge.A] + t * (cornerY[edge.B] - cornerY[edge.A]);
						var drop = 32 * (a + t * (b - a));
						for (var y = 0; y < 320; y++)
						{
							var index = ((frame / 10 * 320 + y) * width + frame % 10 * 128 + x) * 4;
							if (direction < 2)
							{
								if (data[index + 3] != installed.Data[index + 3]) throw new InvalidDataException("Wall material correction changed its silhouette.");
								var referenceFrame = frame / 100 * 100 + direction * 25 + 24;
								var reference = ((referenceFrame / 10 * 320 + y) * width + referenceFrame % 10 * 128 + x) * 4;
								for (var channel = 0; channel < 4; channel++)
								{
									if (data[index + channel] != installed.Data[index + channel])
										throw new InvalidDataException("Rear rim correction changed a front wall.");
									if (data[index + 3] != 0 && data[index + channel] != data[reference + channel])
										throw new InvalidDataException("Short wall strata do not match the full-height reference.");
								}
								continue;
							}
							if (data[index + 3] != installed.Data[index + 3]) throw new InvalidDataException("Rear rim material correction changed its silhouette.");
							var within = t >= 0 && t <= 1 && a + b != 0 && y + 0.5 >= top - Math.Min(6, drop) && y + 0.5 < top + Math.Min(2, drop);
							if (!within && data[index + 3] != 0) throw new InvalidDataException("Rear rim exceeds its bounded silhouette.");
							if (within && y + 0.5 >= top && data[index + 3] == 0) throw new InvalidDataException("Rear rim contact strip has a gap.");
						}
					}
				}
			}
			File.WriteAllText(Path.Combine(output, "rim-preflight.txt"),
				"PASS all 150 front frames unchanged RGBA; each visible pixel matches its full-height material reference; all 150 rear profiles retain their six-above/two-below bounds and continuous contact.\n" +
				"Rear profiles bounded to six pixels above/two below the roof with continuous contact and empty zero-drop profiles.\n" +
				"Front art: grassland_1x1 slots 0-2 and 5-7 for every drop; roof-relative vertical sampling uses constant denominator 128. Short profiles crop the same strata, never shrink or change material.\n" +
				"Rear art: derived roof-relative sampling of original rock_cliffs slots 8-10 and 12-14. Native alpha silhouette retained above roof; inward rock samples extend contact at leaning ends.\n" +
				"Sampling: source x = floor(128 * (0.08 + 0.84 * edgeFraction)); rear roof y = 160-x/2 or 96+x/2; vertical sampling scale 2. RGB samples the same rock sheet 12 source pixels inward with x clamped to 32..95; alpha unchanged. Contact fallback clamps x to 32..95 and samples roof+8.\n");
			var result = new Png(data, SpriteFrameType.Rgba32, width, 9600);
			result.EmbeddedData.Add("FrameSize", "128,320");
			result.EmbeddedData.Add("FrameAmount", "300");
			result.EmbeddedData.Add("Offset", "0,96");
			result.Save(Path.Combine(output, "native-cliff-faces.png"));
			var sequences = new StringBuilder();
			for (var i = 0; i < 100; i++)
				sequences.AppendLine($"terrain.rubberduck.plateauwall{i}:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/native-cliff-faces.png\n\t\tStart: 0\n\t\tFrames: {i}, {i + 100}, {i + 200}\n\t\tLength: 3\n");
			File.WriteAllText(Path.Combine(output, "native-cliff-sequences.yaml"), sequences.ToString());
			File.WriteAllText(Path.Combine(output, "native-cliff-frames.tsv"), audit.ToString());
			var hashes = new StringBuilder();
			foreach (var name in new[] { "grassland_1x1.png", "rock_cliffs.png" })
				hashes.AppendLine(name + "\t" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(source, "grassland", name)))));
			File.WriteAllText(Path.Combine(output, "native-cliff-sources.sha256"), hashes.ToString());
			File.AppendAllText(Path.Combine(output, "rim-preflight.txt"), hashes.ToString() +
				"Atlas SHA256: " + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, "native-cliff-faces.png")))) + "\n");
			var fixture = Path.Combine(output, "fixture");
			if (args.Length == 3)
				((IUtilityCommand)new RubberduckGeneratedPlateauTestCommand()).Run(utility,
					new[] { "--rubberduck-generated-plateau-test", fixture, "players=4", "seed=42", "mode=tactical" });
			var originalPath = args.Length == 4 ? Path.GetFullPath(args[3]) : Path.Combine(fixture, "rubberduck-generated-plateau-test.oramap");
			using var originalStream = File.OpenRead(originalPath);
			using var original = new ZipFileLoader.ReadOnlyZipFile(originalStream, originalPath);
			using var map = new Map(utility.ModData, original);
			if (map.SequenceDefinitions != null && (!map.SequenceDefinitions.Nodes.IsDefaultOrEmpty || !string.IsNullOrEmpty(map.SequenceDefinitions.Value))) throw new InvalidDataException("Preview input must use shared art, not map-local sequence overrides.");
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString().Replace(
				"ca|bits/terrain/rubberduck/derived/native-cliff-faces.png", "native-cliff-faces.png"), "native-cliff-preview"));
			var candidatePath = Path.Combine(output, "rubberduck-native-cliff-preview.oramap");
			using (var package = ZipFileLoader.Create(candidatePath))
			{
				map.Save(package);
				package.Update("native-cliff-faces.png", File.ReadAllBytes(Path.Combine(output, "native-cliff-faces.png")));
			}
			using var checkStream = File.OpenRead(candidatePath);
			using var checkPackage = new ZipFileLoader.ReadOnlyZipFile(checkStream, candidatePath);
			using var check = new Map(utility.ModData, checkPackage);
			if (check.InvalidCustomRules) throw new InvalidDataException("Native artwork fixture failed reload.", check.InvalidCustomRulesException);
			Console.WriteLine("Derived native cliff faces (original source pixels, geometry normalized): " + output);
		}
	}
}
