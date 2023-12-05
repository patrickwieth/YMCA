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
	public class MobdRegion
	{
		const int Factor = 256;

		public readonly int Id;
		public readonly int FromX;
		public readonly int FromY;
		public readonly int ToX;
		public readonly int ToY;

		public MobdRegion(Stream stream)
		{
			Id = stream.ReadInt32();
			var fromX = stream.ReadInt32();
			var fromY = stream.ReadInt32();
			var fromZ = stream.ReadInt32();
			var toX = stream.ReadInt32();
			var toY = stream.ReadInt32();
			var toZ = stream.ReadInt32();

			if (fromZ != 0 || toZ != 0)
				throw new Exception("MOBD: 3D Regions not supported!");

			if (fromX % Factor != 0 || fromY % Factor != 0 || toX % Factor != 0 || toY % Factor != 0)
				throw new Exception("MOBD: Not aligned to tiles!");

			FromX = fromX / Factor;
			FromY = fromY / Factor;
			ToX = toX / Factor;
			ToY = toY / Factor;
		}
	}
}
