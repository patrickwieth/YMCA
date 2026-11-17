using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Activates the local Two Sides shellmap bots; optional environment-controlled battle diagnostics.")]
	public sealed class ShellmapBattleMonitorInfo : TraitInfo
	{
		public readonly string[] Players = { "GDI", "Soviets" };
		public readonly int ClearSmudgesInterval = 0;
		public override object Create(ActorInitializer init) { return new ShellmapBattleMonitor(this); }
	}

	public sealed class ShellmapBattleMonitor : IWorldLoaded, ITick, INotifyOtherProduction
	{
		readonly string report = Environment.GetEnvironmentVariable("YMCA_SHELLMAP_REPORT");
		readonly Dictionary<string, int> produced = new();
		readonly Dictionary<string, int> built = new();
		readonly HashSet<uint> observed = new();
		readonly List<LocalShellmapBot> bots = new();
		int ticks;
		int stopTicks;
		readonly ShellmapBattleMonitorInfo info;

		public ShellmapBattleMonitor(ShellmapBattleMonitorInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			// A shellmap has no client-backed players: normal network bot orders are
			// rejected by the order validator. Run the same decision modules through
			// an ownership-checked local order queue, exclusively in WorldType.Shellmap.
			if (world.Type == WorldType.Shellmap)
				foreach (var player in world.Players.Where(p => p.IsBot && info.Players.Contains(p.InternalName)))
				{
					var definition = player.PlayerActor.TraitsImplementing<IBot>().First(b => b.Info.Type == player.BotType).Info;
					var bot = new LocalShellmapBot(definition);
					bot.Activate(player);
					bots.Add(bot);
				}

			if (string.IsNullOrEmpty(report))
				return;

			int.TryParse(Environment.GetEnvironmentVariable("YMCA_SHELLMAP_TEST_TICKS"), out stopTicks);
			foreach (var actor in world.Actors)
				observed.Add(actor.ActorID);
			Directory.CreateDirectory(Path.GetDirectoryName(report));
			File.WriteAllText(report, "");
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor unit, string productionType, TypeDictionary init)
		{
			if (string.IsNullOrEmpty(report))
				return;

			var owner = unit.Owner.InternalName;
			produced.TryGetValue(owner, out var count);
			produced[owner] = count + 1;
		}

		void ITick.Tick(Actor self)
		{
			foreach (var bot in bots)
				bot.Tick();

			ticks++;
			if (self.World.Type == WorldType.Shellmap && info.ClearSmudgesInterval > 0 && ticks % info.ClearSmudgesInterval == 0)
			{
				var layers = self.TraitsImplementing<SmudgeLayer>().ToArray();
				foreach (var cell in self.World.Map.AllCells)
					if (self.World.Map.Contains(cell) && self.World.Map.GetTerrainInfo(cell).AcceptsSmudgeType.Any())
						foreach (var layer in layers)
							layer.RemoveSmudge(cell);
			}

			if (string.IsNullOrEmpty(report))
				return;
			if (ticks % 100 == 0)
			{
				foreach (var actor in self.World.Actors.Where(a => a.Info.HasTraitInfo<BuildingInfo>()))
					if (observed.Add(actor.ActorID))
					{
						var owner = actor.Owner.InternalName;
						built.TryGetValue(owner, out var count);
						built[owner] = count + 1;
					}

				var rows = self.World.Players.Where(p => info.Players.Contains(p.InternalName)).Select(p =>
				{
					var stats = p.PlayerActor.Trait<PlayerStatistics>();
					return new { player = p.InternalName, actors = self.World.Actors.Count(a => a.Owner == p),
						buildings = self.World.Actors.Count(a => a.Owner == p && a.Info.HasTraitInfo<BuildingInfo>()),
						built = built.TryGetValue(p.InternalName, out var b) ? b : 0,
						produced = produced.TryGetValue(p.InternalName, out var n) ? n : 0,
						kills = stats.UnitsKilled, deaths = stats.UnitsDead,
						units = self.World.Actors.Where(a => a.Owner == p && (a.Info.Name == "liberty.kirov" || a.Info.Name == "liberty.statue"))
							.Select(a => new { type = a.Info.Name, cell = a.Location.ToString(), position = a.CenterPosition.ToString(),
								activity = a.CurrentActivity?.GetType().Name, hp = a.TraitOrDefault<Health>()?.HP,
								arms = a.TraitsImplementing<Armament>().Select(arm => new { weapon = arm.Info.Weapon,
									disabled = arm.IsTraitDisabled, paused = arm.IsTraitPaused, range = arm.Weapon.Range.Length }).ToArray() }).ToArray() };
				}).ToArray();
				File.AppendAllText(report, JsonConvert.SerializeObject(new { ticks, players = rows }) + "\n");
			}

			if (ticks == 500 || ticks == 2500 || ticks == stopTicks - 25)
				Game.TakeScreenshot();
			if (stopTicks > 0 && ticks >= stopTicks)
				Game.Exit();
		}

		sealed class LocalShellmapBot : IBot
		{
			readonly Queue<Order> orders = new();
			IBotTick[] modules;
			int ticks;
			bool baseSaturated;
			public IBotInfo Info { get; }
			public Player Player { get; private set; }

			public LocalShellmapBot(IBotInfo info) { Info = info; }

			public void Activate(Player player)
			{
				if (player.World.Type != WorldType.Shellmap)
					throw new InvalidOperationException("Local shellmap AI must never run in a network match.");
				Player = player;
				modules = player.PlayerActor.TraitsImplementing<IBotTick>().ToArray();
				foreach (var enabled in player.PlayerActor.TraitsImplementing<IBotEnabled>())
					enabled.BotEnabled(this);
			}

			public void QueueOrder(Order order) { orders.Enqueue(order); }

			void ResolveOwnedOrder(Order order)
			{
				if (order.Subject != null && !order.Subject.IsDead && order.Subject.Owner == Player)
					order.Subject.ResolveOrder(order);
			}

			public void Tick()
			{
				if (ticks++ % 25 == 0)
					baseSaturated = Player.World.Actors.Count(a => a.Owner == Player && a.Info.HasTraitInfo<BuildingInfo>()) >= 24;

				Sync.RunUnsynced(false, Player.World, () =>
				{
					foreach (var module in modules)
						if (module.IsTraitEnabled() && !(baseSaturated && module is BaseBuilderBotModuleCA))
							module.BotTick(this);
				});
				var count = Math.Min(8, orders.Count);
				for (var i = 0; i < count; i++)
				{
					var order = orders.Dequeue();
					if (order.GroupedActors == null)
						ResolveOwnedOrder(order);
					else
						foreach (var subject in order.GroupedActors)
							ResolveOwnedOrder(Order.FromGroupedOrder(order, subject));
				}
			}
		}
	}
}
