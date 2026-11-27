using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Requires logical map borders and persistent gameplay terrain; previews legacy resource indices using current Tiberium colors.")]
	public sealed class HandcraftedMapSupportInfo : TraitInfo, IMapPreviewSignatureInfo
	{
		public override object Create(ActorInitializer init)
		{
			if (!init.World.Map.HasCellBounds || init.World.Map.BaseTerrainOverrides == null)
				throw new InvalidOperationException("Handcrafted map requires the matching logical-border engine support.");
			return new HandcraftedMapSupport();
		}

		void IMapPreviewSignatureInfo.PopulateMapPreviewSignatureCells(Map map, ActorInfo ai, ActorReference actor, List<(MPos Uv, Color Color)> cells)
		{
			foreach (var c in map.AllCells)
			{
				if (!map.Contains(c)) continue;
				var resource = map.Resources[c].Type;
				if (resource != 1 && resource != 2) continue;
				var type = resource == 1 ? "Tiberium" : "BlueTiberium";
				cells.Add((c.ToMPos(map), map.Rules.TerrainInfo.TerrainTypes[map.Rules.TerrainInfo.GetTerrainIndex(type)].Color));
			}
		}
	}

	public sealed class HandcraftedMapSupport { }
}
