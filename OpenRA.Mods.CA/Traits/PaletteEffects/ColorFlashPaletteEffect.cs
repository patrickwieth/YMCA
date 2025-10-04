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

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Color flashing effect imported from TiberianAurora.")]
	public class ColorFlashPaletteEffectInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteReference(true)]
		[Desc("The palette to apply the flash effect to.")]
		public readonly string AffectedPalette = null;

		[Desc("Whether the affected palette should follow the player color.")]
		public readonly bool IsAffectedPalettePlayerColor = false;

		[Desc("Colors used for the flash animation. Number of entries controls how many zaps are displayed.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(0, 0, 0, 30),
			Color.FromArgb(0, 0, 0, 50),
			Color.FromArgb(0, 0, 0, 100),
		};

		[Desc("Lower bound for the frame index stepping.")]
		public readonly int StartIndex = 0;

		[Desc("Upper bound for the frame index stepping.")]
		public readonly int EndIndex = 32;

		public override object Create(ActorInitializer init) { return new ColorFlashPaletteEffect(this); }
	}

	public class ColorFlashPaletteEffect : ILoadsPlayerPalettes, IPaletteModifier, ITick
	{
		int t;
		readonly ColorFlashPaletteEffectInfo info;
		readonly HashSet<string> palettes;

		public ColorFlashPaletteEffect(ColorFlashPaletteEffectInfo info)
		{
			this.info = info;
			palettes = new HashSet<string>();

			if (!info.IsAffectedPalettePlayerColor && info.AffectedPalette != null)
				palettes.Add(info.AffectedPalette);
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting)
		{
			if (!info.IsAffectedPalettePlayerColor || info.AffectedPalette == null)
				return;

			palettes.Add(info.AffectedPalette + playerName);
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettesByName)
		{
			foreach (var paletteName in palettes)
			{
				if (!palettesByName.TryGetValue(paletteName, out var palette))
					continue;

				for (var i = 0; i < info.Colors.Length; i++)
				{
					var k = (t + i) % 255 + 1;
					for (var index = k; index < 256; index += 32)
						palette.SetColor(index, info.Colors[i]);
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (++t >= info.EndIndex)
				t = info.StartIndex;
		}
	}
}
