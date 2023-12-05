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

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class MobdImage
	{
		public readonly uint Width;
		public readonly uint Height;
		public readonly byte[] Pixels;

		public MobdImage(Stream stream, AssetFormat assetFormat, MobdImageVariation imageVariation)
		{
			Width = stream.ReadUInt32();
			Height = stream.ReadUInt32();

			if (assetFormat == AssetFormat.Kknd1)
			{
				var flags = stream.ReadUInt8();

				if ((flags & 0xfd) != 0)
					throw new Exception("MOBD: Unknown flags!");

				Pixels = flags >> 1 == 1
					? Decompressor.DecompressSprt(stream, Width, Height)
					: stream.ReadBytes((int)(Width * Height));
			}
			else
				Pixels = imageVariation.IsCompressed
					? Decompressor.DecompressSpnsSprc(stream, Width, Height, imageVariation.Has256Colors)
					: stream.ReadBytes((int)(Width * Height));
		}
	}
}
