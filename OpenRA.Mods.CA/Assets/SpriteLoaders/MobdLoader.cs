#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
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
using OpenRA.Graphics;
using OpenRA.Mods.CA.Assets.FileFormats;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Assets.SpriteLoaders
{
	public class MobdLoader : ISpriteLoader
	{
		private class MobdSpriteFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size { get; }
			public Size FrameSize { get; }
			public float2 Offset { get; }
			public byte[] Data { get; }

			public bool DisableExportPadding => true;

			public MobdSpriteFrame(MobdImageVariation imageVariation, MobdImage image, MobdFrame frame)
			{
				var pixels = image.Pixels;

				if (imageVariation.Mirrored)
				{
					var mirrored = new byte[pixels.Length];

					for (var y = 0; y < image.Height; y++)
					{
						Array.Copy(
							pixels.Skip((int)(y * image.Width)).Take((int)image.Width).Reverse().ToArray(),
							0,
							mirrored,
							y * image.Width,
							image.Width);
					}

					pixels = mirrored;
				}

				FrameSize = Size = new Size((int)image.Width, (int)image.Height);
				Offset = new int2(Size.Width / 2 - frame.OffsetX, Size.Height / 2 - frame.OffsetY);
				Data = pixels;
			}
		}

		public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			if (!filename.EndsWith(".mobd") || !(stream is Lvl.LvlStream segmentStream))
			{
				metadata = null;
				frames = null;

				return false;
			}

			// This is damn ugly, but MOBD uses offsets from LVL start.
			segmentStream.BaseStream.Position = segmentStream.BaseOffset;
			var mobd = new Mobd(segmentStream, segmentStream.AssetFormat);

			var tmp = new List<ISpriteFrame>();

			var animations = new List<MobdAnimation>();
			animations.AddRange(mobd.OrderedAnimations.Select(e => mobd.Animations[e]));
			animations.AddRange(mobd.Animations.Values.Where(e => !animations.Contains(e)));

			foreach (var animationFrames in animations
				         .Select(animation => (animation.Frames.Distinct().Count() == 1
						         ? animation.Frames.Take(1)
						         : animation.Frames)
					         .Select(f => mobd.Frames[f])))
			{
				tmp.AddRange(animationFrames.Select(frame =>
				{
					var imageVariation = mobd.ImageVariations[frame.ImageVariation];
					var image = mobd.Images[imageVariation.Image];
					return new MobdSpriteFrame(imageVariation, image, frame);
				}));
			}

			frames = tmp.Select(e => e).ToArray();

			var palette = mobd.Palettes.Values.FirstOrDefault()?.Palette;

			metadata = new TypeDictionary();

			if (palette != null)
				metadata.Add(new EmbeddedSpritePalette(palette.Select(e => (uint)e.ToArgb()).ToArray()));

			return true;
		}
	}
}
