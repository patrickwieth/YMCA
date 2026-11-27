using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.Traits
{
	// Runtime conditions remain standard WithIdleOverlay conditions; only previews
	// need the explicit builder faction instead of EnabledByDefault.
	public sealed class WithFactionBibOverlayInfo : WithIdleOverlayInfo, IRenderActorPreviewSpritesInfo
	{
		public readonly HashSet<string> Factions = new();
		public readonly HashSet<string> ExcludeFactions = new();
		public bool MatchesFaction(string faction) => (Factions.Count == 0 || Factions.Contains(faction)) && !ExcludeFactions.Contains(faction);
		public bool MatchesPreview(ActorPreviewInitializer init) => MatchesFaction(init.GetValue<FactionInit, string>(init.Get<OwnerInit>().Value(init.World).Faction.InternalName));

		public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!MatchesPreview(init)) yield break;
			if (Palette != null) p = init.WorldRenderer.Palette(IsPlayerPalette ? Palette + init.Get<OwnerInit>().InternalName : Palette);
			var facing = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
			var animation = new Animation(init.World, Image ?? image, () => facing) { IsDecoration = IsDecoration };
			animation.PlayRepeating(RenderSprites.NormalizeSequence(animation, init.GetDamageState(), Sequence));
			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			WVec Position() => body.LocalToWorld(Offset.Rotate(body.QuantizeOrientation(WRot.FromYaw(facing), facings)));
			int ZOffset() { var offset = Position(); return offset.Y + offset.Z + 1; }
			yield return new SpriteActorPreview(animation, Position, ZOffset, p);
		}
	}
}
