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
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.Assets.FileFormats;

namespace OpenRA.Mods.CA.Assets.FileSystem
{
	public class LvlPackageLoader : IPackageLoader
	{
		public bool TryParsePackage(Stream s, string filename, OpenRA.FileSystem.FileSystem fs, out IReadOnlyPackage package)
		{
			if (filename.EndsWith(".lvl", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".slv", StringComparison.OrdinalIgnoreCase))
			{
				var test = Encoding.ASCII.GetString(s.ReadBytes(4));
				s.Position -= 4;

				if (test == "DATA")
				{
					package = new Lvl(s, filename, AssetFormat.Kknd1);

					return true;
				}
			}

			if (filename.EndsWith(".lpk", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".bpk", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".spk", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".lps", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".lpm", StringComparison.OrdinalIgnoreCase) ||
			    filename.EndsWith(".mpk", StringComparison.OrdinalIgnoreCase))
			{
				package = new Lvl(s, filename, AssetFormat.Kknd2);

				return true;
			}

			package = null;

			return false;
		}
	}
}
