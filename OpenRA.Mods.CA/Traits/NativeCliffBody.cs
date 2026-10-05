using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.MapGeneration;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Clips fitted native cliff pieces against nearer terrain columns, shared by generated maps and editor previews.")]
	public class NativeCliffBodyInfo : TraitInfo, IRenderActorPreviewInfo
	{
		public readonly int Slot = 0;
		// Source-grid vocabulary for reserved components. Legacy actors retain their anchors.
		public readonly bool SourceGrid = false;
		// An authored rock-pile billboard rooted on an already blocked low cell.
		public readonly bool GroundAbutment = false;
		public NativeCliffBodyInfo() { }
		internal NativeCliffBodyInfo(bool groundAbutment) { GroundAbutment = groundAbutment; }
		public override object Create(ActorInitializer init) => new NativeCliffBody(this);
		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			var body = new NativeCliffBody(this);
			yield return new CliffEditorPreview((map, cell) => body.Build(map, cell, init.Actor.Name));
		}
	}

	public class NativeCliffBody : IRender
	{
		static readonly double[] CornerX = { 0, 64, 0, -64 };
		static readonly double[] CornerY = { -32, 0, 32, 0 };
		readonly NativeCliffBodyInfo info;
		Sprite[] slices;
		IRenderable[] renderables;
		WPos renderPosition;
		long revision = -1;
		public NativeCliffBody(NativeCliffBodyInfo info) { this.info = info; }
		internal Sprite[] Prepare(Actor self)
		{
			var current = TerrainRenderRevision.Get(self.World.Map);
			if (slices == null || revision != current)
			{
				slices = Build(self.World.Map, self.Location, self.Info.Name);
				revision = current;
				renderables = null;
			}
			if (renderables == null || renderPosition != self.CenterPosition)
			{
				renderPosition = self.CenterPosition;
				renderables = slices.Length == 0 ? Array.Empty<IRenderable>() : new IRenderable[] { new CliffSliceRenderable(slices, renderPosition, info.GroundAbutment ? 4 * 724 : 0) };
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
			var p = wr.ScreenPxPosition(self.CenterPosition);
			yield return info.GroundAbutment ? new Rectangle(p.X - 64, p.Y - 160, 128, 256) :
				info.SourceGrid ? new Rectangle(p.X - 192, p.Y - 196, 384, 452) : new Rectangle(p.X - 192, p.Y - 64, 384, 320);
		}
		internal Sprite[] Build(Map map, CPos location, string image) =>
			TerrainBuildMetrics.Measure(map, "native-cliff", () => BuildCore(map, location, image));

		Sprite[] BuildCore(Map map, CPos location, string image)
		{
			var sprite = map.Sequences.GetSequence(image, "idle").GetSprite(0);
			var offsetX = info.GroundAbutment ? -64 : info.SourceGrid ? -64 : info.Slot <= 2 ? -128 : info.Slot <= 7 ? 0 : info.Slot == 26 ? 64 : info.Slot == 30 ? -192 : -64;
			// Derive padding from the actual sprite, so older unpadded map-local
			// art still renders correctly. Padding must not move the rock or foot.
			var topPadding = info.Slot == 24 ? sprite.Bounds.Height - 256 : 0;
			if (topPadding != 0 && topPadding != 4)
				throw new InvalidOperationException("Native outer-corner sprite has unsupported roof padding.");
			var offsetY = (info.GroundAbutment ? -160 : info.SourceGrid ? -192 : info.Slot <= 7 ? -32 : info.Slot == 26 || info.Slot == 30 ? -64 : 0) - topPadding;
			var roofs = new List<(double X, double Y, double Depth)[]>();
			for (var dx = info.SourceGrid ? -6 : -3; dx <= 6; dx++)
				for (var dy = info.SourceGrid ? -6 : -3; dy <= 6; dy++)
				{
					var cell = location + new CVec(dx, dy);
					if (!map.Contains(cell) || map.Ramp[cell] > 4 || map.Height[cell] == 0 && map.Ramp[cell] == 0) continue;
					var surface = new PlateauSurface(map.Height[cell], map.Ramp[cell]);
					var corners = new (double, double, double)[4];
					for (var c = 0; c < 4; c++)
					{
						var depth = 32 * (dx + dy) + CornerY[c];
						corners[c] = (64 * (dx - dy) + CornerX[c], depth - 32 * (surface.CornerHeight(c) - map.Height[location]), depth);
					}
					roofs.Add(corners);
				}
			var spans = new List<(int X, int Y, int Height)>();
			for (var x = 0; x < 128; x++)
			{
				var px = offsetX + x + 0.5;
				var columns = new List<(double NearY, double FarY, double NearDepth, double FarDepth)>();
				foreach (var roof in roofs)
				{
					var nearY = double.PositiveInfinity; var farY = double.NegativeInfinity;
					var nearDepth = 0.0; var farDepth = 0.0;
					for (var i = 0; i < 4; i++)
					{
						var a = roof[i]; var b = roof[(i + 1) % 4];
						if (a.X == b.X || px < Math.Min(a.X, b.X) || px > Math.Max(a.X, b.X)) continue;
						var t = (px - a.X) / (b.X - a.X);
						var y = a.Y + t * (b.Y - a.Y); var depth = a.Depth + t * (b.Depth - a.Depth);
						if (y < nearY) { nearY = y; nearDepth = depth; }
						if (y > farY) { farY = y; farDepth = depth; }
					}
					if (!double.IsInfinity(nearY)) columns.Add((nearY, farY, nearDepth, farDepth));
				}
				var southwest = info.Slot <= 2 || info.Slot == 30 || info.Slot == 24 && x < 64 || info.Slot == 16 && x >= 64;
				var columnGeometry = info.SourceGrid ? RubberduckClosedCliffGeometry.Column(info.Slot, x + 0.5) :
					(southwest ? (x + 0.5) / 2 : 64 - (x + 0.5) / 2, 192.0);
				var start = -1;
				for (var y = 0; y <= sprite.Bounds.Height; y++)
				{
					var visible = y < sprite.Bounds.Height;
					var py = offsetY + y + 0.5;
					var geometry = info.SourceGrid ? RubberduckClosedCliffGeometry.Column(info.Slot, x + 0.5, y + 0.5 - topPadding) : columnGeometry;
					// Rear-facing source interiors are normally covered by the roof.
					// A ramp opening must not expose that opaque backface as a black
					// side wall. Keep the authored roof lip, not the hidden interior.
					if (info.SourceGrid && geometry.Item2 == 64 &&
						(y + 0.5 > geometry.Item1 + topPadding + 4 ||
						info.Slot >= 8 && info.Slot <= 10 && x > 65 ||
						info.Slot >= 12 && info.Slot <= 14 && x < 62)) visible = false;
					var v = Math.Clamp((y + 0.5 - topPadding - geometry.Item1) / geometry.Item2, 0, 1);
					// Unlike a vertical face, native rock depth changes down the leaning wall.
					var depth = info.GroundAbutment ? 0 : py - 128 * v + (info.SourceGrid ? 128 : 0);
					foreach (var column in columns)
					{
						if (column.FarDepth <= depth + 0.001) continue;
						var nearY = column.NearY;
						if (column.NearDepth < depth)
							nearY += (depth - column.NearDepth) / (column.FarDepth - column.NearDepth) * (column.FarY - column.NearY);
						if (py > nearY + 0.5) { visible = false; break; }
					}
					if (visible && start < 0) start = y;
					if (!visible && start >= 0)
					{
						var height = y - start;
						spans.Add((x, start, height));
						start = -1;
					}
				}
			}
			var result = new List<Sprite>();
			foreach (var group in spans.GroupBy(s => (s.Y, s.Height)))
			{
				var columns = group.Select(s => s.X).ToArray();
				for (var i = 0; i < columns.Length;)
				{
					var first = columns[i];
					var end = first + 1;
					i++;
					while (i < columns.Length && columns[i] == end) { end++; i++; }
					result.Add(new Sprite(sprite.Sheet, new Rectangle(sprite.Bounds.Left + first, sprite.Bounds.Top + group.Key.Y, end - first, group.Key.Height),
						0, new float3(offsetX + (first + end) / 2f, offsetY + group.Key.Y + group.Key.Height / 2f, 0), sprite.Channel, sprite.BlendMode));
				}
			}
			return result.ToArray();
		}
	}
}
