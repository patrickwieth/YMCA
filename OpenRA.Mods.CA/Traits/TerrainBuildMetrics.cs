using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.CA.Traits
{
	// Opt-in, map-scoped cold-build measurements. No production logging or map retention.
	static class TerrainBuildMetrics
	{
		sealed class Counters
		{
			public readonly Dictionary<string, (int Count, long Total, long Max)> Values = new();
		}
		static readonly ConditionalWeakTable<Map, Counters> Maps = new();
		public static void Enable(Map map) => Maps.GetValue(map, _ => new Counters());
		public static T Measure<T>(Map map, string name, Func<T> build)
		{
			if (!Maps.TryGetValue(map, out var counters)) return build();
			var start = Stopwatch.GetTimestamp();
			try { return build(); }
			finally
			{
				var duration = Stopwatch.GetTimestamp() - start;
				counters.Values.TryGetValue(name, out var previous);
				counters.Values[name] = (previous.Count + 1, previous.Total + duration, Math.Max(previous.Max, duration));
			}
		}
		public static void Count(Map map, string name, int count)
		{
			if (!Maps.TryGetValue(map, out var counters)) return;
			counters.Values.TryGetValue(name, out var previous);
			counters.Values[name] = (previous.Count + count, previous.Total, previous.Max);
		}

		public static string Take(Map map)
		{
			if (!Maps.TryGetValue(map, out var counters)) return "";
			var result = string.Join("; ", counters.Values.OrderBy(p => p.Key).Select(p => string.Format(CultureInfo.InvariantCulture,
				"{0}: count={1} totalMs={2:F2} maxMs={3:F2}", p.Key, p.Value.Count,
				p.Value.Total * 1000.0 / Stopwatch.Frequency, p.Value.Max * 1000.0 / Stopwatch.Frequency)));
			counters.Values.Clear();
			return result;
		}
	}
}
