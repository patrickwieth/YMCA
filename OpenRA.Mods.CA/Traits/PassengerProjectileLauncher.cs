#region Copyright & License Information
/*
 * Copyright 2015-2025 OpenRA.Mods.CA Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Launches the first passenger as a projectile when the specified armament fires.")]
	public class PassengerProjectileLauncherInfo : TraitInfo, Requires<CargoInfo>
	{
		[Desc("Armament name that triggers the passenger launch.")]
		public readonly string Armament = "passenger";

		[ActorReference]
		[Desc("Projectile actor to spawn for the launched passenger.")]
		public readonly string ProjectileActor = null;

		[Desc("Offset from the firing actor where the projectile spawns.")]
		public readonly WVec SpawnOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new PassengerProjectileLauncher(init.Self, this); }
	}

	public class PassengerProjectileLauncher : INotifyAttack
	{
		readonly Actor self;
		readonly PassengerProjectileLauncherInfo info;
		readonly Cargo cargo;

		public PassengerProjectileLauncher(Actor self, PassengerProjectileLauncherInfo info)
		{
			this.self = self;
			this.info = info;
			cargo = self.Trait<Cargo>();
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (info.ProjectileActor == null)
				return;

			if (!string.IsNullOrEmpty(info.Armament) && a.Info.Name != info.Armament)
				return;

			var passenger = cargo.Passengers.FirstOrDefault();
			if (passenger == null)
				return;

			cargo.Unload(self, passenger);

			var spawnPos = self.CenterPosition + info.SpawnOffset.Rotate(self.Orientation);
			var facing = target.Type == TargetType.Invalid ? self.Orientation.Yaw : (target.CenterPosition - spawnPos).Yaw;
			var init = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new FacingInit(facing),
				new CenterPositionInit(spawnPos),
				new PassengerPayloadInit(passenger)
			};

			var projectile = self.World.CreateActor(false, info.ProjectileActor.ToLowerInvariant(), init);
			var missileBase = projectile.TraitOrDefault<MissileBase>();
			var targetPos = target.Type == TargetType.Invalid ? Target.FromPos(spawnPos) : Target.FromPos(target.CenterPosition);

			if (missileBase != null)
				missileBase.SetTarget(targetPos);
			else
			{
				var bm = projectile.TraitOrDefault<BallisticMissile>();
				if (bm != null)
					bm.Target = targetPos;
			}

			self.World.AddFrameEndTask(w => w.Add(projectile));

			if (cargo.IsEmpty())
				foreach (var attack in self.TraitsImplementing<AttackBase>())
					attack.OnStopOrder(self);
		}
	}
}
