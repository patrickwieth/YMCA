using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Sinks dry-land husks left by traffic killed on a collapsed Rubberduck bridge.")]
	public class RubberduckBridgeCollapseInfo : TraitInfo<RubberduckBridgeCollapse>, Requires<BuildingInfo> { }

	public class RubberduckBridgeCollapse : INotifyRemovedFromWorld
	{
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			var cells = self.Info.TraitInfo<BuildingInfo>().PathableTiles(self.Location).ToArray();
			// GroundLevelBridge restores the underlying water and kills invalid
			// traffic. Its death-spawn traits create husks at frame end. Give those
			// tasks time to finish, then sink only husks forbidden on restored water.
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(2, () => w.AddFrameEndTask(world =>
			{
				foreach (var cell in cells)
				{
					if (world.Map.GetTerrainInfo(cell).Type != "Water") continue;
					foreach (var actor in world.ActorMap.GetActorsAt(cell).ToArray())
					{
						var husk = actor.TraitOrDefault<Husk>();
						if (husk != null && !husk.CanExistInCell(cell)) actor.Dispose();
					}
				}
			}))));
		}
	}
}
