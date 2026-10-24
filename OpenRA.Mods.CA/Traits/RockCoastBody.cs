using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Projects native coast faces against the existing bank plane, shared with editor previews.")]
	public class RockCoastBodyInfo : TraitInfo, IRenderActorPreviewInfo
	{
		public override object Create(ActorInitializer init) => new RockCoastBody();
		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			yield return new CliffEditorPreview((map, cell) => RockCoastBody.Build(map, cell, init.Actor.Name));
		}
	}

	public class RockCoastBody : IRender
	{
		Sprite[] sprites;
		IRenderable[] renderables;
		long revision = -1;
		WPos position;
		internal static Sprite[] Build(Map map, CPos location, string image)
		{
			if (!map.Contains(location)) return Array.Empty<Sprite>();
			var sprite = map.Sequences.GetSequence(image, "idle").GetSprite(0);
			var direction = image.StartsWith("terrain.rubberduck.coast-native-2-", StringComparison.Ordinal) ? 2 :
				image.StartsWith("terrain.rubberduck.coast-native-3-", StringComparison.Ordinal) ? 3 : -1;
			if (sprite.Bounds.Width != 128 || sprite.Bounds.Height != 256)
				throw new InvalidOperationException("Expected original 128x256 rock-coast source.");
			var frontCorner = image.Contains("coast-piece-24.", StringComparison.Ordinal) || image.Contains("coast-piece-25.", StringComparison.Ordinal);
			var rearCorner = image.Contains("coast-piece-29.", StringComparison.Ordinal);
			var lowCap = image.Contains("coast-abutment-", StringComparison.Ordinal) || image.Contains("coast-piece-28.", StringComparison.Ordinal);
			var sideCorner = image.Contains("coast-piece-26.", StringComparison.Ordinal) || image.Contains("coast-piece-27.", StringComparison.Ordinal) ||
				image.Contains("coast-piece-30.", StringComparison.Ordinal) || image.Contains("coast-piece-31.", StringComparison.Ordinal);
			var offset = sprite.Offset + new float3(0, direction >= 2 || rearCorner ? 64 : frontCorner ? -64 : 0, 0);
			bool Land(double x, double y)
			{
				var cell = location + new CVec((int)Math.Floor(x / 128 + y / 64 + 0.5), (int)Math.Floor(y / 64 - x / 128 + 0.5));
				return map.Contains(cell) && map.Tiles[cell].Type != MapGeneration.RubberduckRockCoastPlan.BlockedWater &&
					map.GetTerrainInfo(cell).Type != "Water" && map.Height[cell] >= map.Height[location];
			}

			// Occlude the cliff interior where the existing near bank actually lies.
			// At bends this also hides forward-facing rock below intervening land.
			// Rear-facing components retain a narrow crest even at their short forward
			// steps. Side-corner interiors use the same bank-relative band instead of
			// exposing the source's raised roof triangle. No terrain/pixel edits occur.
			var rearBank = IsRearBank(map, location);
			var hidden = new bool[128 * 256];
			for (var y = 0; y < 256; y++)
				for (var x = 0; x < 128; x++)
				{
					var px = offset.X - 64 + x + 0.5;
					var py = offset.Y - 128 + y + 0.5;
					hidden[y * 128 + x] = Land(px, py) && Land(px, py - 6) && Land(px - 4, py) && Land(px + 4, py) ||
						direction == 3 && x > 65 || direction == 2 && x < 62 ||
						rearCorner && y + 0.5 > 128 - Math.Abs(x + 0.5 - 64) / 2 + 4 ||
						(rearBank || sideCorner || rearCorner || lowCap) && !Land(px, py) && !Land(px - 16, py) && !Land(px + 16, py) &&
						!Land(px, py - 12) && !Land(px, py + 12);
				}
			return RubberduckMaterialLayer.ExclusionRectangles(hidden, 128, 256).Select(r => new Sprite(sprite.Sheet,
				new Rectangle(sprite.Bounds.X + r.X, sprite.Bounds.Y + r.Y, r.Width, r.Height), 0,
				offset + new float3(r.X + r.Width / 2 - 64, r.Y + r.Height / 2 - 128, 0),
				sprite.Channel, sprite.BlendMode)).ToArray();
		}

		static bool IsRearBank(Map map, CPos location)
		{
			bool Bank(CPos c) => map.Contains(c) && (map.Tiles[c].Type == 14011 || map.Tiles[c].Type == 14012);
			var pending = new Queue<CPos>(); var seen = new HashSet<CPos>();
			for (var x = -1; x <= 1; x++)
				for (var y = -1; y <= 1; y++)
					if (Bank(location + new CVec(x, y))) pending.Enqueue(location + new CVec(x, y));
			var rear = 0; var front = 0;
			while (pending.Count != 0)
			{
				var c = pending.Dequeue(); if (!seen.Add(c)) continue;
				for (var d = 0; d < 4; d++)
				{
					var n = c + MapGeneration.PlateauTopology.Directions[d];
					if (map.Contains(n) && (map.Tiles[n].Type == 14010 || map.GetTerrainInfo(n).Type == "Water"))
						if (d >= 2) rear++; else front++;
				}
				for (var x = -1; x <= 1; x++)
					for (var y = -1; y <= 1; y++)
						if (Bank(c + new CVec(x, y)) && !seen.Contains(c + new CVec(x, y))) pending.Enqueue(c + new CVec(x, y));
			}
			return rear > front;
		}

		internal Sprite[] Prepare(Actor self)
		{
			var current = TerrainRenderRevision.Get(self.World.Map);
			if (sprites == null || revision != current)
			{
				sprites = Build(self.World.Map, self.Location, self.Info.Name);
				revision = current; renderables = null;
			}
			if (renderables == null || position != self.CenterPosition)
			{
				position = self.CenterPosition;
				renderables = new IRenderable[] { new CliffSliceRenderable(sprites, position) };
			}
			return sprites;
		}
		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr) { Prepare(self); return renderables; }
		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			Prepare(self);
			yield return new CliffSliceRenderable(sprites, position).ScreenBounds(wr);
		}
	}
}
