using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	// Clip overlay geometry against nearer terrain, retaining original atlas pixels.
	sealed class RubberduckMaterialLayer : IDisposable
	{
		readonly Map map;
		readonly WorldRenderer renderer;
		readonly TerrainSpriteLayer ordinary;
		readonly bool restrictToBounds;
		readonly Func<CPos, bool[]> additionalExclusion;
		readonly Dictionary<CPos, Sprite> sources = new();
		readonly Dictionary<CPos, Sprite[]> clipped = new();
		long revision;

		public RubberduckMaterialLayer(World world, WorldRenderer renderer, Sprite empty, BlendMode blend, bool restrictToBounds, Func<CPos, bool[]> additionalExclusion = null)
		{
			map = world.Map;
			this.renderer = renderer;
			this.restrictToBounds = restrictToBounds;
			this.additionalExclusion = additionalExclusion;
			ordinary = new TerrainSpriteLayer(world, renderer, empty, blend, restrictToBounds);
			revision = TerrainRenderRevision.Get(map);
		}

		public void Update(CPos cell, Sprite sprite, PaletteReference palette)
		{
			if (palette != null) throw new ArgumentException("Rubberduck material layers require RGBA sprites.", nameof(palette));
			if (sprite == null)
			{
				sources.Remove(cell); clipped.Remove(cell);
				ordinary.Update(cell, (Sprite)null, null);
				return;
			}
			sources[cell] = sprite;
			Apply(cell, sprite);
		}

		void Apply(CPos cell, Sprite sprite)
		{
			var rectangles = VisibleRectangles(map, cell, additionalExclusion?.Invoke(cell));
			if (rectangles == null)
			{
				clipped.Remove(cell);
				ordinary.Update(cell, sprite, null);
				return;
			}
			ordinary.Update(cell, (Sprite)null, null);
			var slices = new Sprite[rectangles.Length];
			for (var i = 0; i < slices.Length; i++)
			{
				var r = rectangles[i];
				var y = r.Y + r.Height / 2f - 32;
				slices[i] = new Sprite(sprite.Sheet,
					new Rectangle(sprite.Bounds.X + r.X, sprite.Bounds.Y + r.Y, r.Width, r.Height),
					sprite.ZRamp, sprite.Offset + new float3(r.X + r.Width / 2f - 64, y, y * sprite.ZRamp), sprite.Channel, sprite.BlendMode);
			}
			clipped[cell] = slices;
		}

		internal static Rectangle[] VisibleRectangles(Map map, CPos cell, bool[] additionalExclusion = null)
		{
			var roofs = new List<(double X, double Y, double Depth)[]>();
			var reach = map.Grid.MaximumTerrainHeight + 2;
			var cornerX = new[] { 0, 64, 0, -64 };
			var cornerY = new[] { -32, 0, 32, 0 };
			for (var dx = -1; dx <= reach; dx++)
				for (var dy = Math.Max(-1, dx - 1); dy <= Math.Min(reach, dx + 1); dy++)
				{
					if (dx + dy <= 0) continue;
					var other = cell + new CVec(dx, dy);
					if (!map.Contains(other) || map.Ramp[other] > 4 ||
						map.Height[other] + (map.Ramp[other] == 0 ? 0 : 1) <= map.Height[cell]) continue;
					var surface = new PlateauSurface(map.Height[other], map.Ramp[other]);
					var corners = new (double X, double Y, double Depth)[4];
					var minY = double.PositiveInfinity; var maxY = double.NegativeInfinity;
					for (var c = 0; c < 4; c++)
					{
						var depth = 32 * (dx + dy) + cornerY[c];
						var y = depth - 32 * (surface.CornerHeight(c) - map.Height[cell]);
						corners[c] = (64 * (dx - dy) + cornerX[c], y, depth);
						minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
					}
					if (minY < 32 && maxY > -32) roofs.Add(corners);
				}
			if (roofs.Count == 0 && additionalExclusion == null) return null;

			if (additionalExclusion != null && additionalExclusion.Length != 128 * 64)
				throw new ArgumentException("Expected a 128x64 exclusion mask.", nameof(additionalExclusion));
			var hidden = additionalExclusion == null ? new bool[128 * 64] : (bool[])additionalExclusion.Clone();
			// A source-alpha exclusion already includes its antialiased diamond fringe.
			// Do not crop it to the mathematical diamond unless height clipping needs it.
			if (roofs.Count != 0)
				for (var y = 0; y < 64; y++)
					for (var x = 0; x < 128; x++)
						hidden[y * 128 + x] |= Math.Abs(x + 0.5 - 64) / 64 + Math.Abs(y + 0.5 - 32) / 32 > 1;
			var anyHidden = additionalExclusion != null;
			for (var x = 0; x < 128; x++)
			{
				var px = x + 0.5 - 64;
				foreach (var roof in roofs)
				{
					var near = double.PositiveInfinity; var far = double.NegativeInfinity;
					var nearDepth = 0.0; var farDepth = 0.0;
					for (var c = 0; c < 4; c++)
					{
						var a = roof[c]; var b = roof[(c + 1) % 4];
						if (a.X == b.X || px < Math.Min(a.X, b.X) || px > Math.Max(a.X, b.X)) continue;
						var t = (px - a.X) / (b.X - a.X);
						var y = a.Y + t * (b.Y - a.Y); var depth = a.Depth + t * (b.Depth - a.Depth);
						if (y < near) { near = y; nearDepth = depth; }
						if (y > far) { far = y; farDepth = depth; }
					}
					if (far <= near) continue;
					for (var y = 0; y < 64; y++)
					{
						var py = y + 0.5 - 32;
						if (Math.Abs(px) / 64 + Math.Abs(py) / 32 > 1 || py < near || py > far) continue;
						var depth = nearDepth + (py - near) / (far - near) * (farDepth - nearDepth);
						if (depth <= py + 0.001) continue;
						hidden[y * 128 + x] = true; anyHidden = true;
					}
				}
			}
			if (!anyHidden) return null;

			return ExclusionRectangles(hidden);
		}

		internal static Rectangle[] ExclusionRectangles(bool[] hidden, int width = 128, int height = 64)
		{
			if (width <= 0 || height <= 0 || hidden.Length != (long)width * height) throw new ArgumentException("Exclusion mask dimensions do not match.", nameof(hidden));
			// Merge equal horizontal runs vertically; no per-cell texture copies.
			var rectangles = new List<Rectangle>();
			var previous = new Dictionary<(int X, int Width), int>();
			for (var y = 0; y < height; y++)
			{
				var current = new Dictionary<(int X, int Width), int>();
				for (var x = 0; x < width;)
				{
					while (x < width && hidden[y * width + x]) x++;
					var start = x;
					while (x < width && !hidden[y * width + x]) x++;
					if (x == start) continue;
					var key = (start, x - start);
					if (previous.TryGetValue(key, out var index))
					{
						var r = rectangles[index];
						rectangles[index] = new Rectangle(r.X, r.Y, r.Width, r.Height + 1);
					}
					else { index = rectangles.Count; rectangles.Add(new Rectangle(start, y, x - start, 1)); }
					current[key] = index;
				}
				previous = current;
			}
			return rectangles.ToArray();
		}

		internal bool IsClipped(CPos cell)
		{
			Refresh();
			return clipped.ContainsKey(cell);
		}

		internal bool IsFullyClipped(CPos cell)
		{
			Refresh();
			return clipped.TryGetValue(cell, out var slices) && slices.Length == 0;
		}

		void Refresh()
		{
			var next = TerrainRenderRevision.Get(map);
			if (revision == next) return;
			foreach (var source in sources) Apply(source.Key, source.Value);
			revision = next;
		}

		public void Draw(Viewport viewport)
		{
			Refresh();
			ordinary.Draw(viewport);
			var visible = (restrictToBounds ? viewport.VisibleCellsInsideBounds : viewport.AllVisibleCells).CandidateMapCoords;
			foreach (var entry in clipped)
			{
				var row = entry.Key.ToMPos(map).V;
				if (row < visible.TopLeft.V || row > visible.BottomRight.V) continue;
				var position = map.CenterOfCell(entry.Key) - new WVec(0, 0, map.Grid.Ramps[map.Ramp[entry.Key]].CenterHeightOffset);
				var origin = renderer.Screen3DPosition(position);
				var tint = renderer.TerrainLighting?.TintAt(position) ?? float3.Ones;
				foreach (var sprite in entry.Value)
					Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, null, origin - 0.5f * sprite.Size, 1f, tint, 1f);
			}
			Game.Renderer.Flush();
		}

		public void Dispose()
		{
			ordinary.Dispose(); sources.Clear(); clipped.Clear();
		}
	}
}
