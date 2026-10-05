using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Checks that diagnostic height-combat sources do not leave permanent visibility after removal.")]
	public class HeightVisionLifecycleProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new HeightVisionLifecycleProbe(this);
	}

	public class HeightVisionLifecycleProbe : ITick
	{
		readonly HeightVisionLifecycleProbeInfo info;
		Player owner;
		Actor[] targets;
		public HeightVisionLifecycleProbe(HeightVisionLifecycleProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			if (self.World.WorldTick == 450)
			{
				var sources = self.World.Actors.Where(a => a.TraitOrDefault<HeightCombatProbe>() != null).ToArray();
				if (sources.Length != 16) throw new InvalidOperationException("Missing vision lifecycle sources.");
				owner = sources[0].Owner;
				targets = self.World.Actors.Where(a => a.Info.Name.StartsWith("calibration.height-target-", StringComparison.Ordinal)).ToArray();
				if (targets.Length != 16) throw new InvalidOperationException("Missing vision lifecycle targets.");
				self.World.AddFrameEndTask(_ => { foreach (var source in sources) source.Dispose(); });
			}
			if (self.World.WorldTick != 500) return;
			if (targets.Any(a => owner.Shroud.IsVisible(a.Location))) throw new InvalidOperationException("Removed height source left a visible target.");
			PlateauMovementProbe.WriteResult(info.ResultPath, "HEIGHT SIGHT LIFECYCLE PASS: all sixteen targets fogged after source removal.");
			Game.TakeScreenshot();
		}
	}
}
