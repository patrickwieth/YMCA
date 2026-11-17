using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.MapGeneration
{
	// Isolated experiment: shared land vertices determine the coast before sprites.
	// No global tileset changes or production-export hooks.
	static class RubberduckShoreCalibration
	{
		public static void Export(ModData modData, string output)
		{
			ValidateEdges();
			var png = Compose(modData);
			var imagePath = Path.Combine(output, "calibration-coast.png");
			png.Save(imagePath);
			foreach (var shape in new[] { "island", "bay", "lake", "saddle", "saddle-east", "irregular" })
				ExportMap(modData, output, shape, File.ReadAllBytes(imagePath));
		}

		// N, E, S, W vertices in screen space; u/v follow the CPos axes.
		static double Field(int mask, double u, double v)
		{
			double n = mask & 1, e = (mask >> 1) & 1, s = (mask >> 2) & 1, w = (mask >> 3) & 1;
			if (mask == 5 || mask == 10)
			{
				// Ambiguous opposite corners remain disconnected. A water-valued
				// center avoids a visual land bridge through an impassable cell.
				if (v <= u && v <= 1 - u) return n * (1 - u - v) + e * (u - v);
				if (u >= v && u >= 1 - v) return e * (u - v) + s * (u + v - 1);
				if (v >= u && v >= 1 - u) return s * (u + v - 1) + w * (v - u);
				return w * (v - u) + n * (1 - u - v);
			}
			return n * (1 - u) * (1 - v) + e * u * (1 - v) + s * u * v + w * (1 - u) * v;
		}

		static void ValidateEdges()
		{
			for (var a = 0; a < 16; a++)
				for (var b = 0; b < 16; b++)
					for (var step = 0; step <= 32; step++)
					{
						var t = step / 32.0;
						if (((a >> 1) & 1) == (b & 1) && ((a >> 2) & 1) == ((b >> 3) & 1) &&
							Math.Abs(Field(a, 1, t) - Field(b, 0, t)) > 0.000001)
							throw new InvalidDataException("Shore u-edge mismatch.");
						if (((a >> 3) & 1) == (b & 1) && ((a >> 2) & 1) == ((b >> 1) & 1) &&
							Math.Abs(Field(a, t, 1) - Field(b, t, 0)) > 0.000001)
							throw new InvalidDataException("Shore v-edge mismatch.");
					}
			for (var mask = 0; mask < 16; mask++)
				for (var x = 0; x <= 16; x++)
					for (var y = 0; y <= 16; y++)
					{
						var value = Field(mask, x / 16.0, y / 16.0);
						var rotated = ((mask << 1) | (mask >> 3)) & 15;
						if (value < 0 || value > 1 || Math.Abs(value - Field(rotated, 1 - y / 16.0, x / 16.0)) > 0.000001)
							throw new InvalidDataException("Shore mask range or rotation mismatch.");
					}
			if (Field(5, 0.5, 0.5) != 0 || Field(10, 0.5, 0.5) != 0)
				throw new InvalidDataException("Shore saddle connects land.");
		}

		static ISpriteFrame Frame(ModData modData, string name)
		{
			var path = "ca|bits/terrain/rubberduck/" + name + ".png";
			using var stream = modData.DefaultFileSystem.Open(path);
			if (!new PngSheetLoader().TryParseSprite(stream, path, out var frames, out _) || frames.Length == 0 ||
				frames[0].Type != SpriteFrameType.Rgba32 || frames[0].Size.Width != 128 || frames[0].Size.Height != 64)
				throw new InvalidDataException("Expected a complete 128x64 RGBA ground frame: " + path);
			return frames[0];
		}

		static Png Compose(ModData modData)
		{
			var sand = Frame(modData, "sand_base").Data;
			var water = Frame(modData, "water_marker_v01").Data;
			var grass = Frame(modData, "grass_a_base").Data;
			const int width = 128 * 32;
			var pixels = new byte[width * 64 * 4];
			for (var frame = 0; frame < 32; frame++)
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 128; x++)
					{
						var u = (x + 0.5) / 128 + (y + 0.5) / 64 - 0.5;
						var v = (y + 0.5) / 64 - (x + 0.5) / 128 + 0.5;
						var level = Field(frame % 16, Math.Clamp(u, 0, 1), Math.Clamp(v, 0, 1));
						var blend = Math.Clamp((level - 0.40) / 0.20, 0, 1);
						blend = blend * blend * (3 - 2 * blend);
						// A narrow damp-sand band, not invented foam or stretched water frames.
						var wet = frame >= 16 ? 1 : 1 - 0.18 * Math.Max(0, 1 - Math.Abs(level - 0.58) / 0.16);
						var background = frame >= 16 ? sand : water;
						var foreground = frame >= 16 ? grass : sand;
						var source = (y * 128 + x) * 4;
						var dest = (y * width + frame * 128 + x) * 4;
						for (var c = 0; c < 3; c++)
							pixels[dest + c] = (byte)(background[source + c] * (1 - blend) + foreground[source + c] * wet * blend);
						pixels[dest + 3] = (byte)Math.Max(background[source + 3], foreground[source + 3]);
					}
			var png = new Png(pixels, SpriteFrameType.Rgba32, width, 64);
			png.EmbeddedData.Add("FrameSize", "128,64");
			png.EmbeddedData.Add("FrameAmount", "32");
			return png;
		}

		static int LandComponents(Map map)
		{
			var remaining = new HashSet<CPos>();
			foreach (var cell in map.AllCells)
				if (map.Tiles[cell].Type == 1000 || map.Tiles[cell].Type == 1020) remaining.Add(cell);
			var components = 0;
			var pending = new Queue<CPos>();
			while (remaining.Count != 0)
			{
				var first = remaining.First();
				remaining.Remove(first);
				pending.Enqueue(first);
				components++;
				while (pending.Count != 0)
				{
					var cell = pending.Dequeue();
					for (var y = -1; y <= 1; y++)
						for (var x = -1; x <= 1; x++)
						{
							var next = cell + new CVec(x, y);
							if (remaining.Remove(next)) pending.Enqueue(next);
						}
				}
			}
			return components;
		}

		static void ExportMap(ModData modData, string output, string shape, byte[] image)
		{
			using var map = new Map(modData, (DefaultTerrain)modData.DefaultTerrainInfo["RUBBERDUCK-TEMPERATE"], 66, 82);
			map.Title = "Rubberduck Shore Calibration " + shape;
			map.Author = "YMCA terrain calibration";
			map.RequiresMod = "ca";
			map.SetBounds(new PPos(1, 17), new PPos(64, 80));
			var center = new MPos(32, 48).ToCPos(map);
			var players = new MapPlayers(map.Rules, 2);
			for (var i = 0; i < 2; i++)
			{
				players.Players[$"Multi{i}"].Spawn = i + 1;
				players.Players[$"Multi{i}"].LockSpawn = true;
			}
			map.PlayerDefinitions = players.ToMiniYaml();
			var rules = new List<MiniYamlNode>
			{
				new MiniYamlNode("World", new MiniYaml("", new[]
				{
					new MiniYamlNode("TerrainCalibrationView", new MiniYaml("", new[]
					{
						new MiniYamlNode("Center", (center + new CVec(2, -2)).ToString()),
					})),
				})),
				new MiniYamlNode("Player", new MiniYaml("", new[]
				{
					new MiniYamlNode("Shroud", new MiniYaml("", new[]
					{
						new MiniYamlNode("FogCheckboxEnabled", "false"), new MiniYamlNode("FogCheckboxLocked", "true"),
						new MiniYamlNode("ExploredMapCheckboxEnabled", "true"), new MiniYamlNode("ExploredMapCheckboxLocked", "true"),
					})),
				})),
				new MiniYamlNode("^CalibrationCoast", new MiniYaml("", new[]
				{
					new MiniYamlNode("AlwaysVisible", ""),
					new MiniYamlNode("Immobile", new MiniYaml("", new[] { new MiniYamlNode("OccupiesSpace", "false") })),
					new MiniYamlNode("RenderSprites", ""), new MiniYamlNode("WithSpriteBody", ""),
					new MiniYamlNode("BodyOrientation", new MiniYaml("", new[] { new MiniYamlNode("QuantizedFacings", "1") })),
				})),
			};
			var sequences = new List<MiniYamlNode>();
			for (var mask = 1; mask < 31; mask++)
			{
				var name = "calibration.coast" + mask;
				rules.Add(new MiniYamlNode(name, new MiniYaml("", new[] { new MiniYamlNode("Inherits", "^CalibrationCoast") })));
				sequences.Add(new MiniYamlNode(name, new MiniYaml("", new[]
				{
					new MiniYamlNode("idle", new MiniYaml("", new[]
					{
						new MiniYamlNode("Filename", "calibration-coast.png"), new MiniYamlNode("Start", mask.ToString()),
						new MiniYamlNode("Length", "1"), new MiniYamlNode("ZOffset", "-16384"),
					})),
				})));
			}
			map.RuleDefinitions = new MiniYaml("", rules);
			map.SequenceDefinitions = new MiniYaml("", sequences);
			bool Land(int x, int y)
			{
				var inside = Math.Abs(x) <= 4 && Math.Abs(y) <= 4;
				return shape switch
				{
					"irregular" => x * x + y * y < 24 + 5 * Math.Sin(x * 1.3) + 3 * Math.Cos(y * 1.7),
					"bay" => inside && !(x > 0 && y > 0),
					"lake" => !inside,
					"saddle" => inside && ((x <= 0 && y <= 0) || (x >= 1 && y >= 1)),
					"saddle-east" => inside && ((x >= 1 && y <= 0) || (x <= 0 && y >= 1)),
					_ => inside,
				};
			}
			bool Grass(int x, int y)
			{
				for (var dy = -2; dy <= 2; dy++)
					for (var dx = -2; dx <= 2; dx++)
						if (!Land(x + dx, y + dy)) return false;
				return true;
			}
			int Mask(Func<int, int, bool> field, int x, int y) => (field(x, y) ? 1 : 0) | (field(x + 1, y) ? 2 : 0) |
				(field(x + 1, y + 1) ? 4 : 0) | (field(x, y + 1) ? 8 : 0);
			var actors = new List<MiniYamlNode>();
			var masks = new int[32];
			foreach (var cell in map.AllCells)
			{
				var x = cell.X - center.X;
				var y = cell.Y - center.Y;
				var mask = Mask(Land, x, y);
				// Conservative collision: only complete land cells are walkable.
				var terrain = mask == 15 ? (ushort)1020 : (ushort)1050;
				if (mask == 15)
				{
					mask = 16 + Mask(Grass, x, y);
					if (mask == 31) terrain = 1000;
				}
				masks[mask]++;
				map.Tiles[cell] = new TerrainTile(terrain, 0);
				map.Height[cell] = 0;
				if (mask == 0 || mask == 16 || mask == 31) continue;
				var actor = new ActorReference("calibration.coast" + mask) { new LocationInit(cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Coast" + actors.Count, actor.Save()));
			}
			foreach (var delta in new[] { new CVec(-3, -3), new CVec(2, -3) })
			{
				var cell = shape == "lake" ? center + delta + new CVec(-5, -5) :
					shape == "saddle" && delta.X > 0 ? center + new CVec(2, 2) :
					shape == "saddle-east" ? center + (delta.X > 0 ? new CVec(-3, 2) : new CVec(2, -3)) : center + delta;
				if (map.Tiles[cell].Type != 1020 && map.Tiles[cell].Type != 1000)
					throw new InvalidDataException("Shore fixture spawn is not on full land.");
				var spawn = new ActorReference("mpspawn") { new LocationInit(cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("Spawn" + actors.Count, spawn.Save()));
			}
			if (shape == "irregular")
			{
				foreach (var probe in new[]
				{
					(Type: "Light_Infantry", Start: new CVec(-3, 0), End: new CVec(3, 0), Water: false),
					(Type: "Challenger_Tank", Start: new CVec(-2, -2), End: new CVec(0, 3), Water: false),
					(Type: "DD", Start: new CVec(-8, 0), End: new CVec(8, 0), Water: true),
				})
				{
					var name = "calibration.probe." + probe.Type.ToLowerInvariant();
					var sourceImage = map.Rules.Actors[probe.Type.ToLowerInvariant()].TraitInfo<RenderSpritesInfo>().Image ?? probe.Type.ToLowerInvariant();
					rules.Add(new MiniYamlNode(name, new MiniYaml("", new[]
					{
						new MiniYamlNode("Inherits", probe.Type),
						new MiniYamlNode("RenderSprites", new MiniYaml("", new[] { new MiniYamlNode("Image", sourceImage) })),
						new MiniYamlNode("ShoreMovementProbe", new MiniYaml("", new[]
						{
							new MiniYamlNode("Target", (center + probe.End).ToString()),
							new MiniYamlNode("Forbidden", (center + (probe.Water ? new CVec(0, 0) : new CVec(8, 0))).ToString()),
							new MiniYamlNode("Water", probe.Water.ToString()),
						})),
					})));
					var actor = new ActorReference(name) { new LocationInit(center + probe.Start), new OwnerInit("Multi0") };
					actors.Add(new MiniYamlNode("Probe" + actors.Count, actor.Save()));
				}
				map.RuleDefinitions = new MiniYaml("", rules);
			}
			map.ActorDefinitions = actors;
			var components = LandComponents(map);
			if (components != (shape.StartsWith("saddle", StringComparison.Ordinal) ? 2 : 1))
				throw new InvalidDataException("Unexpected shore land connectivity: " + shape);
			var path = Path.Combine(output, "rubberduck-calibration-shore-" + shape + ".oramap");
			if (File.Exists(path)) File.Delete(path);
			using (var package = ZipFileLoader.Create(path))
			{
				package.Update("calibration-coast.png", image);
				map.Save(package);
			}
			using var stream = File.OpenRead(path);
			using var savedPackage = new ZipFileLoader.ReadOnlyZipFile(stream, path);
			using var savedMap = new Map(modData, savedPackage);
			if (savedMap.InvalidCustomRules)
				throw new InvalidDataException("Invalid shore fixture rules.", savedMap.InvalidCustomRulesException);
			foreach (var cell in map.AllCells)
				if (savedMap.Tiles[cell].Type != map.Tiles[cell].Type || savedMap.Height[cell] != 0)
					throw new InvalidDataException("Shore terrain changed during roundtrip.");
			File.WriteAllLines(Path.Combine(output, "shore-" + shape + "-masks.tsv"),
				new[] { "mask\tcells" }.Concat(masks.Select((count, mask) => mask + "\t" + count)));
			File.WriteAllText(Path.Combine(output, "shore-" + shape + "-validation.txt"),
				$"Mask edges, rotation and range: passed\n8-neighbor full-land components: {components}\nTerrain/height roundtrip: passed\nRuntime locomotor tests: pending\n");
		}
	}
}
