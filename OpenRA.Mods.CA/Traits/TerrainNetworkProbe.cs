using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Explicit loopback multiplayer test marker and network-order/sync audit. Diagnostic maps only.")]
	public class TerrainNetworkProbeInfo : TraitInfo
	{
		public readonly int Ticks = 3000;
		public override object Create(ActorInitializer init) => new TerrainNetworkProbe(this);
	}

	public class TerrainNetworkProbe : ITick
	{
		readonly TerrainNetworkProbeInfo info;
		string path;
		bool complete;
		int sent;
		CPos[] initial;
		Actor[] tanks;
		public TerrainNetworkProbe(TerrainNetworkProbeInfo info) { this.info = info; }
		void Record(string message) => File.AppendAllText(path, message + Environment.NewLine);
		void ITick.Tick(Actor self)
		{
			if (complete) return;
			var world = self.World;
			var tick = world.WorldTick;
			if (tick < 20) return;
			if (tick == 20)
			{
				if (world.IsReplay || world.LocalPlayer == null || world.LobbyInfo.Clients.Count(c => c.Bot == null && !c.IsObserver) != 2)
					throw new InvalidOperationException("Network test needs two live human clients.");
				path = Path.Combine(Platform.SupportDir, "Logs", "terrain-network.log");
				Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, "");
				tanks = world.Actors.Where(a => a.Info.Name == "calibration.networktank").OrderBy(a => a.Owner.InternalName).ToArray();
				if (tanks.Length != 2) throw new InvalidOperationException("Missing multiplayer test tanks.");
				initial = tanks.Select(a => a.Location).ToArray();
				Record($"NETWORK START local={world.LocalPlayer.InternalName} map={world.Map.Uid} clients=2");
			}
			if (tick >= 100 && tick % 150 == 100 && tick < info.Ticks - 200)
			{
				foreach (var tank in tanks.Where(a => a.Owner == world.LocalPlayer))
				{
					var index = Array.IndexOf(tanks, tank);
					var destination = initial[index] + (tick / 150 % 2 == 0 ? new CVec(0, 3) : CVec.Zero);
					// Only the owning client issues this order. Delivery to the other
					// simulation goes through the real server/order manager.
					world.IssueOrder(new Order("Move", tank, Target.FromCell(world, destination), false));
					sent++;
				}
			}
			if (tick % 100 == 0)
				Record($"NETWORK HASH tick={tick} hash={world.SyncHash()} cells={string.Join(";", tanks.Select(a => a.Location))}");
			if (tick == info.Ticks)
			{
				if (sent < 10) throw new InvalidOperationException("Insufficient multiplayer movement orders.");
				Record($"NETWORK COMPLETE ticks={tick} sent={sent}; compare both clients' hashes and actual positions.");
				Game.TakeScreenshot(); complete = true;
			}
		}
	}
}
