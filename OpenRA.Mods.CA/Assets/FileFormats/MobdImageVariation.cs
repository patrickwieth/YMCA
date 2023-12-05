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
using System.Linq;
using System.Text;

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class MobdImageVariation
	{
		public readonly bool Mirrored;
		public readonly bool IsCompressed;
		public readonly bool Has256Colors;
		public readonly bool Unk1; // TODO external palette related?
		public readonly bool Unk2; // TODO external palette related?
		public readonly uint Image;
		public readonly uint? Palette;

		public MobdImageVariation(Stream stream, AssetFormat assetFormat)
		{
			var type = Encoding.ASCII.GetString(stream.ReadBytes(4).Reverse().ToArray());
			var flags = stream.ReadUInt32();

			if (assetFormat <= AssetFormat.Kknd1)
			{
				if (type != "SPRT")
					throw new Exception("MOBD: ImageVariation type is wrong!");

				if ((flags & 0xfffffffe) != 0)
					throw new Exception("MOBD: Unknown Flags");

				Mirrored = (flags & 0x1) == 1;
			}
			else
			{
				if (type != "SPNS" && type != "SPRC") // TODO does this have any impact?
					throw new Exception("MOBD: ImageVariation type is wrong!");

				if ((flags & 0x73fffffc) != 0)
					throw new Exception("MATD: Unknown Flags");

				Mirrored = ((flags >> 31) & 0x1) == 1;
				IsCompressed = ((flags >> 27) & 0x1) == 1;
				Has256Colors = ((flags >> 26) & 0x1) == 1;
				Unk1 = ((flags >> 1) & 0x1) == 1;
				Unk2 = ((flags >> 0) & 0x1) == 1;

				Palette = OffsetUtils.ReadUInt32Offset(stream);
			}

			Image = OffsetUtils.ReadUInt32Offset(stream);
		}
	}
}
