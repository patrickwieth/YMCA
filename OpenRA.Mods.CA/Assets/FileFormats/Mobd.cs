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

namespace OpenRA.Mods.CA.Assets.FileFormats
{
	public class Mobd
	{
		public const string Type = "MOBD";

		public readonly Dictionary<uint, MobdAnimation> Animations = new Dictionary<uint, MobdAnimation>();
		public readonly uint[] OrderedAnimations;
		public readonly Dictionary<uint, MobdFrame> Frames = new Dictionary<uint, MobdFrame>();
		public readonly Dictionary<uint, uint[]> RegionsLists = new Dictionary<uint, uint[]>();
		public readonly Dictionary<uint, MobdRegion> Regions = new Dictionary<uint, MobdRegion>();
		public readonly Dictionary<uint, MobdPoint[]> Points = new Dictionary<uint, MobdPoint[]>();

		public readonly Dictionary<uint, MobdImageVariation> ImageVariations = new Dictionary<uint, MobdImageVariation>();

		public readonly Dictionary<uint, MobdImage> Images = new Dictionary<uint, MobdImage>();
		public readonly Dictionary<uint, MobdPalette> Palettes = new Dictionary<uint, MobdPalette>();

		public Mobd(Stream stream, AssetFormat assetFormat)
		{
			var firstFrame = stream.Length;

			while (stream.Position < firstFrame)
			{
				var test = OffsetUtils.ReadUInt32Offset(stream);

				if (test != 0)
				{
					stream.Position -= 4;

					if (test < stream.Position)
						break;
				}
				else
				{
					test = OffsetUtils.ReadUInt32Offset(stream);
					stream.Position -= 8;

					if (test < stream.Position)
						break;
				}

				var position = (uint)stream.Position;
				var animation = new MobdAnimation(stream);

				if (animation.Frames.Length > 0)
					firstFrame = Math.Min(firstFrame, animation.Frames.Min());

				Animations.Add(position, animation);
			}

			var orderedAnimations = new List<uint>();

			while (stream.Position < firstFrame)
			{
				var animation = OffsetUtils.ReadUInt32Offset(stream);

				if (animation == 0)
					continue;

				if (!Animations.ContainsKey(animation))
					throw new Exception("MOBD: Rotation animation not found!");

				orderedAnimations.Add(animation);
			}

			// TODO it is possible that we have 8 or 16 directions which all point to the same animation.
			// TODO find a good procedure to detect those, and group them!
			OrderedAnimations = orderedAnimations.ToArray();

			foreach (var frame in Animations.Values.SelectMany(e => e.Frames).Distinct().OrderBy(e => e))
			{
				if (stream.Position != frame)
					throw new Exception("MOBD: Frame offset not found!");

				Frames.Add(frame, new MobdFrame(stream));
			}

			foreach (var regionsList in Frames.Values.Select(e => e.RegionsList).Distinct().OrderBy(e => e))
			{
				if (regionsList == 0)
					continue;

				if (stream.Position != regionsList)
					throw new Exception("MOBD: RegionsList offset not found!");

				var regions = new List<uint>();

				while (true)
				{
					var region = OffsetUtils.ReadUInt32Offset(stream);

					if (region == 0)
						break;

					regions.Add(region);
				}

				RegionsLists.Add(regionsList, regions.ToArray());
			}

			foreach (var regionOffset in RegionsLists.Values.SelectMany(e => e).Distinct().OrderBy(e => e))
			{
				if (stream.Position != regionOffset)
					throw new Exception("MOBD: Region offset not found!");

				Regions.Add(regionOffset, new MobdRegion(stream));
			}

			foreach (var pointsOffset in Frames.Values.Select(e => e.Points).Distinct().OrderBy(e => e))
			{
				if (pointsOffset == 0)
					continue;

				if (stream.Position != pointsOffset)
					throw new Exception("MOBD: Points offset not found!");

				var points = new List<MobdPoint>();

				while (true)
				{
					var test = stream.ReadInt32();

					if (test == -1)
						break;

					stream.Position -= 4;

					points.Add(new MobdPoint(stream));
				}

				Points.Add(pointsOffset, points.ToArray());
			}

			foreach (var imageVariation in Frames.Values.Select(e => e.ImageVariation).Distinct().OrderBy(e => e))
			{
				if (stream.Position != imageVariation)
					throw new Exception("MOBD: ImageVariation offset not found!");

				ImageVariations.Add(imageVariation, new MobdImageVariation(stream, assetFormat));
			}

			foreach (var imageVariation in ImageVariations.Values.DistinctBy(e => e.Image).OrderBy(e => e.Image))
			{
				if (stream.Position < imageVariation.Image)
					stream.Position = imageVariation.Image; // TODO fix me!
				else if (stream.Position > imageVariation.Image)
					throw new Exception("MOBD: Image offset not found!");

				Images.Add(imageVariation.Image, new MobdImage(stream, assetFormat, imageVariation));
			}

			foreach (var palette in ImageVariations.Values.Select(e => e.Palette).Distinct().OrderBy(e => e))
			{
				if (palette == null)
					continue;

				if (stream.Position < palette.Value)
					stream.Position = palette.Value; // TODO fix me!
				else if (stream.Position > palette.Value)
					throw new Exception("MOBD: Palette offset not found!");

				Palettes.Add(palette.Value, new MobdPalette(stream));
			}

			if (stream.Position < stream.Length)
				throw new Exception("MOBD: Missing data!");
		}
	}
}
