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
		const int ColumnStride = 32;

		int t;
		readonly ColorFlashPaletteEffectInfo info;
		readonly HashSet<string> palettes;
		readonly Dictionary<string, Dictionary<int, Color>> highlightedIndices;
		readonly int startIndex;
		readonly int endIndex;
		readonly int span;

		public ColorFlashPaletteEffect(ColorFlashPaletteEffectInfo info)
		{
			this.info = info;
			palettes = new HashSet<string>();
			highlightedIndices = new Dictionary<string, Dictionary<int, Color>>();

			if (!info.IsAffectedPalettePlayerColor && info.AffectedPalette != null)
				palettes.Add(info.AffectedPalette);

			startIndex = Math.Max(0, info.StartIndex);
			if (startIndex >= Palette.Size)
				startIndex = Palette.Size - 1;

			var configuredEnd = info.EndIndex <= 0 ? Palette.Size - 1 : info.EndIndex;
			if (configuredEnd >= Palette.Size)
				configuredEnd = Palette.Size - 1;
			if (configuredEnd < startIndex)
				configuredEnd = startIndex;

			endIndex = configuredEnd;
			span = Math.Max(1, endIndex - startIndex + 1);
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting)
		{
			if (!info.IsAffectedPalettePlayerColor || info.AffectedPalette == null)
				return;

			palettes.Add(info.AffectedPalette + playerName);
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettesByName)
		{
			if (info.Colors.Length == 0)
				return;

			var bandLength = Math.Min(info.Colors.Length, span);
			if (bandLength == 0)
				return;

			foreach (var paletteName in palettes)
			{
				if (!palettesByName.TryGetValue(paletteName, out var palette))
					continue;

				if (!highlightedIndices.TryGetValue(paletteName, out var previousHighlights))
				{
					previousHighlights = new Dictionary<int, Color>();
					highlightedIndices.Add(paletteName, previousHighlights);
				}
				else
				{
					foreach (var entry in previousHighlights)
						palette.SetColor(entry.Key, entry.Value);
					previousHighlights.Clear();
				}

				for (var column = startIndex; column <= endIndex; column++)
				{
					var wavePosition = (column - startIndex + t) % span;
					if (wavePosition >= bandLength)
						continue;

					var highlightColor = info.Colors[wavePosition];
					for (var index = column; index < Palette.Size; index += ColumnStride)
					{
						if (!previousHighlights.ContainsKey(index))
							previousHighlights[index] = palette.GetColor(index);

						palette.SetColor(index, highlightColor);
					}
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (span <= 0)
				return;

			if (++t >= span)
				t = 0;
		}
	}
}
