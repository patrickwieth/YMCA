using System.Runtime.CompilerServices;

namespace OpenRA.Mods.CA.Traits
{
	// One pair of subscriptions per map, not one per cliff actor. Weak map keys let
	// editor sessions and utility maps be collected without a global map reference.
	static class TerrainRenderRevision
	{
		sealed class Revision
		{
			public long Value;
			public Revision(Map map)
			{
				map.Height.CellEntryChanged += _ => Value++;
				map.Tiles.CellEntryChanged += _ => Value++;
			}
		}
		static readonly ConditionalWeakTable<Map, Revision> Revisions = new();
		public static long Get(Map map) => Revisions.GetValue(map, m => new Revision(m)).Value;
	}
}
