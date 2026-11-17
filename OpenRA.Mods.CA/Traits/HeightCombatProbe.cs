using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Isolated real-weapon height fixture assertion.")]
	public class HeightCombatProbeInfo : TraitInfo
	{
		public readonly string TargetType = "";
		public readonly string ResultPath = "";
		public readonly bool ExpectDamage = true;
		public readonly bool CheckVisibility = false;
		public readonly CPos BarrierCenter = CPos.Zero;
		public readonly CVec BarrierTangent = new CVec(0, 1);
		public override object Create(ActorInitializer init) => new HeightCombatProbe(this);
	}

	public class HeightCombatProbe : ITick, INotifyAttack, INotifyAppliedDamage
	{
		readonly HeightCombatProbeInfo info;
		Actor target;
		Health health;
		int ticks, initialHP, shots, weaponDamage, impacts, escapedImpacts;
		CPos start;
		WPos sourcePosition;
		public HeightCombatProbe(HeightCombatProbeInfo info) { this.info = info; }
		void INotifyAttack.Attacking(Actor self, in Target target, Armament armament, Barrel barrel) { shots++; }
		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament armament, Barrel barrel) { }
		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			if (damaged == target && e.Damage.Value > 0) weaponDamage += e.Damage.Value;
		}
		public void RecordImpact(Actor source, WPos position)
		{
			if (target == null) return;
			impacts++;
			if (impacts == 1)
			{
				var cell = source.World.Map.CellContaining(position);
				PlateauMovementProbe.WriteResult(info.ResultPath, $"HEIGHT IMPACT {source.Info.Name}: position={position}, cell={cell}, height={source.World.Map.Height[cell]}, source={sourcePosition}, target={target.CenterPosition}.");
			}
			if (!info.ExpectDamage)
			{
				// Compare against the authored ridge, not moving/subcell actor centers.
				// Splash damage beyond rock does not imply that the projectile crossed it.
				var cell = source.World.Map.CellContaining(position);
				if (!Enumerable.Range(-2, 5).Any(t => cell == info.BarrierCenter + info.BarrierTangent * t) || source.World.Map.Height[cell] != 4)
					escapedImpacts++;
			}
		}
		void ITick.Tick(Actor self)
		{
			ticks++;
			if (ticks == 10)
			{
				target = self.World.Actors.Single(a => a.Info.Name == info.TargetType);
				health = target.Trait<Health>(); initialHP = health.HP; start = self.Location; sourcePosition = self.CenterPosition;
				if (info.CheckVisibility)
				{
					if (!self.Owner.Shroud.FogEnabled || !self.Owner.Shroud.IsVisible(target.Location))
						throw new InvalidOperationException("Height fixture visibility policy failed: " + self.Info.Name);
					PlateauMovementProbe.WriteResult(info.ResultPath, "HEIGHT SIGHT PASS " + self.Info.Name + ": actual owner shroud reveals target with fog enabled.");
				}
				foreach (var attack in self.TraitsImplementing<AttackBase>())
					attack.AttackTarget(Target.FromActor(target), AttackSource.Default, false, false, true);
			}
			if (ticks != 400) return;
			var damage = weaponDamage;
			var message = $"HEIGHT COMBAT {(shots > 0 && self.Location == start && (info.ExpectDamage ? damage > 0 : impacts > 0 && escapedImpacts == 0) ? "PASS" : "FAIL")} {self.Info.Name}: shots={shots}, impacts={impacts}, beyondRidge={escapedImpacts}, damage={damage}, healthDelta={initialHP - health.HP}, expectDamage={info.ExpectDamage}, moved={self.Location != start}.";
			PlateauMovementProbe.WriteResult(info.ResultPath, message);
			if (message.Contains("FAIL", StringComparison.Ordinal)) throw new InvalidOperationException(message);
			if (!info.CheckVisibility) Game.TakeScreenshot();
		}
	}
}
