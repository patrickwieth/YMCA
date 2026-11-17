using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Opt-in generated-map diagnostic: real autonomous units traverse the objective chain from both teams.")]
	public class StrategicFrontProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public override object Create(ActorInitializer init) => new StrategicFrontProbe(this);
	}

	public class StrategicFrontProbe : ITick
	{
		readonly StrategicFrontProbeInfo info;
		readonly List<(Actor Unit, int[] Order)> units = new();
		readonly Dictionary<Actor, int> visited = new();
		Actor[] checkpoints;
		int ticks, phase = 1;
		bool pending, complete;
		public StrategicFrontProbe(StrategicFrontProbeInfo info) { this.info = info; }
		void ITick.Tick(Actor self)
		{
			if (complete) return;
			var world = self.World;
			if (++ticks > 12000) throw new InvalidOperationException("Strategic autonomous front traversal timed out.");
			if (ticks < 30 || pending) return;
			if (checkpoints == null)
			{
				checkpoints = world.ActorsHavingTrait<Checkpoint>().OrderBy(a => a.Trait<Checkpoint>().Hierarchy).ToArray();
				if (checkpoints.Length < 3) throw new InvalidOperationException("Missing Strategic objective chain.");
				var expectedPlayers = new MapPlayers(world.Map.PlayerDefinitions);
				foreach (var player in world.Players.Where(p => p.PlayerReference.Playable))
				{
					var client = world.LobbyInfo.ClientWithIndex(player.ClientIndex);
					var expected = expectedPlayers.Players[player.InternalName];
					if (client == null || client.Team != expected.Team || client.SpawnPoint != expected.Spawn)
						throw new InvalidOperationException("Lobby did not honor locked Strategic team/spawn.");
					Log($"STRATEGIC SLOT PASS {player.InternalName} team={client.Team} spawn={client.SpawnPoint} home={player.HomeLocation}");
				}
			}
			if (units.Count == 0)
			{
				pending = true;
				world.AddFrameEndTask(w =>
				{
					foreach (var player in w.Players.Where(p => p.PlayerReference.Playable && p.PlayerReference.Team == phase))
					{
						var unit = w.CreateActor("challenger_tank", new TypeDictionary { new OwnerInit(player), new LocationInit(player.HomeLocation) });
						var order = Enumerable.Range(0, checkpoints.Length).ToArray();
						if (phase == 2) Array.Reverse(order);
						if (unit.Trait<Autobattler>().Hierarchy != order[0]) throw new InvalidOperationException("Unit chose the wrong front entry.");
						units.Add((unit, order)); visited[unit] = 0;
					}
					if (units.Count == 0) throw new InvalidOperationException("Missing diagnostic team.");
					pending = false;
				});
				return;
			}
			foreach (var test in units)
			{
				if (test.Unit.IsDead) throw new InvalidOperationException("Front diagnostic unit died.");
				var index = visited[test.Unit];
				if (index == test.Order.Length) continue;
				var target = checkpoints[test.Order[index]];
				var hierarchy = test.Unit.Trait<Autobattler>().Hierarchy;
				var reached = index < test.Order.Length - 1 ? hierarchy == test.Order[index + 1] :
					(test.Unit.CenterPosition - target.CenterPosition).HorizontalLength < 3 * 1024;
				if (!reached) continue;
				Log($"STRATEGIC ROUTE PASS {test.Unit.Owner.InternalName} checkpoint={test.Order[index]} tick={ticks}");
				visited[test.Unit]++;
			}
			if (units.Any(t => visited[t.Unit] < t.Order.Length)) return;
			if (phase == 2)
			{
				Log($"STRATEGIC FRONT COMPLETE checkpoints={checkpoints.Length}; both teams, all occupied homes, autonomous routing.");
				complete = true; Game.TakeScreenshot(); return;
			}
			pending = true;
			world.AddFrameEndTask(w =>
			{
				foreach (var test in units) test.Unit.Dispose();
				units.Clear(); visited.Clear(); phase = 2; pending = false;
			});
		}
		void Log(string text) => PlateauMovementProbe.WriteResult(info.ResultPath, text);
	}
}
