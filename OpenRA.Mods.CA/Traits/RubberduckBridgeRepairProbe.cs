using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class RubberduckBridgeRepairProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new RubberduckBridgeRepairProbe(this);
	}
	public sealed class RubberduckBridgeRepairProbe : ITick
	{
		readonly RubberduckBridgeRepairProbeInfo info;
		Actor bridge, unit; Actor[] huts;
		CPos start, end, middle; CVec step, side;
		int phase, round, began;
		public RubberduckBridgeRepairProbe(RubberduckBridgeRepairProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			if (Environment.GetEnvironmentVariable("YMCA_BRIDGE_REPAIR_PROBE") != "1" || phase == 8 || self.World.LocalPlayer == null) return;
			var w = self.World;
			if (w.WorldTick < 200) return;
			if (w.WorldTick > 8000) throw new InvalidOperationException("Bridge repair probe timed out, phase " + phase);
			w.AddFrameEndTask(world =>
			{
				Actor Spawn(string name, CPos c) => world.CreateActor(name, new TypeDictionary { new OwnerInit(world.LocalPlayer), new LocationInit(c) });
				void Order(Actor a, string name, Target target)
				{
					foreach (var resolver in a.TraitsImplementing<IResolveOrder>()) resolver.ResolveOrder(a, new Order(name, a, target, false));
				}
				if (phase == 0)
				{
					bridge = world.Actors.First(a => a.Info.HasTraitInfo<RubberduckBridgeBodyInfo>());
					var b = bridge.Info.TraitInfo<RubberduckBridgeBodyInfo>();
					step = b.AlongY ? new CVec(0, 1) : new CVec(1, 0); side = b.AlongY ? new CVec(1, 0) : new CVec(0, 1);
					start = bridge.Location; end = start + step * (b.Length - 1); middle = start + step * (b.Length / 2);
					huts = world.Actors.Where(a => a.Info.HasTraitInfo<BridgeHutInfo>() && a.Info.TraitInfo<BridgeHutInfo>().NeighbourOffsets.Any(o => a.Location + o == start || a.Location + o == end)).ToArray();
					if (huts.Length != 2) throw new InvalidOperationException("Bridge needs two correctly linked bank huts.");
					phase = 1;
				}
				else if (phase == 1)
				{
					bridge.Kill(huts[round % 2]); began = world.WorldTick; phase = 2;
				}
				else if (phase == 2 && world.WorldTick > began + 10)
				{
					if (bridge.IsInWorld || world.Map.GetTerrainInfo(middle).Type != "Water" || huts.Any(h => h.Trait<BridgeHut>().BridgeDamageState != DamageState.Dead))
						throw new InvalidOperationException("Destroyed bridge/repair placeholder state is incorrect.");
					unit = Spawn("dd", middle + side * 3);
					Order(unit, "Move", Target.FromCell(world, middle - side * 3)); phase = 3;
				}
				else if (phase == 3 && unit.Location == middle - side * 3)
				{
					unit.Dispose();
					if (round == 2)
					{
						// Two repair requests in the same frame must rebuild only once.
						foreach (var h in huts) h.Trait<BridgeHut>().Repair(h);
						phase = 4; return;
					}
					var hut = huts[round];
					var shore = hut.Info.TraitInfo<BridgeHutInfo>().NeighbourOffsets.Any(o => hut.Location + o == start) ? start - step * 2 : end + step * 2;
					unit = Spawn("engineer", shore);
					Order(unit, "RepairBridge", Target.FromActor(hut)); phase = 4;
				}
				else if (phase == 4)
				{
					var restored = world.Actors.FirstOrDefault(a => a.Info.HasTraitInfo<RubberduckBridgeBodyInfo>() && a.Location == start);
					if (restored == null) return;
					bridge = restored;
					if (world.Actors.Count(a => a.Info.HasTraitInfo<RubberduckBridgeBodyInfo>() && a.Location == start) != 1)
						throw new InvalidOperationException("Concurrent repair duplicated the bridge.");
					if (world.Map.GetTerrainInfo(middle).Type != "Clear" || bridge.Trait<IHealth>().HP != bridge.Trait<IHealth>().MaxHP)
						throw new InvalidOperationException("Repaired bridge did not restore its deck/full health.");
					unit = Spawn("mtnk", round == 0 ? start - step * 2 : end + step * 2);
					Order(unit, "Move", Target.FromCell(world, round == 0 ? end + step * 2 : start - step * 2)); phase = 5;
				}
				else if (phase == 5 && unit.Location == (round == 0 ? end + step * 2 : start - step * 2))
				{
					unit.Dispose();
					if (++round < 3) { phase = 1; return; }
					Directory.CreateDirectory(Path.GetDirectoryName(info.ResultPath));
					File.WriteAllText(info.ResultPath, "{\"Passed\":true,\"RepairsFromOppositeBanks\":2,\"ActualNavalCrossings\":3,\"ActualRepairedTankCrossings\":3,\"ConcurrentRepairSingleBridge\":true}");
					phase = 8;
				}
			});
		}
	}
}
