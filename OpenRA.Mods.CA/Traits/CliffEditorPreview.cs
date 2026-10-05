using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	sealed class CliffEditorPreview : IActorPreview
	{
		readonly Func<Map, CPos, Sprite[]> build;
		Sprite[] sprites;
		CPos cachedCell;
		long revision = -1;
		public CliffEditorPreview(Func<Map, CPos, Sprite[]> build) { this.build = build; }
		public void Tick() { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos)
		{
			var cell = wr.World.Map.CellContaining(pos);
			var current = TerrainRenderRevision.Get(wr.World.Map);
			if (sprites == null || cell != cachedCell || revision != current)
			{
				sprites = build(wr.World.Map, cell);
				cachedCell = cell;
				revision = current;
			}
			foreach (var sprite in sprites)
				yield return new SpriteRenderable(sprite, pos, WVec.Zero, 0, null, 1, 1, float3.Ones, TintModifiers.None, true);
		}
		public IEnumerable<IRenderable> RenderUI(WorldRenderer wr, int2 pos, float scale) { yield break; }
		public IEnumerable<Rectangle> ScreenBounds(WorldRenderer wr, WPos pos)
		{
			var p = wr.ScreenPxPosition(pos);
			yield return new Rectangle(p.X - 192, p.Y - 64, 384, 320);
		}
	}
}
