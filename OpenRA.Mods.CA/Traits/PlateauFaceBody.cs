using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Clips static plateau walls against nearer solid terrain without requiring a global depth buffer.")]
	public class PlateauFaceBodyInfo : TraitInfo, IRenderActorPreviewInfo
	{
		public readonly int Direction = 0;
		public readonly int DropA = 0;
		public readonly int DropB = 0;
		public override object Create(ActorInitializer init) => new PlateauFaceBody(this);
		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var body = new PlateauFaceBody(this);
			yield return new CliffEditorPreview((map, cell) => body.BuildSlices(map, cell, init.Actor.Name));
		}
	}

	public class PlateauFaceBody : IRender
	{
		static readonly double[] CornerX = { 0, 64, 0, -64 };
		static readonly double[] CornerY = { -32, 0, 32, 0 };
		readonly PlateauFaceBodyInfo info;
		Sprite[] slices;
		IRenderable[] renderables;
		WPos renderPosition;
		long revision = -1;

		public PlateauFaceBody(PlateauFaceBodyInfo info) { this.info = info; }

		internal Sprite[] Prepare(Actor self)
		{
			var current = TerrainRenderRevision.Get(self.World.Map);
			if (slices == null || revision != current)
			{
				slices = BuildSlices(self.World.Map, self.Location, self.Info.Name);
				revision = current;
				renderables = null;
			}
			if (renderables == null || renderPosition != self.CenterPosition)
			{
				renderPosition = self.CenterPosition;
				renderables = slices.Length == 0 ? Array.Empty<IRenderable>() : new IRenderable[] { new CliffSliceRenderable(slices, renderPosition) };
			}
			return slices;
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			Prepare(self);
			return renderables;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			var center = wr.ScreenPxPosition(self.CenterPosition);
			yield return new Rectangle(center.X - 64, center.Y - 64, 128, 320);
		}

		internal Sprite[] BuildSlices(Map map, CPos location, string image) =>
			TerrainBuildMetrics.Measure(map, "plateau-face", () => BuildSlicesCore(map, location, image));

		Sprite[] BuildSlicesCore(Map map, CPos location, string image)
		{
			var sequence = map.Sequences.GetSequence(image, "idle");
			var variation = unchecked((uint)(location.X * 73856093 ^ location.Y * 19349663)) % (uint)sequence.Length;
			var sprite = sequence.GetSprite((int)variation);
			var height = map.Height[location];
			var roofs = new List<(double X, double Y, double Depth)[]>();
			for (var dx = -1; dx <= 5; dx++)
				for (var dy = -1; dy <= 5; dy++)
				{
					if (dx == 0 && dy == 0 || Math.Abs(dx - dy) > 2) continue;
					var cell = location + new CVec(dx, dy);
					if (!map.Contains(cell) || map.Ramp[cell] > 4) continue;
					var surface = new PlateauSurface(map.Height[cell], map.Ramp[cell]);
					var roof = new (double X, double Y, double Depth)[4];
					for (var corner = 0; corner < 4; corner++)
					{
						var depth = 32 * (dx + dy) + CornerY[corner];
						roof[corner] = (64 * (dx - dy) + CornerX[corner],
							depth - 32 * (surface.CornerHeight(corner) - height), depth);
					}
					roofs.Add(roof);
				}

			var limits = new int[128];
			var edge = PlateauTopology.Edge(info.Direction);
			for (var column = 0; column < limits.Length; column++)
			{
				limits[column] = 320;
				var x = column + 0.5 - 64;
				var t = (x - CornerX[edge.A]) / (CornerX[edge.B] - CornerX[edge.A]);
				if (t < 0 || t > 1) continue;
				var top = CornerY[edge.A] + t * (CornerY[edge.B] - CornerY[edge.A]);
				var bottom = top + Math.Min(info.Direction >= 2 ? 2 : 128, 32 * (info.DropA + t * (info.DropB - info.DropA)));
				var limit = bottom;
				foreach (var roof in roofs)
				{
					var nearY = double.PositiveInfinity;
					var farY = double.NegativeInfinity;
					var nearDepth = 0.0;
					var farDepth = 0.0;
					for (var i = 0; i < 4; i++)
					{
						var a = roof[i]; var b = roof[(i + 1) % 4];
						if (a.X == b.X || x < Math.Min(a.X, b.X) || x > Math.Max(a.X, b.X)) continue;
						var fraction = (x - a.X) / (b.X - a.X);
						var y = a.Y + fraction * (b.Y - a.Y);
						var depth = a.Depth + fraction * (b.Depth - a.Depth);
						if (y < nearY) { nearY = y; nearDepth = depth; }
						if (y > farY) { farY = y; farDepth = depth; }
					}
					if (farDepth <= top || double.IsInfinity(nearY)) continue;
					if (nearDepth < top)
						nearY += (top - nearDepth) / (farDepth - nearDepth) * (farY - nearY);
					// Terrain is a solid column down to level zero. Once nearer ground
					// covers the wall, its volume also hides the remainder below that roof.
					limit = Math.Min(limit, nearY);
				}
				if (limit < bottom - 0.001)
					limits[column] = limit <= top ? 0 : Math.Clamp((int)Math.Ceiling(limit + 64 - 0.5), 0, 320);
			}

			var result = new List<Sprite>();
			for (var first = 0; first < limits.Length;)
			{
				var end = first + 1;
				while (end < limits.Length && limits[end] == limits[first]) end++;
				var h = limits[first];
				if (h > 0)
					result.Add(new Sprite(sprite.Sheet, new Rectangle(sprite.Bounds.Left + first, sprite.Bounds.Top, end - first, h),
						0, new float3(first + (end - first) / 2f - 64, h / 2f - 64, 0), sprite.Channel, sprite.BlendMode));
				first = end;
			}
			return result.ToArray();
		}
	}
}
