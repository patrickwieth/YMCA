#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Recomputes custom-formation slowdown assignments at a fixed interval.")]
	public class CustomFormationSlowdownManagerInfo : TraitInfo
	{
		[Desc("Ticks between formation slowdown recalculations.")]
		public readonly int RefreshTicks = 10;

		[Desc("Radius in cells below which actors are considered inside the centroid cluster.")]
		public readonly int MinRadiusCells = 1;

		[Desc("Distance in cells from the assigned target that counts as completed.")]
		public readonly int CompletionDistanceCells = 1;

		public override object Create(ActorInitializer init) { return new CustomFormationSlowdownManager(init.Self, this); }
	}

	public class CustomFormationSlowdownManager : ITick
	{
		class FormationSession
		{
			public readonly Dictionary<Actor, WPos> Assignments;
			public int TicksUntilUpdate;

			public FormationSession(Dictionary<Actor, WPos> assignments, int refreshTicks)
			{
				Assignments = assignments;
				TicksUntilUpdate = refreshTicks;
			}
		}

		readonly Actor worldActor;
		readonly CustomFormationSlowdownManagerInfo info;
		readonly List<FormationSession> sessions = new();

		public CustomFormationSlowdownManager(Actor worldActor, CustomFormationSlowdownManagerInfo info)
		{
			this.worldActor = worldActor;
			this.info = info;
		}

		public void RegisterSession(List<Actor> actors, List<WPos> targets)
		{
			if (actors == null || targets == null)
				return;

			var count = Math.Min(actors.Count, targets.Count);
			if (count < 2)
				return;

			var assignments = new Dictionary<Actor, WPos>();
			for (int i = 0; i < count; i++)
			{
				var actor = actors[i];
				if (!IsActorValid(actor))
					continue;

				if (assignments.ContainsKey(actor))
					continue;

				if (actor.TraitOrDefault<CustomFormationSpeedController>() == null)
					continue;

				assignments.Add(actor, targets[i]);
			}

			if (assignments.Count < 2)
				return;

			var session = new FormationSession(assignments, Math.Max(1, info.RefreshTicks));
			sessions.Add(session);
			EvaluateSession(session);
		}

		void ITick.Tick(Actor self)
		{
			for (int i = sessions.Count - 1; i >= 0; i--)
			{
				var session = sessions[i];
				session.TicksUntilUpdate--;
				if (session.TicksUntilUpdate > 0)
					continue;

				session.TicksUntilUpdate = Math.Max(1, info.RefreshTicks);
				if (!EvaluateSession(session))
					sessions.RemoveAt(i);
			}
		}

		bool EvaluateSession(FormationSession session)
		{
			var assignments = session.Assignments;
			var removals = assignments.Keys.Where(a => !IsActorValid(a)).ToList();
			foreach (var actor in removals)
			{
				assignments.Remove(actor);
				ResetControllerIfSafe(actor);
			}

			if (assignments.Count < 2)
				return false;

			var actors = assignments.Keys.ToList();
			var center = CalculateCenter(actors);
			var radiusBase = Math.Max(1, info.MinRadiusCells);
			var radiusThreshold = (int)(WDist.FromCells(radiusBase).Length * Math.Max(1.0, Math.Sqrt(actors.Count)));
			var completionThreshold = WDist.FromCells(Math.Max(1, info.CompletionDistanceCells)).Length;

			var speedCache = new Dictionary<Actor, int>();
			var aheadFlags = new Dictionary<Actor, bool>();
			var offsets = new Dictionary<Actor, int>();

			var minSpeed = int.MaxValue;
			var hasCandidate = false;
			var allComplete = true;

			foreach (var kv in assignments)
			{
				var actor = kv.Key;
				var target = kv.Value;

				var actorDistance = (actor.CenterPosition - target).Length;
				if (actorDistance > completionThreshold)
					allComplete = false;

				var centerDistance = (center - target).Length;
				var offset = (actor.CenterPosition - center).Length;

				offsets[actor] = offset;
				var isAhead = offset > radiusThreshold && actorDistance < centerDistance;
				aheadFlags[actor] = isAhead;
				if (isAhead)
					hasCandidate = true;

				var speed = GetActorSpeed(actor);
				speedCache[actor] = speed;
				if (speed < minSpeed)
					minSpeed = speed;
			}

			if (allComplete)
			{
				ResetControllers(assignments.Keys);
				return false;
			}

			var shouldSlow = hasCandidate && minSpeed > 0;
			var halfSlowest = Math.Max(1, minSpeed / 2);

			foreach (var actor in assignments.Keys)
			{
				var controller = actor.TraitOrDefault<CustomFormationSpeedController>();
				if (controller == null)
					continue;

				if (!shouldSlow || !aheadFlags[actor])
				{
					controller.Reset();
					continue;
				}

				var actorSpeed = speedCache[actor];
				var desiredPercent = (int)Math.Clamp((long)halfSlowest * 100 / Math.Max(1, actorSpeed), 1, 100);
				controller.ApplySpeedPercent(desiredPercent, info.RefreshTicks);
			}

			return true;
		}

		bool IsActorValid(Actor actor)
		{
			if (actor == null)
				return false;

			if (actor.Disposed || actor.IsDead || actor.World != worldActor.World)
				return false;

			return true;
		}

		static WPos CalculateCenter(IReadOnlyList<Actor> actors)
		{
			if (actors.Count == 0)
				return WPos.Zero;

			long sumX = 0;
			long sumY = 0;
			long sumZ = 0;

			foreach (var actor in actors)
			{
				var pos = actor.CenterPosition;
				sumX += pos.X;
				sumY += pos.Y;
				sumZ += pos.Z;
			}

			return new WPos((int)(sumX / actors.Count), (int)(sumY / actors.Count), (int)(sumZ / actors.Count));
		}

		static void ResetControllers(IEnumerable<Actor> actors)
		{
			foreach (var actor in actors)
				ResetControllerIfSafe(actor);
		}

		static void ResetControllerIfSafe(Actor actor)
		{
			if (actor == null || !actor.IsInWorld || actor.Disposed)
				return;

			actor.TraitOrDefault<CustomFormationSpeedController>()?.Reset();
		}

		static int GetActorSpeed(Actor actor)
		{
			var mobile = actor.TraitOrDefault<Mobile>();
			if (mobile != null)
			{
				var speed = mobile.MovementSpeedForCell(actor.Location);
				if (speed <= 0)
					speed = mobile.Info.Speed;
				if (speed > 0)
					return speed;
			}

			var aircraft = actor.TraitOrDefault<Aircraft>();
			if (aircraft != null)
			{
				var speed = aircraft.MovementSpeed;
				if (speed <= 0)
					speed = aircraft.Info.Speed;
				if (speed > 0)
					return speed;
			}

			return 1;
		}
	}
}
