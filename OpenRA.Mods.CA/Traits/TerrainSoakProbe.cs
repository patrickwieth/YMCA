using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Opt-in terrain soak instrumentation; never enabled in production rules.")]
	public class TerrainSoakProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly int DurationTicks = 15000;
		public readonly int BotCount = 0;
		public override object Create(ActorInitializer init) => new TerrainSoakProbe(this);
	}

	public class TerrainSoakProbe : IPostWorldLoaded, ITick, IRenderAboveWorld
	{
		readonly TerrainSoakProbeInfo info;
		readonly Stopwatch elapsed = new Stopwatch();
		readonly List<double> frames = new List<double>(4096);
		WorldRenderer renderer;
		long lastFrame;
		int started, samples, droppedSamples, focusedFrames;
		bool complete;

		public TerrainSoakProbe(TerrainSoakProbeInfo info) { this.info = info; }
		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			if (world.Type != WorldType.Regular || info.DurationTicks < 1000 || string.IsNullOrEmpty(info.ResultPath))
				throw new InvalidOperationException("Invalid terrain soak configuration.");
			TerrainBuildMetrics.Enable(world.Map);
			if (world.Players.Count(p => p.IsBot) != info.BotCount)
				throw new InvalidOperationException("Soak did not launch the requested real bot clients.");
			renderer = wr;
			started = world.WorldTick;
			elapsed.Start();
			PlateauMovementProbe.WriteResult(info.ResultPath, $"SOAK START bots={info.BotCount}; wall-clock world-render frame cadence, not GPU timer; no balance certification.");
			PlateauMovementProbe.WriteResult(info.ResultPath, $"SOAK CONFIG resolution={Game.Renderer.Resolution} scale={Game.Renderer.WindowScale} vsync={Game.Settings.Graphics.VSync} cap={Game.Settings.Graphics.CapFramerate} maxfps={Game.Settings.Graphics.MaxFramerate} gamefpscap={Game.Settings.Graphics.CapFramerateToGameFps}.");
		}

		void IRenderAboveWorld.RenderAboveWorld(Actor self, WorldRenderer wr)
		{
			if (complete || !elapsed.IsRunning) return;
			var now = Stopwatch.GetTimestamp();
			if (lastFrame != 0)
			{
				if (frames.Count < 16384)
				{
					frames.Add((now - lastFrame) * 1000.0 / Stopwatch.Frequency);
					if (Game.Renderer.WindowHasInputFocus) focusedFrames++;
				}
				else droppedSamples++;
			}
			lastFrame = now;
		}

		void ITick.Tick(Actor self)
		{
			if (complete || renderer == null) return;
			var ticks = self.World.WorldTick - started;
			if (ticks == 10)
			{
				// LocalPlayer is assigned after PostWorldLoaded. Never silently skip this
				// and benchmark a black unexplored screen instead of terrain.
				if (info.BotCount > 0)
				{
					if (self.World.LocalPlayer != null && !self.World.LocalPlayer.Spectating)
						throw new InvalidOperationException("Bot soak requires a true observer, not an idle human target.");
					self.World.RenderPlayer = null;
					PlateauMovementProbe.WriteResult(info.ResultPath, "SOAK OBSERVER all combat actors rendered; bot visibility rules unchanged.");
				}
				else
				{
					var player = self.World.RenderPlayer ?? self.World.LocalPlayer;
					if (player == null) throw new InvalidOperationException("Soak has no render player.");
					player.Shroud.ExploreAll();
					PlateauMovementProbe.WriteResult(info.ResultPath, "SOAK EXPLORED " + player.InternalName);
				}
			}
			if (ticks == 0 || ticks % 500 != 0) return;
			var sorted = frames.OrderBy(v => v).ToArray();
			double Percentile(double fraction) => sorted.Length == 0 ? 0 : sorted[(int)((sorted.Length - 1) * fraction)];
			using var process = Process.GetCurrentProcess();
			PlateauMovementProbe.WriteResult(info.ResultPath, string.Format(CultureInfo.InvariantCulture,
				"SOAK SAMPLE ticks={0} seconds={1:F2} actors={2} managed={3} private={4} working={5} frames={6} p50ms={7:F2} p95ms={8:F2} p99ms={9:F2} maxms={10:F2} dropped={11} gc2={12} recentRenderMs={13:F2} recentWorldMs={14:F2} recentLogicMs={15:F2} focusedFrames={16}",
				ticks, elapsed.Elapsed.TotalSeconds, self.World.Actors.Count(), GC.GetTotalMemory(false), process.PrivateMemorySize64,
				process.WorkingSet64, sorted.Length, Percentile(.50), Percentile(.95), Percentile(.99), Percentile(1), droppedSamples, GC.CollectionCount(2), OpenRA.Support.PerfHistory.Items["render"].Average(200),
				OpenRA.Support.PerfHistory.Items["render_world"].Average(200), OpenRA.Support.PerfHistory.Items["tick_time"].Average(200), focusedFrames));
			PlateauMovementProbe.WriteResult(info.ResultPath, "SOAK BUILDS " + TerrainBuildMetrics.Take(self.World.Map));
			frames.Clear(); droppedSamples = 0; focusedFrames = 0; samples++;
			var bots = self.World.Players.Where(p => p.IsBot).ToArray();
			foreach (var bot in bots)
			{
				var stats = bot.PlayerActor.Trait<PlayerStatistics>();
				PlateauMovementProbe.WriteResult(info.ResultPath, $"SOAK BOT {bot.InternalName} faction={bot.Faction.InternalName} earned={bot.PlayerActor.Trait<PlayerResources>().Earned} unitKills={stats.UnitsKilled} unitLosses={stats.UnitsDead} buildingKills={stats.BuildingsKilled} buildingLosses={stats.BuildingsDead} state={bot.WinState}");
			}
			if (ticks >= info.DurationTicks)
			{
				if (bots.Length > 0 && info.DurationTicks >= 10000 &&
					(bots.Sum(p => (long)p.PlayerActor.Trait<PlayerResources>().Earned) < 10000 ||
					bots.Sum(p => p.PlayerActor.Trait<PlayerStatistics>().UnitsKilled) < 10))
					throw new InvalidOperationException("Bot soak did not demonstrate harvesting and actual combat.");
				complete = true;
				PlateauMovementProbe.WriteResult(info.ResultPath, $"SOAK COMPLETE ticks={ticks} samples={samples}; inspect measurements before acceptance.");
				Game.TakeScreenshot();
				return;
			}

			// Cycle all nine map sectors and three zooms, including the map boundary.
			var map = self.World.Map;
			var sector = samples % 9;
			var u = map.Bounds.Left + (sector % 3 * 2 + 1) * map.Bounds.Width / 6;
			var v = map.Bounds.Top + (sector / 3 * 2 + 1) * map.Bounds.Height / 6;
			renderer.Viewport.UnlockMinimumZoom(.1f);
			var zoom = new[] { .25f, .5f, 1f }[samples / 9 % 3];
			renderer.Viewport.AdjustZoom((float)Math.Log(zoom / renderer.Viewport.Zoom));
			var cell = new MPos(u, v).ToCPos(map);
			if (!map.Contains(cell) || self.World.ShroudObscures(cell))
				throw new InvalidOperationException("Soak camera sector is unexplored.");
			renderer.Viewport.Center(map.CenterOfCell(cell));
		}
	}
}
