using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.MapGeneration
{
	// A bounded shared face vocabulary: no generated-map custom rules or per-cell sheets.
	static class RubberduckPlateauRenderer
	{
		static readonly double[] X = { 64, 128, 64, 0 };
		static readonly double[] Y = { 32, 64, 96, 64 };

		public static byte[] Face(int direction, int dropA, int dropB, byte[] rock)
		{
			if (dropA < 0 || dropA > 4 || dropB < 0 || dropB > 4) throw new ArgumentOutOfRangeException(nameof(dropA));
			var pixels = new byte[128 * 320 * 4];
			var edge = PlateauTopology.Edge(direction);
			for (var x = 0; x < 128; x++)
			{
				var t = (x + 0.5 - X[edge.A]) / (X[edge.B] - X[edge.A]);
				if (t < 0 || t > 1) continue;
				var top = Y[edge.A] + t * (Y[edge.B] - Y[edge.A]);
				var bottom = top + Math.Min(direction >= 2 ? 2 : 128, 32 * (dropA + t * (dropB - dropA)));
				for (var y = (int)Math.Ceiling(top - 0.5); y < Math.Ceiling(bottom - 0.5); y++)
				{
					var source = ((64 + y % 80) * 128 + 40 + x % 24) * 4;
					var dest = (y * 128 + x) * 4;
					for (var c = 0; c < 3; c++) pixels[dest + c] = rock[source + 3] == 0 ? (byte)65 : rock[source + c];
					pixels[dest + 3] = 255;
				}
			}
			return pixels;
		}

		public static byte[] ComposeWalls(CPos cell, PlateauSurface surface, IReadOnlyDictionary<CPos, PlateauSurface> cells, byte[] rock)
		{
			var pixels = new byte[128 * 320 * 4];
			for (var direction = 0; direction < 4; direction++)
			{
				var neighbor = PlateauTopology.Get(cells, cell + PlateauTopology.Directions[direction]);
				var edge = PlateauTopology.Edge(direction);
				var a = surface.CornerHeight(edge.A) - neighbor.CornerHeight(edge.NeighborA);
				var b = surface.CornerHeight(edge.B) - neighbor.CornerHeight(edge.NeighborB);
				if (a < 0 || b < 0 || a + b == 0) continue;
				if (surface.Ramp != 0) throw new InvalidDataException("Ramp needs a higher retaining strip, not an unsupported face.");
				var face = Face(direction, a, b, rock);
				for (var i = 0; i < face.Length; i += 4)
					if (face[i + 3] != 0) Array.Copy(face, i, pixels, i, 4);
			}
			return pixels;
		}

		public static void ExportSprites(string output, byte[] rock)
		{
			const int width = 1280;
			var pixels = new byte[width * 3200 * 4];
			var rules = new StringBuilder();
			var sequences = new StringBuilder();
			for (var direction = 0; direction < 4; direction++)
				for (var a = 0; a <= 4; a++)
					for (var b = 0; b <= 4; b++)
					{
						var index = direction * 25 + a * 5 + b;
						var face = Face(direction, a, b, rock);
						for (var row = 0; row < 320; row++)
							Array.Copy(face, row * 128 * 4, pixels, ((index / 10 * 320 + row) * width + index % 10 * 128) * 4, 128 * 4);
						rules.AppendLine($"terrain.rubberduck.plateauwall{index}:\n\tInherits: ^RubberduckTerrainDecoration\n\t-WithSpriteBody:\n\tPlateauFaceBody:\n\t\tDirection: {direction}\n\t\tDropA: {a}\n\t\tDropB: {b}\n");
						sequences.AppendLine($"terrain.rubberduck.plateauwall{index}:\n\tidle:\n\t\tFilename: ca|bits/terrain/rubberduck/derived/plateau-faces.png\n\t\tStart: {index}\n\t\tLength: 1\n");
					}
			var png = new Png(pixels, SpriteFrameType.Rgba32, width, 3200);
			png.EmbeddedData.Add("FrameSize", "128,320");
			png.EmbeddedData.Add("FrameAmount", "100");
			png.EmbeddedData.Add("Offset", "0,96");
			png.Save(Path.Combine(output, "plateau-faces.png"));
			File.WriteAllText(Path.Combine(output, "plateau-wall-rules.yaml"), rules.ToString());
			File.WriteAllText(Path.Combine(output, "plateau-wall-sequences.yaml"), sequences.ToString());
		}

		public static void Apply(Map map, IReadOnlyDictionary<CPos, PlateauSurface> surfaces, List<MiniYamlNode> actors,
			Func<CPos, int, bool> renderFace = null)
		{
			foreach (var pair in surfaces)
			{
				map.Height[pair.Key] = pair.Value.Height;
				map.Tiles[pair.Key] = new TerrainTile(pair.Value.Blocked ? (ushort)3992 :
					pair.Value.Ramp == 0 ? (ushort)1000 : (ushort)(14000 + pair.Value.Ramp), 0);
			}
			AddActors(map, surfaces, actors, renderFace);
		}

		// One composition entry point for normal exports and editor shape transactions.
		// Keep face suppression and native actor emission together: diverging implementations
		// can otherwise leave holes or emit both a native face and a fallback wall.
		public static void ComposeNative(Map map, IReadOnlyDictionary<CPos, PlateauSurface> surfaces,
			List<MiniYamlNode> actors, ISet<(CPos Cell, int Direction)> nativeFaces,
			IEnumerable<(CPos Cell, int Slot)> nativePieces, string actorPrefix, bool applyTerrain)
		{
			var closed = new RubberduckClosedCliffSelection(map, surfaces);
			bool RenderFallback(CPos c, int d) => !nativeFaces.Contains((c, d)) &&
				!(closed.Cells.Contains(c) && !closed.Cells.Contains(c + PlateauTopology.Directions[d]));
			if (applyTerrain) Apply(map, surfaces, actors, RenderFallback);
			else AddActors(map, surfaces, actors, RenderFallback);
			foreach (var piece in nativePieces)
			{
				if (closed.Cells.Contains(piece.Cell)) continue;
				var actor = new ActorReference("terrain.rubberduck.nativecliff" + piece.Slot)
				{ new LocationInit(piece.Cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode(actorPrefix + actors.Count, actor.Save()));
			}
			foreach (var piece in closed.Pieces)
			{
				var actor = new ActorReference("terrain.rubberduck.closedcliff" + piece.Slot)
				{ new LocationInit(piece.Cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode(actorPrefix + actors.Count, actor.Save()));
			}
		}

		// Pure actor planning is also used by atomic editor actions before mutating the map.
		public static void AddActors(Map map, IReadOnlyDictionary<CPos, PlateauSurface> surfaces, List<MiniYamlNode> actors,
			Func<CPos, int, bool> renderFace = null)
		{
			void Add(string type, CPos cell)
			{
				var actor = new ActorReference(type) { new LocationInit(cell), new OwnerInit("Neutral") };
				actors.Add(new MiniYamlNode("PlateauTerrain" + actors.Count, actor.Save()));
			}
			foreach (var pair in surfaces)
			{
				// Clear tops are real terrain, so resources and buildings must render above
				// the terrain layer instead of underneath a duplicate grass actor.
				if (pair.Value.Blocked && pair.Value.Height > 0) Add("terrain.rubberduck.cliff17", pair.Key);
				for (var direction = 0; direction < 4; direction++)
				{
					var other = pair.Key + PlateauTopology.Directions[direction];
					var neighbor = surfaces.TryGetValue(other, out var value) ? value : new PlateauSurface(map.Height[other], map.Ramp[other]);
					var edge = PlateauTopology.Edge(direction);
					var a = pair.Value.CornerHeight(edge.A) - neighbor.CornerHeight(edge.NeighborA);
					var b = pair.Value.CornerHeight(edge.B) - neighbor.CornerHeight(edge.NeighborB);
					if (a < 0 || b < 0 || a + b == 0) continue;
					if (pair.Value.Ramp != 0 || a > 4 || b > 4) throw new InvalidDataException("Unsupported planned plateau boundary.");
					if (renderFace == null || renderFace(pair.Key, direction))
						Add("terrain.rubberduck.plateauwall" + (direction * 25 + a * 5 + b), pair.Key);
				}
			}
		}
	}
}
