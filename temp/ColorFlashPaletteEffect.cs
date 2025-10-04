#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Traits
{
	[Desc("The cloak palette effect used by TA.")]
	public class ColorFlashPaletteEffectInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteReference(true)]
		[Desc("The name of the player palette to base off.")]
		public readonly string AffectedPalette = null;

		[Desc("The name of the player palette to base off.")]
		public readonly bool IsAffectedPalettePlayerColor = false;

		[Desc("Colors of the zaps. The amount of zaps are the amount of colors listed here.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(0, 0, 0, 30),
			Color.FromArgb(0, 0, 0, 50),
			Color.FromArgb(0, 0, 0, 100),
		};

		[Desc("Start Index to apply the effect.")]
		public readonly int StartIndex = 0;

		[Desc("End Index to apply the effect.")]
		public readonly int EndIndex = 32;

		public override object Create(ActorInitializer init) { return new ColorFlashPaletteEffect(this); }
	}

	public class ColorFlashPaletteEffect : ILoadsPlayerPalettes, IPaletteModifier, ITick
	{
		int t = 0;
		readonly ColorFlashPaletteEffectInfo info;
		readonly HashSet<string> palettes;

		public ColorFlashPaletteEffect(ColorFlashPaletteEffectInfo info)
		{
			this.info = info;
			palettes = new HashSet<string>();

			if (!info.IsAffectedPalettePlayerColor)
				palettes.Add(info.AffectedPalette);
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting)
		{
			if (!info.IsAffectedPalettePlayerColor)
				return;

			palettes.Add(info.AffectedPalette + playerName);
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			foreach (var ap in palettes)
			{
				var p = b[ap];

				for (var j = 0; j < info.Colors.Length; j++)
				{
					var k = (t + j) % 255 + 1;
					for (var l = k; l < 256; l += 32)
					{
						p.SetColor(l, info.Colors[j]);
					}
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			t += 1;
			if (t >= info.EndIndex) t = info.StartIndex;
		}
	}
}
