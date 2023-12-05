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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class Lvl : IReadOnlyPackage
	{
		public class LvlStream : SegmentStream
		{
			public readonly AssetFormat AssetFormat;
			public readonly string Container;

			public LvlStream(AssetFormat assetFormat, string container, Stream stream, int offset, int length)
				: base(stream, offset, length)
			{
				AssetFormat = assetFormat;
				Container = container;
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(false);
			}
		}

		public const string Data = "DATA";
		public const string ResourceRelocation = "RRLC";

		readonly Stream stream;
		readonly AssetFormat assetFormat;
		readonly Dictionary<string, uint[]> index = new Dictionary<string, uint[]>();

		public string Name { get; }
		public IEnumerable<string> Contents => index.Keys;

		public Lvl(Stream stream, string name, AssetFormat assetFormat)
		{
			this.assetFormat = assetFormat;
			Name = name;

			if (assetFormat == AssetFormat.Kknd2)
				stream = new MemoryStream(Decompressor.DecompressContainer(stream, assetFormat));

			var type = Encoding.ASCII.GetString(stream.ReadBytes(4));
			var length = BitConverter.ToUInt32(stream.ReadBytes(4).Reverse().ToArray());

			if (type != Data)
				throw new Exception($"LVL: {Data} missing");

			this.stream = new SegmentStream(stream, stream.Position, length);

			var typesListOffset = this.stream.ReadUInt32();
			var typesCount = (this.stream.Length - typesListOffset) / 8;
			var firstFileListStart = typesListOffset;

			for (var i = 0; i < typesCount; i++)
			{
				this.stream.Position = typesListOffset + i * 8;

				type = Encoding.ASCII.GetString(this.stream.ReadBytes(4));
				var filesListStartOffset = this.stream.ReadUInt32();
				var filesListEndOffset = typesListOffset;

				if (i == 0)
					firstFileListStart = filesListStartOffset;

				if (i + 1 == typesCount)
				{
					if (type != "\0\0\0\0" || filesListStartOffset != 0)
						throw new Exception($"LVL: error parsing {Data}");

					break;
				}

				if (i + 2 < typesCount)
				{
					this.stream.Position += 4;
					filesListEndOffset = this.stream.ReadUInt32();
				}

				this.stream.Position = filesListStartOffset;

				for (var j = 0; this.stream.Position < filesListEndOffset; j++)
				{
					var fileStart = this.stream.ReadUInt32();

					var extension = type.ToLower();

					if (extension == "soun")
						extension = "wav";

					if (fileStart != 0)
						index.Add($"{j}.{extension}", new[] { fileStart, 0u });
				}
			}

			var fileOrder = index.Values.Select(offsets => offsets[0])
				.OrderBy(fileStart => fileStart).ToArray();

			foreach (var offsets in index.Values)
			{
				var index = fileOrder.IndexOf(offsets[0]);

				if (index != -1)
					offsets[1] = index + 1 == fileOrder.Length ? firstFileListStart : fileOrder[index + 1];
			}

			if (this.stream.Position < this.stream.Length)
				throw new Exception($"LVL: Unparsed data at end of {Data}");
		}

		public bool Contains(string filename)
		{
			return index.ContainsKey(filename);
		}

		public Stream GetStream(string filename)
		{
			return !index.TryGetValue(filename, out var entry)
				? null
				: new LvlStream(assetFormat, Name, stream, (int)entry[0], (int)(entry[1] - entry[0]));
		}

		public IReadOnlyPackage OpenPackage(string filename, OpenRA.FileSystem.FileSystem context)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
