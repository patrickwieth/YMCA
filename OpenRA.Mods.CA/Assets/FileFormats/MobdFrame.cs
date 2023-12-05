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

using System.IO;

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class MobdFrame
	{
		public readonly int OffsetX;
		public readonly int OffsetY;
		public readonly int OffsetZ;
		public readonly uint ImageVariation;
		public readonly uint RegionsList;
		public readonly uint Unk; // TODO
		public readonly uint Points;

		public MobdFrame(Stream stream)
		{
			OffsetX = stream.ReadInt32();
			OffsetY = stream.ReadInt32();
			OffsetZ = stream.ReadInt32();
			ImageVariation = OffsetUtils.ReadUInt32Offset(stream);
			RegionsList = OffsetUtils.ReadUInt32Offset(stream);
			Unk = stream.ReadUInt32();
			Points = OffsetUtils.ReadUInt32Offset(stream);
		}
	}
}
