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
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[Desc("When enabled, the actor will randomly try to attack nearby other actors.")]
	public class CheckpointInfo : ProximityCapturableInfo, IEditorActorOptions
	{
		[Desc("Display order for the facing slider in the map editor")]
		public readonly int EditorHierarchyDisplayOrder = 3;

		[Desc("The hierarchy of a checkpoint determines the possible next checkpoints, where a unit can go")]
		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorSlider("Hierarchy", EditorHierarchyDisplayOrder, -8, 8, 8,
				actor =>
				{
					var init = actor.GetInitOrDefault<HierarchyInit>();
					return init?.Value ?? 0;
				},
				(actor, value) => actor.ReplaceInit(new HierarchyInit((int)value)));
		}

		public override object Create(ActorInitializer init) { return new Checkpoint(init, init.Self, this); }
	}

	class Checkpoint : ProximityCapturable, INotifyIdle
	{
		readonly CheckpointInfo info;
		readonly int Hierarchy;

		public Checkpoint(ActorInitializer init, Actor self, CheckpointInfo info)
			: base(self, info)
		{
			 this.info = info;

			 var hierarchyInit = init.GetOrDefault<HierarchyInit>();
			 if (hierarchyInit != null)
				 Hierarchy = hierarchyInit.Value;
		}

		public override void ActorEntered(Actor other)
		{
			TextNotificationsManager.Debug("bla "+other.ToString());

			var autobattler = other.TraitOrDefault<Autobattler>();
			if (autobattler != null)
			{
				var nextCheckpoint = Self.World.ActorsHavingTrait<Checkpoint>()
					.Where(a => a.TraitOrDefault<Checkpoint>().Hierarchy > Self.TraitOrDefault<Checkpoint>().Hierarchy)
					.ClosestTo(Self);

				foreach( var cp in Self.World.ActorsHavingTrait<Checkpoint>()
					) //&& a.TraitOrDefault<Checkpoint>().info.Hierarchy > Self.TraitOrDefault<Checkpoint>().info.Hierarchy) )
				{
					TextNotificationsManager.Debug("cp hierarchy:"+cp.TraitOrDefault<Checkpoint>().Hierarchy);
				}

				TextNotificationsManager.Debug("nextcp:"+nextCheckpoint);

				if (nextCheckpoint != null)
					autobattler.setNextCheckpoint(nextCheckpoint);
			}

			base.ActorEntered(other);
		}

		void INotifyIdle.TickIdle(Actor self)
		{

		}
	}

	public class HierarchyInit : ValueActorInit<int>, ISingleInstanceInit
	{
		public HierarchyInit(int value)
			: base(value) { }
	}

}
