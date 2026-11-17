using System.Collections.Generic;

namespace OpenRA.Mods.CA
{
	// Same quaternary min-heap and equal-priority ordering as .NET6 PriorityQueue.
	// Use on BOTH runtimes: path selection must not differ between Mono and .NET.
	internal sealed class PortablePriorityQueue<TElement, TPriority>
	{
		readonly List<(TElement Element, TPriority Priority)> nodes = new();
		readonly Comparer<TPriority> comparer = Comparer<TPriority>.Default;

		public void Enqueue(TElement element, TPriority priority)
		{
			var index = nodes.Count;
			nodes.Add(default);
			while (index > 0)
			{
				var parent = (index - 1) >> 2;
				if (comparer.Compare(priority, nodes[parent].Priority) >= 0) break;
				nodes[index] = nodes[parent];
				index = parent;
			}
			nodes[index] = (element, priority);
		}

		public bool TryDequeue(out TElement element, out TPriority priority)
		{
			if (nodes.Count == 0)
			{
				element = default;
				priority = default;
				return false;
			}

			(element, priority) = nodes[0];
			var last = nodes[nodes.Count - 1];
			nodes.RemoveAt(nodes.Count - 1);
			if (nodes.Count == 0) return true;
			var index = 0;
			while ((index * 4) + 1 < nodes.Count)
			{
				var child = (index * 4) + 1;
				var end = System.Math.Min(child + 4, nodes.Count);
				var best = child;
				for (var i = child + 1; i < end; i++)
					if (comparer.Compare(nodes[i].Priority, nodes[best].Priority) < 0) best = i;
				if (comparer.Compare(last.Priority, nodes[best].Priority) <= 0) break;
				nodes[index] = nodes[best];
				index = best;
			}
			nodes[index] = last;
			return true;
		}
	}
}
