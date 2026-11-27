using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public sealed class CliffShotProbeInfo : TraitInfo
	{
		public override object Create(ActorInitializer init)
		{
			if (Environment.GetEnvironmentVariable("YMCA_CLIFF_FIRE_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
			return new CliffShotProbe();
		}
	}
	public sealed class CliffShotProbe : INotifyAttack
	{
		public int Shots { get; private set; }
		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament arm, Barrel barrel) { }
		void INotifyAttack.Attacking(Actor self, in Target target, Armament arm, Barrel barrel) { Shots++; }
	}

	public sealed class CliffFireProbeInfo : TraitInfo
	{
		public readonly string ResultPath = "";
		public readonly bool Baseline;
		public override object Create(ActorInitializer init) => new CliffFireProbe(this);
	}
	public sealed class CliffFireProbe : ITick, ITickRender
	{
		readonly CliffFireProbeInfo info;
		Actor[] units, targets;
		Health[] health;
		int[] initial;
		CPos[] starts;
		bool defendHeld, forcedFired, stanceStopped;
		int stanceSwitchTick;
		CPos stoppedPosition;
		Actor edgeLow, edgeHigh;
		Health edgeLowHealth, edgeHighHealth;
		int edgeLowInitial, edgeHighInitial;
		public CliffFireProbe(CliffFireProbeInfo info)
		{
			if (Environment.GetEnvironmentVariable("YMCA_CLIFF_FIRE_PROBE") != "1") throw new InvalidOperationException("Diagnostic opt-in required.");
			this.info = info;
		}
		void ITick.Tick(Actor self)
		{
			var world = self.World; var tick = world.WorldTick;
			if (tick == 1)
			{
				starts = new[] { new CPos(90, 0), new CPos(107, 0), new CPos(127, 0), new CPos(150, 0), new CPos(167, 0), new CPos(107, 20) };
				if (world.Actors.Any(a => a.Info.Name is "mtnk" or "mercenary" or "cliff.probe.rocket"))
					foreach (var i in new[] { 1, 2, 4, 5 }) starts[i] += new CVec(1, 0);
				var ends = new[] { new CPos(84, 0), new CPos(113, 0), new CPos(133, 0), new CPos(154, 0), new CPos(173, 0), new CPos(113, 20) };
				if (world.Actors.Any(a => a.Info.Name is "mtnk" or "mercenary" or "cliff.probe.rocket"))
					foreach (var i in new[] { 1, 2, 4, 5 }) ends[i] -= new CVec(1, 0);
				if (world.Actors.Any(a => a.Info.Name == "ssm.multi"))
				{ starts[5] += new CVec(0, 20); ends[5] += new CVec(0, 20); }
				units = starts.Select(p => world.Actors.Single(a => (a.Info.Name == "channeler" || a.Info.Name == "mtnk" || a.Info.Name == "mercenary" || a.Info.Name == "ssm.multi" || a.Info.Name.StartsWith("cliff.probe.", StringComparison.Ordinal)) && a.Location == p)).ToArray();
				targets = ends.Select(p => world.Actors.Single(a => a.Info.Name == "mcv" && a.Location == p)).ToArray();
				foreach (var i in new[] { 1, 2, 4, 5 })
					if (!units[i].TraitsImplementing<Armament>().Any(a => Target.FromActor(targets[i]).IsInRange(units[i].CenterPosition, a.MaxRange())))
						throw new InvalidOperationException("Stance fixture target must already be within weapon range.");
				health = targets.Select(a => a.Trait<Health>()).ToArray();
				initial = health.Select(h => h.HP).ToArray();
				edgeLow = world.Actors.Single(a => (a.Info.Name == "channeler" || a.Info.Name == "mtnk") && (a.Location == new CPos(161, 20) || a.Location == new CPos(164, 20)));
				edgeHigh = world.Actors.Single(a => (a.Info.Name == "channeler" || a.Info.Name == "mtnk") && a.Location == new CPos(167, 20));
				edgeLowHealth = edgeLow.Trait<Health>(); edgeHighHealth = edgeHigh.Trait<Health>();
				edgeLowInitial = edgeLowHealth.HP; edgeHighInitial = edgeHighHealth.HP;
			}
			if (units == null) return;
			if (!info.Baseline && stanceSwitchTick == 0 && units[2].CurrentActivity?.ChildActivity?.GetType().Name == "Move")
			{
				stanceSwitchTick = tick;
				units[2].Trait<AutoTarget>().SetStance(units[2], UnitStance.Defend);
			}
			if (stanceSwitchTick > 0 && tick == stanceSwitchTick + 50) stoppedPosition = units[2].Location;
			if (stanceSwitchTick > 0 && tick == stanceSwitchTick + 100)
			{
				stanceStopped = units[2].Location == stoppedPosition;
				if (!stanceStopped) throw new InvalidOperationException("Defend did not cancel automatic terrain repositioning.");
				units[2].Trait<AutoTarget>().SetStance(units[2], UnitStance.AttackAnything);
			}
			if (tick == 30)
			{
				// Force-ground fire bypasses actor firing-position gating but still
				// collides physically. This verifies real emitted projectiles.
				foreach (var attack in units[0].TraitsImplementing<AttackBase>())
					attack.AttackTarget(Target.FromPos(targets[0].CenterPosition), AttackSource.Default, false, true, true);
				units[5].Trait<Health>().InflictDamage(units[5], targets[5], new Damage(1), true);
			}
			if (tick > 30 && units[0].Trait<CliffShotProbe>().Shots > 0) forcedFired = true;
			if (tick == 300)
			{
				defendHeld = units[1].Location == starts[1] && health[1].HP == initial[1];
				if (!info.Baseline && (!defendHeld || health[0].HP != initial[0]))
					throw new InvalidOperationException("Blocked energy fire or defensive stance failed.");
				// A player-issued attack may move even though the automatic stance is Defend.
				foreach (var attack in units[1].TraitsImplementing<AttackBase>())
					attack.AttackTarget(Target.FromActor(targets[1]), AttackSource.Default, false, true);
			}
			if (tick == 1200)
			{
				var damage = health.Select((h, i) => initial[i] - h.HP).ToArray();
				var moved = units.Select((a, i) => a.Location != starts[i]).ToArray();
				File.WriteAllText(info.ResultPath + ".json", JsonConvert.SerializeObject(new { info.Baseline, defendHeld, forcedFired, stanceStopped, stanceSwitchTick, damage, moved, edgeLowDamage = edgeLowInitial - edgeLowHealth.HP, edgeHighDamage = edgeHighInitial - edgeHighHealth.HP }, Formatting.Indented));
				if (info.Baseline && (!forcedFired || damage[0] <= 0))
					throw new InvalidOperationException("Baseline did not exercise actual projectile damage.");
				if (!info.Baseline && (damage[0] != 0 || !defendHeld || damage[1] <= 0 || !moved[1] || damage[2] <= 0 || !moved[2] || damage[3] != 0 || moved[3] || damage[4] != 0 || moved[4] || damage[5] != 0 || moved[5] || !forcedFired || !stanceStopped || edgeLowHealth.HP >= edgeLowInitial || edgeHighHealth.HP >= edgeHighInitial))
					throw new InvalidOperationException("Cliff firing-position acceptance failed: " + File.ReadAllText(info.ResultPath + ".json"));
				var far = world.Actors.Single(a => a.Info.Name == "mcv" && a.Location == new CPos(175, 30));
				foreach (var attack in units[4].TraitsImplementing<AttackBase>())
					attack.AttackTarget(Target.FromActor(far), AttackSource.Default, false, true);
			}
			if (tick == 1500)
			{
				var farOrderMoved = units[4].Location != starts[4];
				if (!farOrderMoved) throw new InvalidOperationException("A distant explicit attack failed to approach its target.");
				var result = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(info.ResultPath + ".json"));
				result["farOrderMoved"] = farOrderMoved;
				File.WriteAllText(info.ResultPath + ".json", result.ToString());
				File.AppendAllText(info.ResultPath, "PASS: " + (info.Baseline ? "baseline recorded" : "energy collision, all stances, stance change, explicit near/far attacks, edge exchange, unreachable holds") + "\n");
			}
		}
		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var tick = self.World.WorldTick;
			wr.Viewport.Center(self.World.Map.CenterOfCell(tick < 200 ? new CPos(90, 0) : tick < 900 ? new CPos(110, 0) : new CPos(130, 0)));
			if (tick == 35 || tick == 45 || tick == 55 || tick == 320 || tick == 1150) Game.TakeScreenshot();
		}
	}
}
