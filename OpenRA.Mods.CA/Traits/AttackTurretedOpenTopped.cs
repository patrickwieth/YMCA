#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("Implements the YR OpenTopped logic where transported actors used separate firing offsets, ignoring facing."
		+ "Compatible with both `Cargo`/`Passengers` or `Garrionable`/`Garrisoners` logic.")]
	public class AttackTurretedOpenToppedInfo : AttackFollowInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Fire port offsets in local coordinates.")]
		public readonly WVec[] PortOffsets = null;

		[Desc("Turret names")]
		public readonly string[] Turrets = { "primary" };

		[Desc("Passenger Armaments shooting from Open Top")]
		public readonly string[] PassengerArmaments = { "primary" };

		public override object Create(ActorInitializer init) { return new AttackTurretedOpenTopped(init.Self, this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (PortOffsets.Length == 0)
				throw new YamlException("PortOffsets must have at least one entry.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class AttackTurretedOpenTopped : AttackFollow, IRender, INotifyPassengerEntered, INotifyPassengerExited
	{
		public readonly new AttackTurretedOpenToppedInfo Info;
		readonly Lazy<BodyOrientation> coords;
		readonly List<Actor> actors;
		readonly List<Armament> passengerArmaments;
		readonly HashSet<(AnimationWithOffset MuzzleFlash, string Palette)> muzzles;
		readonly Dictionary<Actor, IFacing> paxFacing;
		readonly Dictionary<Actor, IPositionable> paxPos;
		readonly Dictionary<Actor, RenderSprites> paxRender;
		protected TurretedOpenTop[] turrets;

		public AttackTurretedOpenTopped(Actor self, AttackTurretedOpenToppedInfo info)
			: base(self, info)
		{
			Info = info;
			coords = Exts.Lazy(() => self.Trait<BodyOrientation>());
			actors = new List<Actor>();
			passengerArmaments = new List<Armament>();
			muzzles = new HashSet<(AnimationWithOffset, string)>();
			paxFacing = new Dictionary<Actor, IFacing>();
			paxPos = new Dictionary<Actor, IPositionable>();
			paxRender = new Dictionary<Actor, RenderSprites>();
		}

		protected override void Created(Actor self)
		{
			turrets = self.TraitsImplementing<TurretedOpenTop>().Where(t => Info.Turrets.Contains(t.Info.Turret)).ToArray();
			base.Created(self);
		}

		protected override Func<IEnumerable<Armament>> InitializeGetArmaments(Actor self)
		{
			var armaments = self.TraitsImplementing<Armament>()
				.Where(a => Info.Armaments.Contains(a.Info.Name)).ToArray();

			return () => armaments.Concat(passengerArmaments);
		}

		void OnActorEntered(Actor enterer)
		{
			actors.Add(enterer);
			paxFacing.Add(enterer, enterer.Trait<IFacing>());
			paxPos.Add(enterer, enterer.Trait<IPositionable>());
			paxRender.Add(enterer, enterer.Trait<RenderSprites>());
			passengerArmaments.AddRange(
				enterer.TraitsImplementing<Armament>()
				.Where(a => Info.PassengerArmaments.Contains(a.Info.Name)));
		}

		void OnActorExited(Actor exiter)
		{
			actors.Remove(exiter);
			paxFacing.Remove(exiter);
			paxPos.Remove(exiter);
			paxRender.Remove(exiter);
			passengerArmaments.RemoveAll(a => a.Actor == exiter);
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			OnActorEntered(passenger);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			OnActorExited(passenger);
		}

		WVec SelectFirePort(Actor self, Actor firer)
		{
			var passengerIndex = actors.IndexOf(firer);
			if (passengerIndex == -1)
				return new WVec(0, 0, 0);

			var portIndex = passengerIndex % Info.PortOffsets.Length;

			return Info.PortOffsets[portIndex];
		}

		WVec PortOffset(Actor self, WVec offset)
		{
			var bodyOrientation = coords.Value.QuantizeOrientation(self, self.Orientation);
			return coords.Value.LocalToWorld(offset.Rotate(bodyOrientation));
		}

		protected bool TurretCanAttack(Actor self, in Target target)
		{
			if (target.Type == TargetType.Invalid)
				return false;

			// Don't break early from this loop - we want to bring all turrets to bear!
			var turretReady = false;

			// TODO WHY ?
			//turrets = self.TraitsImplementing<TurretedOpenTop>().Where(t => Info.Turrets.Contains(t.Info.Turret)).ToArray();

			foreach (var t in turrets)
			{
				if (t.FaceTarget(self, target))
					turretReady = true;
			}

			return turretReady && base.CanAttack(self, target);
		}

		protected bool PassengerCanAttack(Actor self, in Target target)
		{
			if (!self.IsInWorld || IsTraitDisabled || IsTraitPaused)
				return false;

			if (!target.IsValidFor(self))
				return false;

			if (!HasAnyValidWeapons(target))
				return false;

			var mobile = self.TraitOrDefault<Mobile>();
			if (mobile != null && !mobile.CanInteractWithGroundLayer(self))
				return false;

			if (passengerArmaments.All(a => a.IsReloading))
				return false;

			return true;
		}

		public override void DoAttack(Actor self, in Target target)
		{
			if (TurretCanAttack(self, target))
			{
				foreach (var a in Armaments)
				{
					a.CheckFire(self, facing, target);
				}
			}

			if (!PassengerCanAttack(self, target))
				return;

			var pos = self.CenterPosition;
			var targetedPosition = GetTargetPosition(pos, target);
			var targetYaw = (targetedPosition - pos).Yaw;

			foreach (var a in passengerArmaments)
			{
				//Game.Debug(a.Info.Name.ToString());

				if (a.IsTraitDisabled)
					continue;

				var port = SelectFirePort(self, a.Actor);

				var muzzleFacing = targetYaw;
				paxFacing[a.Actor].Facing = muzzleFacing;
				paxPos[a.Actor].SetVisualPosition(a.Actor, pos + PortOffset(self, port));

				var barrel = a.CheckFire(a.Actor, facing, target);
				if (barrel == null)
					continue;

				if (a.Info.MuzzleSequence != null)
				{
					// Muzzle facing is fixed once the firing starts
					var muzzleAnim = new Animation(self.World, paxRender[a.Actor].GetImage(a.Actor), () => targetYaw);
					var sequence = a.Info.MuzzleSequence;
					var palette = a.Info.MuzzlePalette;

					var muzzleFlash = new AnimationWithOffset(muzzleAnim,
						() => PortOffset(self, port),
						() => false,
						p => RenderUtils.ZOffsetFromCenter(self, p, 1024));

					var pair = (muzzleFlash, palette);
					muzzles.Add(pair);
					muzzleAnim.PlayThen(sequence, () => muzzles.Remove(pair));
				}

				foreach (var npa in self.TraitsImplementing<INotifyAttack>())
					npa.Attacking(self, target, a, barrel);
			}
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			// Display muzzle flashes
			foreach (var m in muzzles)
				foreach (var r in m.MuzzleFlash.Render(self, wr, wr.Palette(m.Palette), 1f))
					yield return r;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Muzzle flashes don't contribute to actor bounds
			yield break;
		}

		protected override void Tick(Actor self)
		{
			base.Tick(self);

			// Take a copy so that Tick() can remove animations
			foreach (var m in muzzles.ToArray())
				m.MuzzleFlash.Animation.Tick();
		}
	}
}
