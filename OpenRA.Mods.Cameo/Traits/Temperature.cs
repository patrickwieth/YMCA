#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits; // if needed for other trait interfaces

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Adds a temperature system that affects damage and speed. Temperature ranges from 0 to -100 (0 = normal, -100 = frozen).")]
	public class TemperatureInfo : PausableConditionalTraitInfo
	{
		[Desc("Initial temperature (0..-100). 0 = normal; negative values indicate colder state.")]
		public readonly int InitialTemperature = 0;

		[Desc("Minimum temperature (most frozen). Usually -100.")]
		public readonly int MinTemperature = -100;

		[Desc("Maximum temperature (normal). Usually 0.")]
		public readonly int MaxTemperature = 0;

		[Desc("Which damage types are considered 'cryo' and will reduce temperature.")]
		public readonly BitSet<DamageType> CryoDamageTypes = default;

		[Desc("Whether temperature should slowly recover toward MaxTemperature over time.")]
		public readonly bool EnableTemperatureRegen = true;

		[Desc("Amount of temperature recovered every RegenInterval (positive integer). Temperature will move toward MaxTemperature.")]
		public readonly int RegenAmount = 1;

		[Desc("Ticks between temperature regeneration events.")]
		public readonly int RegenInterval = 25;

		public override object Create(ActorInitializer init) { return new Temperature(init, this); }
	}

	public class Temperature : PausableConditionalTrait<TemperatureInfo>, ISync, ITick, INotifyDamage, IDamageModifier, ISpeedModifier
	{
		readonly Actor self;
		IHealth health;

		// synced temperature value (0 .. -100 typically)
		[Sync]
		public int CurrentTemperature;

		int regenTicks;

		public Temperature(ActorInitializer init, TemperatureInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			health = self.TraitOrDefault<IHealth>() ?? throw new InvalidOperationException("Temperature trait requires an IHealth trait on the actor.");

			// Clamp initial temp to configured bounds
			CurrentTemperature = Math.Max(Info.MinTemperature, Math.Min(Info.MaxTemperature, Info.InitialTemperature));

			regenTicks = Info.RegenInterval;
		}

		// ITick: do regen if enabled
		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!Info.EnableTemperatureRegen)
				return;

			if (CurrentTemperature >= Info.MaxTemperature)
				return;

			if (--regenTicks > 0)
				return;

			// Move temperature toward MaxTemperature by RegenAmount (not overshooting)
			var target = Info.MaxTemperature;
			if (CurrentTemperature < target)
			{
				CurrentTemperature += Info.RegenAmount;
				if (CurrentTemperature > target)
					CurrentTemperature = target;
			}

			regenTicks = Info.RegenInterval;
		}

		// INotifyDamage: react to damage and reduce temperature if it's cryo damage
		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value == 0 || e.Attacker == self)
				return;

			// If CryoDamageTypes is empty, do nothing (configuration may choose to leave it empty)
			if (Info.CryoDamageTypes.IsEmpty)
				return;

			// If the damage types do not include any cryo types, ignore
			if (!e.Damage.DamageTypes.Overlaps(Info.CryoDamageTypes))
				return;

			// Compute cryo damage amount (use applied damage value)
			var cryoDamage = Math.Abs(Convert.ToInt32(e.Damage.Value));

			// Temperature reduction = ceil(cryoDamage / MaxHP * 100)
			// Ensure health.MaxHP > 0
			var maxHP = Math.Max(1, health.MaxHP);
			var deltaPercent = (int)Math.Ceiling(cryoDamage * 100.0 / maxHP);

			// Decrease temperature (temperature goes toward MinTemperature which is negative)
			CurrentTemperature -= deltaPercent;

			// Clamp
			if (CurrentTemperature < Info.MinTemperature)
				CurrentTemperature = Info.MinTemperature;

			if (CurrentTemperature > Info.MaxTemperature)
				CurrentTemperature = Info.MaxTemperature;

			// reset regen timer so temperature doesn't immediately regen
			regenTicks = Info.RegenInterval;
		}

		// IDamageModifier: return percentage damage modifier
		// Desired linear mapping: at Temp = 0 => 100% damage taken; at Temp = -100 => 200% damage taken
		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			if (IsTraitDisabled)
				return 100;

			// Damage modifier percent = 100 - Temperature
			// E.g. Temperature = 0 => 100 - 0 = 100%
			// Temperature = -50 => 100 - (-50) = 150%
			// Temperature = -100 => 200%
			var mod = 100 - CurrentTemperature;

			// Keep reasonable bounds
			if (mod < 1) mod = 1;
			if (mod > 1000) mod = 1000;

			return mod;
		}

		// ISpeedModifier: return percentage speed modifier
		// Desired linear mapping: at Temp = 0 => 100% speed; at Temp = -100 => 0% speed
		int ISpeedModifier.GetSpeedModifier()
		{
			if (IsTraitDisabled)
				return 100;

			// Speed percent = 100 + Temperature (Temperature negative)
			// E.g. Temperature = 0 => 100
			// Temperature = -50 => 50
			// Temperature = -100 => 0
			var speed = 100 + CurrentTemperature;

			// clamp 0..1000 for safety
			if (speed < 0) speed = 0;
			if (speed > 1000) speed = 1000;

			return speed;
		}

		// Optional helpers for other systems, debug, etc.
		public void SetTemperature(int value)
		{
			CurrentTemperature = Math.Max(Info.MinTemperature, Math.Min(Info.MaxTemperature, value));
		}

		public int GetTemperature() { return CurrentTemperature; }
	}
}
