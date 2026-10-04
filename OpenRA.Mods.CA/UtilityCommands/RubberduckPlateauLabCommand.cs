using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class RubberduckPlateauLabCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--rubberduck-plateau-lab";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2 || args.Length == 3 && args[2] == "shaded";

		[Desc("OUTPUT [shaded]", "Build a filled plateau with four real ramp orientations and round-trip movement probes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var output = Path.GetFullPath(args[1]);
			Directory.CreateDirectory(output);
			foreach (var id in new[] { "RUBBERDUCK-TEMPERATE", "RUBBERDUCK-TEMPERATE-SHADED" })
			{
				var terrain = utility.ModData.DefaultTerrainInfo[id];
				if (terrain.DefaultTerrainTile.Type != 1000) throw new InvalidDataException("Grass must remain the default terrain.");
				for (byte ramp = 1; ramp <= 4; ramp++)
					if (terrain.GetTerrainInfo(new TerrainTile((ushort)(14000 + ramp), 0)).RampType != ramp)
						throw new InvalidDataException("Missing engine ramp definition: " + id);
			}
			var grass = Frame(utility.ModData, "grass_a_base", 128, 64);
			var shadedGrass = Frame(utility.ModData, "grass_a_base_shaded", 128, 64);
			var rock = Frame(utility.ModData, "cliffs/high_sw", 128, 192);
			foreach (var variant in new[] { (Source: grass, Suffix: ""), (Source: shadedGrass, Suffix: "-shaded") })
			{
				var pixels = new byte[128 * 256 * 4];
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 128; x++)
					{
						var index = (y * 128 + x) * 4;
						if (variant.Source[index + 3] != 0) Array.Copy(variant.Source, index, pixels, index, 4);
						if (Math.Abs(x + 0.5 - 64) / 64 + Math.Abs(y + 0.5 - 32) / 32 > 1) pixels[index + 3] = 0;
					}
				// Preserve normal roof rendering exactly while allowing shaded roofs
				// to use the same material as the surrounding terrain and ramps.
				if (variant.Suffix == "" && !pixels.SequenceEqual(Frame(utility.ModData, "derived/anchored-cliffs", 128, 256, 17)))
					throw new InvalidDataException("Extracted plateau roof differs from the installed normal roof.");
				var roof = new Png(pixels, SpriteFrameType.Rgba32, 128, 256);
				roof.EmbeddedData.Add("FrameSize", "128,256");
				roof.EmbeddedData.Add("FrameAmount", "1");
				roof.EmbeddedData.Add("Offset", "0,96");
				roof.Save(Path.Combine(output, $"plateau-roof{variant.Suffix}.png"));
			}
			for (byte ramp = 1; ramp <= 4; ramp++)
			{
				var surface = new PlateauSurface(0, ramp);
				var bytes = new byte[128 * 128 * 4];
				var shadedBytes = new byte[bytes.Length];
				var n = 64 - 32 * surface.CornerHeight(0);
				var ay = 96 - 32 * surface.CornerHeight(1) - n;
				var by = 96 - 32 * surface.CornerHeight(3) - n;
				var determinant = 64.0 * (ay + by);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
					{
						var dx = x + 0.5 - 64;
						var dy = y + 0.5 - n;
						var u = (dx * by + 64 * dy) / determinant;
						var v = (64 * dy - dx * ay) / determinant;
						if (u < 0 || u >= 1 || v < 0 || v >= 1) continue;
						var sx = Math.Clamp((int)(64 + 64 * (u - v)), 0, 127);
						var sy = Math.Clamp((int)(32 * (u + v)), 0, 63);
						void Sample(byte[] source, byte[] target)
						{
							var sample = (sy * 128 + sx) * 4;
							// The source diamond's matte is not slope coverage. Resampling
							// it inside the projected slope leaves translucent grid seams.
							// Use nearby opaque grass for fringe RGB, not its matte color.
							if (source[sample + 3] != 255)
							{
								var innerU = Math.Clamp(u, 0.06, 0.94);
								var innerV = Math.Clamp(v, 0.06, 0.94);
								sample = ((int)(32 * (innerU + innerV)) * 128 + (int)(64 + 64 * (innerU - innerV))) * 4;
							}
							if (source[sample + 3] != 255) throw new InvalidDataException("Ramp source has no opaque interior sample.");
							var destination = (y * 128 + x) * 4;
							Array.Copy(source, sample, target, destination, 3);
							target[destination + 3] = 255;
						}
						Sample(grass, bytes);
						Sample(shadedGrass, shadedBytes);
					}
				foreach (var variant in new[] { (Data: bytes, Suffix: ""), (Data: shadedBytes, Suffix: "-shaded") })
				{
					var png = new Png(variant.Data, SpriteFrameType.Rgba32, 128, 128);
					png.EmbeddedData.Add("FrameSize", "128,128");
					png.EmbeddedData.Add("FrameAmount", "1");
					png.EmbeddedData.Add("Offset", "0,-32");
					var path = Path.Combine(output, $"plateau-ramp{ramp}{variant.Suffix}.png");
					png.Save(path);
					using var saved = File.OpenRead(path);
					var decoded = new Png(saved);
					var opaque = 0;
					for (var pixel = 3; pixel < decoded.Data.Length; pixel += 4)
					{
						if (decoded.Data[pixel] == 255) opaque++;
						else if (decoded.Data[pixel] != 0) throw new InvalidDataException("Slope retained source matte alpha.");
					}
					if (opaque != (ramp <= 2 ? 2048 : 6144) || !decoded.Data.SequenceEqual(variant.Data))
						throw new InvalidDataException("Slope coverage or PNG roundtrip changed.");
				}
			}
			File.WriteAllText(Path.Combine(output, "plateau-ramp-provenance.txt"),
				"Derived affine grass slopes, not native ramp artwork. Source frames: grass_a_base.png / grass_a_base_shaded.png, first 128x64 RGBA frame.\n" +
				"Normal frame SHA256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(grass)) + "\n" +
				"Shaded frame SHA256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(shadedGrass)) + "\n" +
				"Projected pixel-center coverage is opaque inside, transparent outside. Source matte fringe RGB samples use u/v clamped to 0.06..0.94.\n" +
				"Eight decoded PNG checks: exact RGBA roundtrip; 2048 opaque pixels for ramps 1/2, 6144 for ramps 3/4; no partial-alpha pixels.\n" +
				"Two plateau-roof PNGs: source grass diamond at 0,0 in a 128x256 canvas, Offset 0,96. Normal RGBA exactly equals anchored-cliffs frame 17; shaded uses its own grass frame.\n");
			RubberduckPlateauRenderer.ExportSprites(output, rock);
			Export(utility.ModData, output, rock, args.Length == 3);
			Console.WriteLine("Plateau geometry, ramps and reload checked: " + output);
		}

		static byte[] Frame(ModData modData, string name, int width, int height, int frame = 0)
		{
			var path = "ca|bits/terrain/rubberduck/" + name + ".png";
			using var stream = modData.DefaultFileSystem.Open(path);
			if (!new PngSheetLoader().TryParseSprite(stream, path, out var frames, out _) ||
				frames.Length <= frame || frames[frame].Type != SpriteFrameType.Rgba32 || frames[frame].Size.Width != width || frames[frame].Size.Height != height)
				throw new InvalidDataException("Unexpected calibration texture: " + path);
			return frames[frame].Data;
		}

		static void Export(ModData modData, string output, byte[] rock, bool shaded)
		{
			var result = Path.Combine(Path.GetTempPath(), "ymca-terrain-probes", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(output))) + ".log");
			Directory.CreateDirectory(Path.GetDirectoryName(result));
			File.WriteAllText(result, "");
			File.WriteAllText(Path.Combine(output, "runtime-results-path.txt"), result);
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo[shaded ? "RUBBERDUCK-TEMPERATE-SHADED" : "RUBBERDUCK-TEMPERATE"], 98, 130);
			map.Title = "Rubberduck Walkable Plateau - Four Ramps";
			map.Author = "YMCA terrain calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(96, 128));
			map.PlayerDefinitions = new MapPlayers(map.Rules, 2).ToMiniYaml();
			var center = new MPos(48, 72).ToCPos(map);
			var cells = PlateauTopology.CalibrationPlateau(center);
			foreach (var cell in map.AllCells) map.Tiles[cell] = new TerrainTile(1000, 0);
			foreach (var pair in cells)
			{
				var surface = pair.Value;
				map.Tiles[pair.Key] = new TerrainTile(surface.Blocked ? (ushort)3992 : surface.Ramp == 0 ? (ushort)1000 : (ushort)(14000 + surface.Ramp), 0);
				map.Height[pair.Key] = surface.Height;
				// Check the shared surface model against the actual engine ramp geometry.
				for (var corner = 0; corner < 4; corner++)
					if (map.Grid.Ramps[surface.Ramp].Corners[corner].Z != 724 * (surface.CornerHeight(corner) - surface.Height))
						throw new InvalidDataException("Surface/engine ramp corner mismatch.");
			}
			var actors = new List<MiniYamlNode>();
			void Add(string type, CPos cell, string owner = "Neutral")
			{
				var actor = new ActorReference(type) { new LocationInit(cell), new OwnerInit(owner) };
				actors.Add(new MiniYamlNode("Plateau" + actors.Count, actor.Save()));
			}
			var rules = new StringBuilder($"World:\n\tTerrainCalibrationView:\n\t\tCenter: {center + new CVec(3, -3)}\n\t\tZoomSteps: -4\n\t\tMinimumZoomScale: 0.66\n\t\tScreenshotTicks: 200\nPlayer:\n\tShroud:\n\t\tFogCheckboxEnabled: false\n\t\tFogCheckboxLocked: true\n\t\tExploredMapCheckboxEnabled: true\n\t\tExploredMapCheckboxLocked: true\n");
			var sequences = new StringBuilder();
			var frames = new List<byte[]>();
			foreach (var pair in cells)
			{
				Add("terrain.rubberduck.cliff16", pair.Key);
				if (pair.Value.Ramp == 0) Add("terrain.rubberduck.cliff17", pair.Key);
				var data = RubberduckPlateauRenderer.ComposeWalls(pair.Key, pair.Value, cells, rock);
				if (!data.Any(b => b != 0)) continue;
				var index = frames.Count;
				frames.Add(data);
				var name = "calibration.plateauwall" + index;
				rules.AppendLine($"{name}:\n\tInherits: ^RubberduckTerrainDecoration");
				// Actors include the ramp center offset; terrain sprites deliberately do not.
				sequences.AppendLine($"{name}:\n\tidle:\n\t\tFilename: plateau-walls.png\n\t\tStart: {index}\n\t\tLength: 1\n\t\tOffset: 0,{(pair.Value.Ramp == 0 ? 0 : 16)}");
				Add(name, pair.Key);
			}
			for (var direction = 0; direction < 4; direction++)
				for (var kind = 0; kind < 2; kind++)
				{
					var outward = PlateauTopology.Directions[direction];
					var lateral = PlateauTopology.Directions[(direction + 1) % 4] * (kind == 0 ? -1 : 1);
					var start = center + outward * 9 + lateral;
					var target = center + outward * 2 + lateral;
					var name = $"calibration.plateauprobe{direction}-{kind}";
					rules.AppendLine($"{name}:\n\tInherits: {(kind == 0 ? "Light_Infantry" : "Heavy_Tank")}\n\tRenderSprites:\n\t\tImage: {(kind == 0 ? "light_infantry" : "heavytank")}\n\tPlateauMovementProbe:\n\t\tTarget: {target}\n\t\tRamp: {(direction + 2) % 4 + 1}\n\t\tCliffTop: {center + outward * 7 + lateral * 5}\n\t\tCliffBottom: {center + outward * 8 + lateral * 5}\n\t\tResultPath: {result}");
					Add(name, start, "Multi0");
				}
			Add("mpspawn", new MPos(8, 24).ToCPos(map));
			Add("mpspawn", new MPos(90, 120).ToCPos(map));
			map.ActorDefinitions = actors;
			map.RuleDefinitions = new MiniYaml("", MiniYaml.FromString(rules.ToString(), "plateau-rules"));
			map.SequenceDefinitions = new MiniYaml("", MiniYaml.FromString(sequences.ToString(), "plateau-sequences"));
			var pixels = new byte[128 * 320 * frames.Count * 4];
			for (var i = 0; i < frames.Count; i++) Array.Copy(frames[i], 0, pixels, i * 128 * 320 * 4, frames[i].Length);
			var image = new Png(pixels, SpriteFrameType.Rgba32, 128, 320 * frames.Count);
			image.EmbeddedData.Add("FrameSize", "128,320");
			image.EmbeddedData.Add("FrameAmount", frames.Count.ToString());
			image.EmbeddedData.Add("Offset", "0,96");
			image.Save(Path.Combine(output, "plateau-walls.png"));
			var path = Path.Combine(output, "rubberduck-walkable-plateau.oramap");
			if (File.Exists(path)) File.Delete(path);
			using (var package = ZipFileLoader.Create(path))
			{
				package.Update("plateau-walls.png", File.ReadAllBytes(Path.Combine(output, "plateau-walls.png")));
				map.Save(package);
			}
			using var stream = File.OpenRead(path);
			using var saved = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var reopened = new Map(modData, saved);
			if (reopened.InvalidCustomRules) throw new InvalidDataException("Plateau custom rules failed.", reopened.InvalidCustomRulesException);
			foreach (var pair in cells)
				if (reopened.Height[pair.Key] != pair.Value.Height || reopened.Ramp[pair.Key] != pair.Value.Ramp ||
					reopened.Tiles[pair.Key].Type != map.Tiles[pair.Key].Type || reopened.Tiles[pair.Key].Index != map.Tiles[pair.Key].Index)
					throw new InvalidDataException("Plateau height/ramp roundtrip mismatch.");
		}

	}
}
