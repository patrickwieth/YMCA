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
using OpenRA.Mods.CA.Assets.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Assets
{
	public static class Decompressor
	{
		public static byte[] DecompressContainer(Stream stream, AssetFormat assetFormat)
		{
			stream.ReadInt32(); // TODO build number (must match the kknd2.exe build number to be loaded)

			if (assetFormat == AssetFormat.Kknd2)
				stream.ReadInt32(); // TODO build time (differs on every file) - How to read?

			var dataDecompressedSize = BitConverter.ToUInt32(stream.ReadBytes(4).Reverse().ToArray());
			var dataCompressedSize = stream.ReadUInt32();

			var data = DecompressContainer(
				new SegmentStream(stream, stream.Position, dataCompressedSize),
				dataDecompressedSize);

			var rrlcDecompressedSize = stream.ReadUInt32();
			stream.ReadUInt32(); // TODO should be rrlcCompressedSize
			var rrlcCompressedSize = (uint)(stream.Length - stream.Position);

			var rrlc = DecompressContainer(
				new SegmentStream(stream, stream.Position, rrlcCompressedSize),
				rrlcDecompressedSize);

			var result = new byte[8 + data.Length + 8 + rrlc.Length];
			var writer = new BinaryWriter(new MemoryStream(result));

			writer.Write(Encoding.ASCII.GetBytes(Lvl.Data));
			writer.Write(BitConverter.GetBytes(data.Length).Reverse().ToArray());
			writer.Write(data);

			writer.Write(Encoding.ASCII.GetBytes(Lvl.ResourceRelocation));
			writer.Write(BitConverter.GetBytes(rrlc.Length).Reverse().ToArray());
			writer.Write(rrlc);

			return result;
		}

		static byte[] DecompressContainer(Stream compressed, uint decompressedSize)
		{
			var result = new byte[decompressedSize];
			var writer = new BinaryWriter(new MemoryStream(result));

			while (compressed.Position < compressed.Length)
			{
				var uncompressedSize = compressed.ReadUInt32();
				var compressedSize = compressed.ReadUInt32();

				if (compressedSize == uncompressedSize)
					writer.Write(compressed.ReadBytes((int)compressedSize));
				else
				{
					var end = compressed.Position + compressedSize;

					while (compressed.Position < end)
					{
						var bitmask = compressed.ReadUInt16();

						for (var i = 0; i < 16; i++)
						{
							if ((bitmask & (1 << i)) == 0)
								writer.Write(compressed.ReadUInt8());
							else
							{
								var metaBytes = compressed.ReadBytes(2);
								var total = 1 + (metaBytes[0] & 0x000F);
								var offset = ((metaBytes[0] & 0x00F0) << 4) | metaBytes[1];

								if (offset == 0)
									throw new Exception("Broken!");

								for (var bytesLeft = total; bytesLeft > 0;)
								{
									var amount = Math.Min(bytesLeft, offset);

									// TODO refactor to simply use Array.Copy !
									writer.BaseStream.Position -= offset;
									var data = new byte[amount];

									for (var j = amount; j > 0;)
										j -= writer.BaseStream.Read(data);

									writer.BaseStream.Position += offset - amount;
									writer.Write(data);

									bytesLeft -= offset;
								}
							}

							if (compressed.Position == end)
								break;
						}
					}
				}
			}

			return result;
		}

		public static byte[] DecompressSprt(Stream compressed, uint width, uint height)
		{
			var data = new byte[width * height];
			var decompressed = new MemoryStream(data);

			while (decompressed.Position < decompressed.Capacity)
			{
				var compressedSize = compressed.ReadUInt8() - 1;
				var lineEndOffset = compressed.Position + compressedSize;
				var isSkipMode = true;

				while (compressed.Position < lineEndOffset)
				{
					var chunkSize = compressed.ReadUInt8();

					if (isSkipMode)
						decompressed.Position += chunkSize;
					else
						decompressed.Write(compressed.ReadBytes(chunkSize));

					isSkipMode = !isSkipMode;
				}

				decompressed.Position += (width - decompressed.Position % width) % width;
			}

			return data;
		}

		public static byte[] DecompressSpnsSprc(Stream compressed, uint width, uint height, bool has256Colors)
		{
			var data = new byte[width * height];
			var decompressed = new MemoryStream(data);

			while (decompressed.Position < decompressed.Capacity)
			{
				int compressedSize = has256Colors ? compressed.ReadUInt16() : compressed.ReadUInt8();

				if (compressedSize == 0)
					decompressed.Position += width;
				else if (!has256Colors && compressedSize > 0x80)
				{
					var pixelCount = compressedSize - 0x80;

					for (var i = 0; i < pixelCount; i++)
					{
						var twoPixels = compressed.ReadUInt8();
						decompressed.WriteByte((byte)((twoPixels & 0xF0) >> 4));

						if (decompressed.Position % width != 0)
							decompressed.WriteByte((byte)(twoPixels & 0x0F));
					}
				}
				else
				{
					var lineEndOffset = compressed.Position + compressedSize;

					while (compressed.Position < lineEndOffset)
					{
						var chunkSize = compressed.ReadUInt8();

						if (chunkSize < 0x80)
							decompressed.Position += chunkSize;
						else
						{
							var pixelCount = chunkSize - 0x80;

							if (has256Colors)
								decompressed.Write(compressed.ReadBytes(pixelCount));
							else
							{
								var size = pixelCount / 2 + pixelCount % 2;

								for (var j = 0; j < size; j++)
								{
									var twoPixels = compressed.ReadUInt8();
									decompressed.WriteByte((byte)((twoPixels & 0xF0) >> 4));

									if (j + 1 < size || pixelCount % 2 == 0)
										decompressed.WriteByte((byte)(twoPixels & 0x0F));
								}
							}
						}
					}
				}

				decompressed.Position += (width - decompressed.Position % width) % width;
			}

			return data;
		}
	}
}
