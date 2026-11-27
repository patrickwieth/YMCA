using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Compatibility;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Decorative one-cell links between real, same-owner building foundations. Never changes occupancy or build radius.")]
	public class BibConnectionLayerInfo : TraitInfo
	{
		public readonly string[] TerrainTypes = { "Clear", "Road", "Rough", "Ore", "Gems", "Beach" };
		public override object Create(ActorInitializer init) => new BibConnectionLayer(this);
	}

	public class BibConnectionLayer : IRender
	{
		static readonly Regex BibName = new("^(allied|soviet|gdi|nod|scrin)bib([1-9][0-9]*)x([1-9][0-9]*)$", RegexOptions.Compiled);
		readonly BibConnectionLayerInfo info;
		readonly Dictionary<string, Animation> animations = new();
		readonly List<(CPos Cell, Actor First, Actor Second, string Image, string Palette)> links = new();
		int cachedTick = -1;
		Player cachedPlayer;

		internal int ConnectionCount => links.Count;

		public BibConnectionLayer(BibConnectionLayerInfo info) { this.info = info; }

		Animation Animation(World world, string image, string sequence = "idle")
		{
			var key = image + ":" + sequence;
			if (!animations.TryGetValue(key, out var animation))
			{
				animation = new Animation(world, image) { IsDecoration = true };
				animation.PlayRepeating(sequence);
				animations.Add(key, animation);
			}

			return animation;
		}

		void Rebuild(WorldRenderer wr)
		{
			var world = wr.World;
			var map = world.Map;
			var cells = new List<BibGapResolver.Cell>();
			var blocked = new HashSet<(int, int)>();
			var sources = new Dictionary<uint, (Actor Actor, string Image, string Palette)>();
			links.Clear();
			foreach (var entry in world.ActorsWithTrait<Building>())
			{
				var actor = entry.Actor;
				if (!actor.IsInWorld || actor.IsDead)
					continue;

				foreach (var cell in entry.Trait.Info.OccupiedTiles(entry.Trait.TopLeft))
					blocked.Add((cell.X, cell.Y));

				// Do not use hidden enemy state to draw connections through fog.
				if (world.FogObscures(actor))
					continue;

				foreach (var bib in actor.TraitsImplementing<WithIdleOverlay>())
				{
					if (bib.IsTraitDisabled || bib.Info.Image == null)
						continue;
					var match = BibName.Match(bib.Info.Image);
					if (!match.Success)
						continue;

					var width = int.Parse(match.Groups[2].Value);
					var height = int.Parse(match.Groups[3].Value);
					var animation = Animation(world, bib.Info.Image, bib.Info.Sequence);
					var sprite = animation.Image;
					var scale = animation.CurrentSequence.Scale;
					// Raised Nod/Scrin canvases have extra pixels below the top plane.
					var planeY = match.Groups[1].Value == "nod" ? sprite.Size.X / 4 - sprite.Size.Y / 2 :
						match.Groups[1].Value == "scrin" ? sprite.Size.X / 4 + 3 - sprite.Size.Y / 2 : 0;
					var pixelOffset = scale * (sprite.Offset + new float3(0, planeY, 0));
					var body = actor.Trait<BodyOrientation>();
					var center = actor.CenterPosition + body.LocalToWorld(bib.Info.Offset.Rotate(body.QuantizeOrientation(actor.Orientation))) +
						new WVec((int)Math.Round(pixelOffset.X * wr.TileScale / wr.TileSize.Width),
							(int)Math.Round(pixelOffset.Y * wr.TileScale / wr.TileSize.Height), 0);
					var halfExtent = (map.CenterOfCell(new CPos(width - 1, height - 1)) - map.CenterOfCell(CPos.Zero)) / 2;
					var origin = map.CellContaining(center - halfExtent);
					var expected = map.CenterOfCell(origin) + halfExtent;
					// Never guess connectivity for art that is not registered to the cell grid.
					if (Math.Abs(center.X - expected.X) > 24 || Math.Abs(center.Y - expected.Y) > 24)
						continue;

					var image = match.Groups[1].Value + "bib1x1";
					var remap = actor.TraitOrDefault<WithBibRemap>();
					var palette = remap?.Matches(bib.Info.Image) == true && remap.Info.RemapConnections ? remap.Info.PaletteFor(bib.Info.Image) : null;
					sources[actor.ActorID] = (actor, image, palette);
					for (var x = 0; x < width; x++)
						for (var y = 0; y < height; y++)
							cells.Add(new BibGapResolver.Cell(origin.X + x, origin.Y + y, actor.ActorID, actor.Owner.InternalName));
				}
			}

			foreach (var gap in BibGapResolver.Resolve(cells, blocked))
			{
				var cell = new CPos(gap.X, gap.Y);
				if (!map.Contains(cell) || world.FogObscures(cell) || !info.TerrainTypes.Contains(map.GetTerrainInfo(cell).Type))
					continue;
				var first = sources[gap.First];
				var second = sources[gap.Second];
				// No links across cliff/height boundaries.
				var z = map.CenterOfCell(cell).Z;
				if (first.Actor.CenterPosition.Z != z || second.Actor.CenterPosition.Z != z)
					continue;
				links.Add((cell, first.Actor, second.Actor, first.Image, first.Palette));
			}
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (cachedTick != self.World.WorldTick || cachedPlayer != self.World.RenderPlayer || self.World.Type == WorldType.Editor)
			{
				Rebuild(wr);
				cachedTick = self.World.WorldTick;
				cachedPlayer = self.World.RenderPlayer;
			}

			foreach (var link in links)
			{
				if (!link.First.IsInWorld || !link.Second.IsInWorld || link.First.IsDead || link.Second.IsDead ||
					link.First.Owner != link.Second.Owner || self.World.FogObscures(link.Cell))
					continue;
				var position = self.World.Map.CenterOfCell(link.Cell);
				var animation = Animation(self.World, link.Image, link.Palette != null && link.Image.StartsWith("sovietbib") ? "grid" : "idle");
				// Normalize the 1x1 top plane to the gap cell (raised art retains its lip).
				var sprite = animation.Image;
				var planeY = link.Image.StartsWith("nodbib") ? sprite.Size.X / 4 - sprite.Size.Y / 2 :
					link.Image.StartsWith("scrinbib") ? sprite.Size.X / 4 + 3 - sprite.Size.Y / 2 : 0;
				var offset = animation.CurrentSequence.Scale * (sprite.Offset + new float3(0, planeY, 0));
				position -= new WVec((int)Math.Round(offset.X * wr.TileScale / wr.TileSize.Width),
					(int)Math.Round(offset.Y * wr.TileScale / wr.TileSize.Height), 0);
				foreach (var renderable in animation.Render(position, wr.Palette("terrain")))
					yield return renderable;
				if (link.Palette != null)
					foreach (var renderable in Animation(self.World, link.Image, "remap").Render(position, WVec.Zero, 1,
						wr.Palette(link.Palette + link.First.Owner.InternalName)))
						yield return renderable;
			}
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr) => Array.Empty<Rectangle>();
	}
}
