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
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Assets
{
	public static class OffsetUtils
	{
		public static uint ReadUInt32Offset(Stream stream)
		{
			var offset = stream.ReadUInt32();

			if (offset != 0 && offset != uint.MaxValue && stream is SegmentStream segmentStream)
				offset -= (uint)segmentStream.BaseOffset;

			return offset;
		}

#if !NET5_0_OR_GREATER
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (keySelector is null)
				throw new ArgumentNullException(nameof(keySelector));

			return DistinctByIterator(source, keySelector);
		}

		static IEnumerable<TSource> DistinctByIterator<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			using (IEnumerator<TSource> enumerator = source.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					var set = new HashSet<TKey>();
					do
					{
						TSource element = enumerator.Current;
						if (set.Add(keySelector(element)))
						{
							yield return element;
						}
					}
					while (enumerator.MoveNext());
				}
			}
		}
#endif
	}
}
