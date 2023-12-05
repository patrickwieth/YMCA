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

using System.Collections.Generic;
using System.IO;

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class MobdAnimation
	{
		public readonly uint Unk1; // TODO
		public readonly uint[] Frames;
		public readonly uint Unk2; // TODO

		public MobdAnimation(Stream stream)
		{
			var frames = new List<uint>();

			Unk1 = stream.ReadUInt32();

			while (true)
			{
				var test = OffsetUtils.ReadUInt32Offset(stream);

				if (test == 0 || test == uint.MaxValue)
				{
					Unk2 = test;
					break;
				}

				frames.Add(test);
			}

			Frames = frames.ToArray();
		}
	}
}
