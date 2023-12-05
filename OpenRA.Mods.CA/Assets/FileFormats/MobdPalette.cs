#region Copyright & License Information

/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class MobdPalette
	{
		public readonly Color[] Palette;

		public MobdPalette(Stream stream)
		{
			var unk1 = stream.ReadInt32();
			var unk2 = stream.ReadInt32();
			var unk3 = stream.ReadInt32();

			if (unk1 != int.MinValue || unk2 != int.MinValue || unk3 != int.MinValue)
				throw new Exception("MOBD: Wrong value!");

			Palette = new Color[stream.ReadUInt16()];

			for (var i = 0; i < Palette.Length; i++)
			{
				var color16 = stream.ReadUInt16();
				var r = ((color16 & 0x7c00) >> 7) & 0xff;
				var g = ((color16 & 0x03e0) >> 2) & 0xff;
				var b = ((color16 & 0x001f) << 3) & 0xff;
				Palette[i] = Color.FromArgb(i == 0 ? 0x00 : 0xff, r, g, b);
			}
		}
	}
}
