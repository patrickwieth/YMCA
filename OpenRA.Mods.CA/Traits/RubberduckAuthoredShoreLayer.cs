using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// Shared source-backed shoreline layer for runtime and editor.
	// Never changes terrain IDs, collision, heights, resources or actor placement.
	sealed class RubberduckAuthoredShoreLayer : IDisposable
	{
		readonly Map map;
		readonly SheetBuilder sheet = new SheetBuilder(SheetType.BGRA, 2048);
		readonly RubberduckMaterialLayer land;
		readonly Dictionary<int, Sprite> sprites = new();
		readonly byte[][][] materials = new byte[5][][];
		readonly Dictionary<CPos, int> cells = new();
		readonly Dictionary<int, bool[]> groundExclusions = new();
		public bool[] GroundExclusion(CPos cell) => cells.TryGetValue(cell, out var key) ? groundExclusions[key] : null;
		public int Count => cells.Count;
		public string Snapshot() => string.Join(";", cells.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y).Select(p => $"{p.Key}:{p.Value}"));

		public RubberduckAuthoredShoreLayer(World world, WorldRenderer renderer)
		{
			map = world.Map;
			using var stream = map.Open("ca|bits/terrain/rubberduck/derived/shores/ground-edge-sources.png");
			var png = new Png(stream);
			if (png.Width != 1024 || png.Height != 3840 || png.Type != SpriteFrameType.Rgba32)
				throw new InvalidDataException("Expected the ten-block authored ground-edge atlas.");
			var shaded = map.Rules.TerrainInfo.Id.EndsWith("-SHADED", StringComparison.Ordinal) ? 1 : 0;
			for (var m = 0; m < 5; m++)
			{
				materials[m] = new byte[48][];
				for (var f = 0; f < 48; f++)
				{
					materials[m][f] = new byte[128 * 64 * 4];
					var index = (m * 2 + shaded) * 48 + f;
					for (var y = 0; y < 64; y++) Array.Copy(png.Data, ((index / 8 * 64 + y) * 1024 + index % 8 * 128) * 4, materials[m][f], y * 128 * 4, 128 * 4);
				}
			}
			var empty = sheet.Add(new byte[128 * 64 * 4], SpriteFrameType.Rgba32, new Size(128, 64));
			land = new RubberduckMaterialLayer(world, renderer, empty, BlendMode.Alpha, world.Type != WorldType.Editor);
		}

		static int Material(ushort tile) => tile == 1000 ? 0 : tile == 1010 ? 1 : tile == 1020 ? 2 : tile == 1030 ? 3 : tile == 1040 ? 4 : -1;
		int Water(CPos c)
		{
			if (!map.Contains(c) || map.Height[c] != 0 || map.Ramp[c] != 0) return 0;
			var type = map.Tiles[c].Type;
			return type == 1050 || type == 1070 ? 1 : type == 1060 || type == 1080 ? 2 : 0;
		}

		// Returns the water family beneath the source-authored land silhouette.
		public int Update(CPos cell)
		{
			var material = map.Contains(cell) ? Material(map.Tiles[cell].Type) : -1;
			var edges = 0; var diagonals = 0; var family = 0;
			if (material >= 0 && map.Height[cell] == 0 && map.Ramp[cell] == 0)
				for (var d = 0; d < 4; d++)
				{
					var edge = Water(cell + PlateauTopology.Directions[d]);
					var diagonal = Water(cell + PlateauTopology.Directions[d] + PlateauTopology.Directions[(d + 1) % 4]);
					if (edge != 0) { edges |= 1 << d; if (family == 0) family = edge; }
					if (diagonal != 0) { diagonals |= 1 << d; if (family == 0) family = diagonal; }
				}
			var topology = RubberduckShoreTopology.Normalize(edges, diagonals);
			if (topology == 0)
			{
				cells.Remove(cell);
				land.Update(cell, null, null);
				return 0;
			}
			var variant = (int)(Math.Abs((long)cell.X + cell.Y) % 4);
			var key = (material * 4 + variant) * 256 + topology;
			cells[cell] = key;
			land.Update(cell, SpriteFor(key), null);
			return family;
		}

		Sprite SpriteFor(int key)
		{
			if (!sprites.TryGetValue(key, out var sprite))
			{
				var pixels = RubberduckShoreTopology.Compose(materials[key / 1024], key % 256, key / 256 % 4);
				sprite = sheet.Add(pixels, SpriteFrameType.Rgba32, new Size(128, 64), 1, float3.Zero);
				sprites.Add(key, sprite);
				// Ordinary material blends must not repaint water revealed by the native
				// fringe. Preserve nonzero antialias coverage at adjoining land edges,
				// but never let a ground blend fill fully transparent source pixels.
				var exclusion = new bool[128 * 64];
				for (var p = 0; p < exclusion.Length; p++) exclusion[p] = pixels[p * 4 + 3] == 0;
				groundExclusions.Add(key, exclusion);
				if (sprites.Count > 920) throw new InvalidDataException("Authored shore cache exceeded 46 topologies x four variants x five materials.");
			}
			return sprite;
		}

		public string ValidateCache()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			for (var materialVariant = 0; materialVariant < 20; materialVariant++)
				for (var edges = 0; edges < 16; edges++)
					for (var diagonals = 0; diagonals < 16; diagonals++)
					{
						var topology = RubberduckShoreTopology.Normalize(edges, diagonals);
						if (topology != 0) SpriteFor(materialVariant * 256 + topology);
					}
			var sheets = sheet.AllSheets.Count();
			if (sprites.Count != 920 || groundExclusions.Count != 920 || sheets > 2)
				throw new InvalidDataException("Authored-shore atlas exceeded its complete-topology bound.");
			return $"SHORE CACHE PASS {sprites.Count} sprites, {sheets} sheets, {groundExclusions.Count * 8192} mask bytes; {watch.ElapsedMilliseconds} ms CPU precache.";
		}

		public void Draw(Viewport viewport) => land.Draw(viewport);
		public void Dispose() { land.Dispose(); sheet.Dispose(); sprites.Clear(); groundExclusions.Clear(); cells.Clear(); }
	}
}
