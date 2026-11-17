using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.Render
{
	[Desc("Optional approximate contact shadow for ground sprites without a separate shadow asset. Not a projected vehicle silhouette.")]
	public class WithTerrainContactShadowInfo : ConditionalTraitInfo, Requires<MobileInfo>
	{
		public readonly WDist HalfLength = new WDist(384);
		public readonly WDist HalfWidth = new WDist(256);
		public readonly Color Color = Color.FromArgb(65, 0, 0, 0);
		public override object Create(ActorInitializer init) => new WithTerrainContactShadow(this);
	}

	public class WithTerrainContactShadow : ConditionalTrait<WithTerrainContactShadowInfo>, IRender
	{
		public WithTerrainContactShadow(WithTerrainContactShadowInfo info) : base(info) { }

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || Info.HalfLength.Length <= 0 || Info.HalfWidth.Length <= 0 ||
				Math.Abs(self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length) > 128)
				yield break;
			var map = self.World.Map;
			WPos Ground(WPos p) => p - new WVec(0, 0, map.DistanceAboveTerrain(p).Length);
			var center = Ground(self.CenterPosition);
			var points = new WPos[16];
			for (var i = 0; i < points.Length; i++)
			{
				var angle = new WAngle(i * 1024 / points.Length);
				var local = new WVec(Info.HalfWidth.Length * angle.Cos() / 1024,
					Info.HalfLength.Length * angle.Sin() / 1024, 0).Rotate(WRot.FromYaw(self.Orientation.Yaw));
				var p = center + local;
				if (!map.Contains(map.CellContaining(p))) yield break;
				points[i] = Ground(p);
				// Do not stretch a shadow polygon down a retaining wall. Rendering is
				// suppressed rather than inventing a smooth surface across a cliff.
				if (Math.Abs(points[i].Z - center.Z) > 724) yield break;
			}
			yield return new ContactShadowRenderable(center, points, Info.Color, -10);
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var renderable in ((IRender)this).Render(self, wr))
				yield return renderable.PrepareRender(wr).ScreenBounds(wr);
		}

		sealed class ContactShadowRenderable : IRenderable, IFinalizedRenderable
		{
			readonly WPos[] points;
			readonly Color color;
			public WPos Pos { get; }
			public int ZOffset { get; }
			public bool IsDecoration => true;
			public ContactShadowRenderable(WPos pos, WPos[] points, Color color, int zOffset)
			{ Pos = pos; this.points = points; this.color = color; ZOffset = zOffset; }
			public IRenderable WithZOffset(int zOffset) => new ContactShadowRenderable(Pos, points, color, zOffset);
			public IRenderable OffsetBy(in WVec offset)
			{
				var delta = offset;
				return new ContactShadowRenderable(Pos + delta, points.Select(p => p + delta).ToArray(), color, ZOffset);
			}
			public IRenderable AsDecoration() => this;
			public IFinalizedRenderable PrepareRender(WorldRenderer wr) => this;
			public void Render(WorldRenderer wr)
			{
				var center = wr.Screen3DPosition(Pos);
				for (var i = 0; i < points.Length; i++)
					Game.Renderer.WorldRgbaColorRenderer.FillTriangle(center, wr.Screen3DPosition(points[i]),
						wr.Screen3DPosition(points[(i + 1) % points.Length]), color);
			}
			public Rectangle ScreenBounds(WorldRenderer wr)
			{
				var screen = points.Select(wr.ScreenPxPosition).ToArray();
				return Rectangle.FromLTRB(screen.Min(p => p.X), screen.Min(p => p.Y), screen.Max(p => p.X) + 1, screen.Max(p => p.Y) + 1);
			}
			public void RenderDebugGeometry(WorldRenderer wr) { }
		}
	}
}
