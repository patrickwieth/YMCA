using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.CA.UtilityCommands;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated local refinery traffic diagnostic; not enabled in normal rules.")]
	public sealed class RefineryDockingProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new RefineryDockingProbe(this);
	}
	public sealed class RefineryDockingProbe : ITick, ITickRender
	{
		readonly RefineryDockingProbeInfo info;
		sealed class Trip { public Actor Actor; public CPos Park; public string Label; public int Stage; }
		readonly List<Trip> trips = new();
		int ticks, expected, unloaded, departed;
		bool completed;
		public RefineryDockingProbe(RefineryDockingProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_REFINERY_SMUDGE_PROBE") != "1")
				throw new InvalidOperationException("Refinery probe requires an explicitly opted-in local harness.");
			this.info = info;
		}
		void Log(string line) => File.AppendAllText(info.ResultPath, line + "\n");
		void ITick.Tick(Actor self)
		{
			var w = self.World; ticks++;
			if (ticks == 10)
				w.AddFrameEndTask(_ =>
				{
					for (var i = 0; i < CheckRefineryDockingCommand.Refineries.Length; i++)
					{
						var b = w.CreateActor(CheckRefineryDockingCommand.Refineries[i], new TypeDictionary
						{
							new OwnerInit(w.LocalPlayer), new LocationInit(new CPos(88 + i % 3 * 18, -45 + i / 3 * 18)), new SkipMakeAnimsInit()
						});
						if (b.Info.Name == "chsupply") b.GrantCondition("local-uplandmines");
					}
				});
			if (ticks == 25)
				w.AddFrameEndTask(_ =>
				{
					// Verify real free spawns, then remove only fixture harvesters to make room for the controlled traffic wave.
					foreach (var a in w.Actors.Where(a => a.TraitOrDefault<Harvester>() != null).ToArray())
					{
						if (!a.Trait<Mobile>().CanEnterCell(a.Location, ignoreActor: a)) throw new InvalidOperationException("Blocked free spawn: " + a.Info.Name + " at " + a.Location + " terrain=" + w.Map.GetTerrainInfo(a.Location).Type + " actors=" + string.Join(",", w.ActorMap.GetActorsAt(a.Location).Select(x => x.Info.Name)));
						Log("SPAWN " + a.Info.Name + " " + a.Location); a.Dispose();
					}
				});
			if (ticks == 40)
				w.AddFrameEndTask(_ =>
				{
					var buildings = w.Actors.Where(a => CheckRefineryDockingCommand.Refineries.Contains(a.Info.Name)).ToArray();
					foreach (var b in buildings)
					{
						var free = b.Info.TraitInfos<FreeActorInfo>().First(s => w.Map.Rules.Actors[s.Actor.ToLowerInvariant()].HasTraitInfo<HarvesterInfo>());
						var docks = w.Actors.Where(a => a == b || a.Info.Name == "refinery.dockhelper" &&
							Math.Abs(a.Location.X - b.Location.X) <= 3 && Math.Abs(a.Location.Y - b.Location.Y) <= 3).ToArray();
						for (var d = 0; d < docks.Length; d++)
							for (var j = 0; j < 3; j++)
							{
								var host = docks[d]; var dock = host.Trait<DockHost>();
								var park = b.Location + new CVec(5 + j * 2, 2 + d * 2);
								var a = w.CreateActor(free.Actor.ToLowerInvariant(), new TypeDictionary { new OwnerInit(w.LocalPlayer), new LocationInit(park) });
								var h = a.Trait<Harvester>();
								for (var load = 0; load < 10; load++) h.AddResource(a, "Tiberium");
								var label = b.Info.Name + "/" + host.Info.Name + "@" + host.Location + "/" + j;
								if (!a.Trait<Mobile>().CanEnterCell(w.Map.CellContaining(dock.DockPosition))) throw new InvalidOperationException("Blocked dock " + label);
								expected++;
								a.QueueActivity(new MoveToDock(a, host, dock));
								trips.Add(new Trip { Actor = a, Park = park, Label = label });
							}
					}
					Log("QUEUED " + expected);
				});
			// Harvester.OnDockCompleted intentionally replaces queued activities with its
			// normal harvesting loop. Observe actual unloading/unreservation externally.
			foreach (var trip in trips)
			{
				var a = trip.Actor;
				if (trip.Stage == 0 && a.Trait<Harvester>().IsEmpty && a.Trait<DockClientManager>().ReservedHost == null)
				{
					trip.Stage = 1; unloaded++; Log("UNLOAD " + trip.Label);
					a.QueueActivity(false, a.Trait<Mobile>().MoveTo(trip.Park));
				}
				else if (trip.Stage == 1 && a.Location == trip.Park)
				{
					trip.Stage = 2; departed++; Log("EXIT " + trip.Label);
					w.AddFrameEndTask(__ => a.Dispose());
				}
			}
			if (!completed && expected > 0 && departed == expected)
			{
				completed = true; Log("PASS: " + unloaded + " deliveries and " + departed + " departures; three harvesters per real main/auxiliary dock.");
			}
			if (ticks % 1000 == 0 && !completed)
				foreach (var trip in trips.Where(t => t.Stage != 2)) Log("PENDING " + trip.Label + " phase=" + trip.Stage + " at=" + trip.Actor.Location + " goal=" + trip.Park + " activity=" + trip.Actor.CurrentActivity?.GetType().Name);
			if (ticks == 6000 && !completed) throw new InvalidOperationException("Traffic stalled: " + unloaded + "/" + expected + " unloaded, " + departed + " departed.");
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (ticks < 50) wr.Viewport.Center(self.World.Map.CenterOfCell(new CPos(110, -32)));
			if (ticks == 150 || ticks == 500) Game.TakeScreenshot();
		}
	}
}
