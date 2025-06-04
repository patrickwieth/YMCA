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
	[Desc("When enabled, the actor will send units with autobattler trait to other checkpoints.")]
	public class CheckpointInfo : ProximityCapturableInfo, IEditorActorOptions
	{
		[Desc("Display order for the facing slider in the map editor")]
		public readonly int EditorHierarchyDisplayOrder = 3;

		[Desc("The interval of game updates between autobattle units are sent forward")]
		public readonly int waveInterval = 1000;

		[Desc("Actor to spawn as group leader at checkpoint.")]
		public readonly string LeaderDummyActor = null;

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

	class Checkpoint : ProximityCapturable, INotifyAddedToWorld, ITick
	{
		readonly CheckpointInfo info;
		readonly public int Hierarchy;
		public RallyPoint RallyPoint;
		public int Ticks;

		public Checkpoint(ActorInitializer init, Actor self, CheckpointInfo info)
			: base(self, info)
		{
			 this.info = info;

			 var hierarchyInit = init.GetOrDefault<HierarchyInit>();
			 if (hierarchyInit != null)
				 Hierarchy = hierarchyInit.Value;
				 Ticks = Hierarchy;
		}

		protected void Created(Actor self)
		{
			RallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		public override void ActorEntered(Actor other)
		{
			var autobattler = other.TraitOrDefault<Autobattler>();
			if (autobattler != null)
				autobattler.handleCheckpoint(other);

			base.ActorEntered(other);
		}

		void ITick.Tick(Actor self) {
			if (++Ticks >= info.waveInterval)
			{
				Ticks = 0;
				spawnGroupLeader(self);
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			RallyPoint = self.TraitOrDefault<RallyPoint>();
			base.AddedToWorldTasks(self);
		}

		public void spawnGroupLeader(Actor self) {

			var td = new TypeDictionary
			{
				new ParentActorInit(self),
				new LocationInit(self.Location + Info.Offset),
				new CenterPositionInit(self.CenterPosition)
			};

			var newUnit = self.World.AddFrameEndTask(w => w.CreateActor(Info.LeaderDummyActor, td));
			var move = newUnit.TraitOrDefault<IMove>();
			newUnit.QueueActivity(new AttackMoveActivity(newUnit, () => move.MoveTo(cell, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));
		}
	}

	public class HierarchyInit : ValueActorInit<int>, ISingleInstanceInit
	{
		public HierarchyInit(int value)
			: base(value) { }
	}


}
