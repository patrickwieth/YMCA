#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class PeriodicCargoProducerInfo : PausableConditionalTraitInfo
	{
		[ActorReference]

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Duration between productions.")]
		public readonly int ChargeDuration = 1000;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color ChargeColor = Color.DarkOrange;

		[Desc("The condition to grant to produced units")]
		public readonly string Condition = null;

		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship SelectionBarDisplayRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new PeriodicCargoProducer(init, this); }
	}

	public class PeriodicCargoProducer : PausableConditionalTrait<PeriodicCargoProducerInfo>, ISelectionBar, ITick, ISync
	{
		readonly PeriodicCargoProducerInfo info;
		Actor self;

		[Sync]
		int ticks;
		int productionDuration;

		public PeriodicCargoProducer(ActorInitializer init, PeriodicCargoProducerInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			ticks = 0;
			productionDuration = 0;
		}

		void GrantCondition(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			self.GrantCondition(cond);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused)
				return;

			if (!IsTraitDisabled && --ticks < 0)
			{
				var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Contains(info.Type));

				var activated = false;

				if (sp != null)
				{
					foreach (var passenger in self.TraitOrDefault<Cargo>().Passengers)
					{
						var name = passenger.Info.Name;
						//var placed = false;

						var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

						activated |= sp.Produce(self, passenger.Info, info.Type, inits, 0, unit => {
							//Game.Debug(String.Join("; ", firedBy.TraitOrDefault<Production>() ));
							GrantCondition(unit, info.Condition);

							if( passenger.TraitOrDefault<Cargo>() != null )
							{
								foreach (var p in passenger.TraitOrDefault<Cargo>().Passengers)
								{
										//Game.Debug(String.Join("; ", passenger.TraitOrDefault<Cargo>().Passengers));
										//Game.Debug(p.Info.Name);

										var newPassenger = self.World.CreateActor(false, p.Info.Name, inits);
										unit.TraitOrDefault<Cargo>().Load(unit, newPassenger);
								}
							}
						});
					}
				}

				if (activated)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);

				ticks = calculateProductionCost();
			}
		}

		int calculateProductionCost()
		{
			var summedCost = 0;
			foreach (var p in self.TraitOrDefault<Cargo>().Passengers)
			{
					summedCost += p.Info.TraitInfos<ValuedInfo>().First().Cost;

					if( p.TraitOrDefault<Cargo>() != null )
					{
						foreach (var p2 in p.TraitOrDefault<Cargo>().Passengers)
						{
								summedCost += p2.Info.TraitInfos<ValuedInfo>().First().Cost;
						}
					}
			}
			// here we use a reload modifier even though this is a production, a bit "hacky" but actually exactly what we want
			var modifiers = self.TraitsImplementing<IReloadModifier>()
				.Select(m => m.GetReloadModifier());

			productionDuration = summedCost * Util.ApplyPercentageModifiers(info.ChargeDuration, modifiers) / 1000;
			productionDuration = productionDuration == 0 ? 0 : productionDuration;
			return productionDuration;
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				ticks = calculateProductionCost();
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !Info.SelectionBarDisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
				return 0f;

			if (productionDuration == 0)
				return 0;

			return (float)(productionDuration - ticks) / productionDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.ChargeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty
		{
			get
			{
				var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
				if (viewer != null && !Info.SelectionBarDisplayRelationships.HasStance(self.Owner.RelationshipWith(viewer)))
					return false;

				return info.ShowSelectionBar && !IsTraitDisabled;
			}
		}
	}
}
