using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("The six co-registered bridge sheets: deck below traffic, steel frame above it.")]
	public class RubberduckBridgeBodyInfo : TraitInfo, IRenderActorPreviewInfo
	{
		public readonly int Length = 3;
		public readonly bool AlongY = false;
		public RubberduckBridgeBodyInfo() { }
		internal RubberduckBridgeBodyInfo(int length, bool alongY) { Length = length; AlongY = alongY; }
		public override object Create(ActorInitializer init) => new RubberduckBridgeBody(this);
		public IEnumerable<IActorPreview> RenderPreview(ActorPreviewInitializer init)
		{
			yield return new RubberduckBridgePreview(this, init.Actor.TraitInfo<BuildingInfo>().CenterOffset(init.World));
		}
	}

	public class RubberduckBridgeBody : IRender
	{
		readonly RubberduckBridgeBodyInfo info;
		IRenderable[] cached;
		WPos cachedStart;
		long revision = -1;
		public RubberduckBridgeBody(RubberduckBridgeBodyInfo info) { this.info = info; }

		// The 256px canvas is NOT a tile pitch. The two authored vertical joins
		// are x=70 and x=145: repeat (75, 37.5), with one common transform for
		// all six sheets. Fit the complete assembly, never individual pieces.
		internal static (int Repeats, float Scale) Layout(int length)
		{
			if (length < 3) throw new InvalidOperationException("Bridge needs two land ends and at least one water cell.");
			var distance = (length - 1) * 64f;
			var repeats = Math.Max(1, (int)Math.Round((distance - 160) / 75) + 1);
			return (repeats, distance / (160 + 75 * (repeats - 1)));
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr) => Render(wr, self.World.Map.CenterOfCell(self.Location));

		internal IEnumerable<IRenderable> Render(WorldRenderer wr, WPos start)
		{
			var current = TerrainRenderRevision.Get(wr.World.Map);
			if (cached == null || start != cachedStart || current != revision)
			{
				cached = Build(wr, start).ToArray(); cachedStart = start; revision = current;
			}
			return cached;
		}

		IEnumerable<IRenderable> Build(WorldRenderer wr, WPos start)
		{
			var (repeats, scale) = Layout(info.Length);
			var suffix = info.AlongY ? "-y" : "";
			foreach (var frame in new[] { false, true })
			{
				var prefix = frame ? "frame-" : "deck-";
				for (var part = -1; part <= repeats; part++)
				{
					var name = part == -1 ? "left" : part == repeats ? "right" : "center";
					var repeat = part == -1 ? 0 : part == repeats ? repeats - 1 : part;
					var sprite = wr.World.Map.Sequences.GetSequence("rubberduck.bridge", prefix + name + (frame ? suffix : "")).GetSprite(0);
					if (Math.Abs(sprite.Size.X) != 256 || sprite.Size.Y != 256 || sprite.Offset != float3.Zero)
						throw new InvalidOperationException("Bridge sheets must retain their shared 256x256 canvas and origin.");
					var x = (80 + repeat * 75) * scale;
					var y = (30 + repeat * 37.5f) * scale;
					var offset = new WVec((int)Math.Round((info.AlongY ? -x : x) * 724 / 64), (int)Math.Round(y * 724 / 32), 0);
					var palette = wr.Palette("bridge-" + prefix + name);
					if (frame)
						yield return new SpriteRenderable(sprite, start + offset, WVec.Zero, 2172, palette, scale, 1, float3.Ones, TintModifiers.None, false);
					else
						yield return new BridgeDeckRenderable(sprite, BankClip(wr.World.Map, start, repeat, scale), start + offset, palette, scale, info.AlongY);
				}
			}
		}

		internal Rectangle[] BankClip(Map map, WPos start, int repeat, float scale)
		{
			var hidden = new bool[256 * 256];
			for (var x = 0; x < 256; x++)
			{
				var occluded = false;
				for (var y = 0; y < 256; y++)
				{
					// Preserve the entire road, its front rail and thickness. Only the
					// lower pier pixels can disappear behind existing flat dry ground.
					if (y <= x * .5 + 112) continue;
					if (occluded) { hidden[y * 256 + x] = true; continue; }
					var dx = (x + .5 - 48 + repeat * 75) * scale;
					var dy = (y + .5 - 98 + repeat * 37.5) * scale;
					var pos = start + new WVec((int)Math.Round((info.AlongY ? -dx : dx) * 724 / 64), (int)Math.Round(dy * 724 / 32), 0);
					var cell = map.CellContaining(pos);
					if (!map.Contains(cell) || map.Height[cell] != 0 || map.Ramp[cell] != 0) continue;
					var tile = map.Tiles[cell].Type;
					occluded |= tile == 1000 || tile == 1010 || tile == 1020 || tile == 1030 || tile == 1040;
					// A bank hides the lower column too: do not leave an isolated
					// pier foot visible again beyond a jagged shoreline cell.
					hidden[y * 256 + x] = occluded;
				}
			}
			return RubberduckMaterialLayer.ExclusionRectangles(hidden, 256, 256);
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			foreach (var r in ((IRender)this).Render(self, wr))
				yield return Bounds(wr, r.Pos);
		}
		internal Rectangle Bounds(WorldRenderer wr, WPos position)
		{
			var radius = (int)Math.Ceiling(128 * Layout(info.Length).Scale) + 2;
			var p = wr.ScreenPxPosition(position);
			return new Rectangle(p.X - radius, p.Y - radius, radius * 2, radius * 2);
		}
	}

	sealed class RubberduckBridgePreview : IActorPreview
	{
		readonly RubberduckBridgeBody body;
		readonly WVec centerOffset;
		public RubberduckBridgePreview(RubberduckBridgeBodyInfo info, WVec centerOffset)
		{
			body = new RubberduckBridgeBody(info); this.centerOffset = centerOffset;
		}
		public void Tick() { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, WPos pos) => body.Render(wr, pos - centerOffset);
		public IEnumerable<IRenderable> RenderUI(WorldRenderer wr, int2 pos, float scale) { yield break; }
		public IEnumerable<Rectangle> ScreenBounds(WorldRenderer wr, WPos pos) =>
			Render(wr, pos).Select(r => body.Bounds(wr, r.Pos));
	}
}
