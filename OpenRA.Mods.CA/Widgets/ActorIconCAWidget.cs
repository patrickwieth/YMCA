#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;


namespace OpenRA.Mods.CA.Widgets
{
	public class ActorIconCAWidget : Widget
	{
		public Actor actor;
		public readonly World World;
		readonly WorldRenderer worldRenderer;
		Animation icon;
		string palette;
		IProductionIconOverlay[] pios;
		public readonly int2 IconSize = new int2(64, 48);

		[ObjectCreator.UseCtor]
		public ActorIconCAWidget(World world, WorldRenderer worldRenderer)
		{
			actor = null;
			World = world;
			this.worldRenderer = worldRenderer;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);
		}

		public void setActor(Actor selectedActor)
		{
			actor = selectedActor;
			var faction = actor.Owner.Faction.InternalName;
			var rsi = actor.Info.TraitInfoOrDefault<RenderSpritesInfo>();
			icon = new Animation(World, rsi.GetImage(actor.Info, faction));
			var bi = actor.Info.TraitInfoOrDefault<BuildableInfo>();
			if (bi == null)
			{
					actor = null;
					return;
			}
			icon.Play(bi.Icon);
			palette = bi.IconPaletteIsPlayerPalette ? bi.IconPalette + actor.Owner.InternalName : bi.IconPalette;

			pios = actor.Owner.PlayerActor.TraitsImplementing<IProductionIconOverlay>().ToArray();
		}

		public override void Draw()
		{
			if (actor != null)
			{
				WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(palette), RenderOrigin, 2.0f);

				foreach (var pio in pios.Where(p => p.IsOverlayActive(actor.Info)))
					WidgetUtils.DrawSpriteCentered(pio.Sprite, worldRenderer.Palette(pio.Palette), RenderOrigin + 2*pio.Offset(IconSize ), 2.0f);

			}
		}
	}
}
