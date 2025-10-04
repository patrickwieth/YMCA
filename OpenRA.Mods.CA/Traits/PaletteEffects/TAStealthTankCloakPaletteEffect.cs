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

using System;
using System.Collections.Generic;
using System.Globalization;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Tiberian Aurora stealth tank cloak palette effect.")]
	public class TAStealthTankCloakPaletteEffectInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteReference(true)]
		[Desc("Palette modified by the cloak shimmer.")]
		public readonly string AffectedPalette = null;

		[Desc("Colors of the shimmering zaps. Each entry becomes one moving highlight.")]
		public readonly Color[] Colors =
		{
			Color.FromArgb(255, 128, 255, 128),
			Color.FromArgb(128, 80, 255, 80),
			Color.FromArgb(64, 0, 255, 0)
		};

		[Desc("Override shimmer colors using ARGB hex values (optional).")]
		public readonly string[] ColorsHex = Array.Empty<string>();

		[Desc("Lower index bound for the shimmer loop.")]
		public readonly int StartIndex = 0;

		[Desc("Upper index bound for the shimmer loop.")]
		public readonly int EndIndex = 32;

		internal Color[] ResolveColors()
		{
			if (ColorsHex.Length == 0)
				return Colors;

			var result = new Color[ColorsHex.Length];
			for (var i = 0; i < ColorsHex.Length; i++)
			{
				var hex = ColorsHex[i];
				if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
				{
					result[i] = Colors[Math.Min(i, Colors.Length - 1)];
					continue;
				}

				var a = (int)((value >> 24) & 0xFF);
				var r = (int)((value >> 16) & 0xFF);
				var g = (int)((value >> 8) & 0xFF);
				var b = (int)(value & 0xFF);

				result[i] = Color.FromArgb(a, r, g, b);
			}

			return result;
		}

		public override object Create(ActorInitializer init)
		{
			return new TAStealthTankCloakPaletteEffect(this);
		}
	}

	public class TAStealthTankCloakPaletteEffect : IPaletteModifier, ITick
	{
		int t;
		readonly TAStealthTankCloakPaletteEffectInfo info;
		readonly Color[] colors;

		public TAStealthTankCloakPaletteEffect(TAStealthTankCloakPaletteEffectInfo info)
		{
			this.info = info;
			colors = info.ResolveColors();
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (!palettes.TryGetValue(info.AffectedPalette, out var palette))
				return;

			for (var i = 0; i < colors.Length; i++)
			{
				var primaryIndex = (t + i) % 255 + 1;
				var secondaryIndex = (primaryIndex + 32) % 255;

				palette.SetColor(primaryIndex, colors[i]);
				palette.SetColor(secondaryIndex, colors[i]);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (++t >= info.EndIndex)
				t = info.StartIndex;
		}
	}
}
