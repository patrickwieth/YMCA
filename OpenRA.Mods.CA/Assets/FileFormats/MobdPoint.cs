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
	public class MobdPoint
	{
		const int Factor = 256;

		public readonly int Id;
		public readonly int X;
		public readonly int Y;

		public MobdPoint(Stream stream)
		{
			Id = stream.ReadInt32();
			var x = stream.ReadInt32();
			var y = stream.ReadInt32();
			var z = stream.ReadInt32();

			if (z != 0)
				throw new Exception("MOBD: 3D Points not supported!");

			if (x % Factor != 0 || y % Factor != 0)
				throw new Exception("MOBD: Not aligned to tiles!");

			X = x / Factor;
			Y = y / Factor;
		}
	}
}
