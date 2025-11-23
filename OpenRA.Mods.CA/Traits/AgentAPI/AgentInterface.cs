#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits.AgentAPI
{
	[Desc("External agent interface for programmatic control via HTTP API.")]
	[TraitLocation(SystemActors.Player)]
	public class AgentInterfaceInfo : TraitInfo
	{
		[Desc("Enable agent control for this player. Can be overridden by Server.AgentAPI=true command line parameter.")]
		public readonly bool Enabled = false;

		[Desc("Minimum portion of pending orders to issue each tick (e.g. 5 issues at least 1/5th of all pending orders).")]
		public readonly int MinOrderQuotientPerTick = 5;

		public override object Create(ActorInitializer init)
		{
			return new AgentInterface(this, init);
		}
	}

	public class AgentInterface : ITick, INotifyCreated
	{
		public bool IsEnabled;
		public Player Player => player;

		readonly AgentInterfaceInfo info;
		readonly World world;
		readonly Queue<Order> orders = new Queue<Order>();

		Player player;

		public AgentInterface(AgentInterfaceInfo info, ActorInitializer init)
		{
			this.info = info;
			world = init.World;
		}

		static bool CheckCommandLineParameter()
		{
			var args = Environment.GetCommandLineArgs();
			foreach (var arg in args)
			{
				if (arg == "Server.AgentAPI=true")
					return true;
			}

			return false;
		}

		void INotifyCreated.Created(Actor self)
		{
			player = self.Owner;

			var enabledViaCommandLine = CheckCommandLineParameter();
			if (!info.Enabled && !enabledViaCommandLine)
				return;

			if (world.IsReplay)
				return;

			if (player == null || player.IsBot || player.NonCombatant)
				return;

			IsEnabled = true;

			var globalServer = world.WorldActor.TraitOrDefault<GlobalAgentServer>();
			if (globalServer != null)
			{
				globalServer.RegisterAgent(this);
			}
			else
			{
				Console.WriteLine($"[AgentInterface] Warning: GlobalAgentServer not found. HTTP API will not be available.");
			}

			Game.RunAfterTick(() =>
			{
				Console.WriteLine($"[AgentInterface] Agent Interface activated for player {player.PlayerName}");
			});
		}

		public void QueueOrderFromExternal(Order order)
		{
			orders.Enqueue(order);
		}

		void ITick.Tick(Actor self)
		{
			if (!IsEnabled || self.World.IsLoadingGameSave)
				return;

			// Issue orders gradually to avoid flooding
			var ordersToIssueThisTick = Math.Min(
				(orders.Count + info.MinOrderQuotientPerTick - 1) / info.MinOrderQuotientPerTick,
				orders.Count);

			for (var i = 0; i < ordersToIssueThisTick; i++)
				world.IssueOrder(orders.Dequeue());
		}

		// Get current game state for external agent
		public GameStateData GetGameState()
		{
			try
			{
				if (player == null || !IsEnabled)
					return null;

				if (player.PlayerActor == null)
					return null;

				var playerResources = player.PlayerActor.TraitOrDefault<PlayerResources>();

				return new GameStateData
				{
					Tick = world.WorldTick,
					Cash = playerResources?.Cash ?? 0,
					Resources = playerResources?.Resources ?? 0,
					PowerProvided = 0, // Will be calculated
					PowerDrained = 0,
					Units = GetUnits(),
					Buildings = GetBuildings(),
					Enemies = GetEnemies()
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AgentInterface] GetGameState error: {ex.Message}");
				Console.WriteLine($"[AgentInterface] Stack trace: {ex.StackTrace}");
				throw;
			}
		}

		List<UnitData> GetUnits()
		{
			var units = new List<UnitData>();
			foreach (var actor in world.Actors.Where(a =>
				a.Owner == player &&
				!a.IsDead &&
				a.IsInWorld &&
				a.Info.HasTraitInfo<MobileInfo>()))
			{
				var mobile = actor.TraitOrDefault<Mobile>();
				var health = actor.TraitOrDefault<IHealth>();
				var buildable = actor.Info.TraitInfoOrDefault<BuildableInfo>();

				units.Add(new UnitData
				{
					Id = actor.ActorID,
					Type = actor.Info.Name,
					Position = new Position2D { X = actor.CenterPosition.X, Y = actor.CenterPosition.Y },
					Cell = new Cell2D { X = actor.Location.X, Y = actor.Location.Y },
					Health = health != null ? new HealthData { Current = health.HP, Max = health.MaxHP } : null,
					IsIdle = actor.IsIdle,
					CanMove = mobile != null,
					CanAttack = actor.TraitsImplementing<AttackBase>().Any()
				});
			}

			return units;
		}

		List<BuildingData> GetBuildings()
		{
			var buildings = new List<BuildingData>();

			foreach (var actor in world.Actors.Where(a =>
				a.Owner == player &&
				!a.IsDead &&
				a.IsInWorld &&
				a.Info.HasTraitInfo<BuildingInfo>()))
			{
				var health = actor.TraitOrDefault<IHealth>();
				var productionQueue = actor.TraitOrDefault<ProductionQueue>();

				buildings.Add(new BuildingData
				{
					Id = actor.ActorID,
					Type = actor.Info.Name,
					Cell = new Cell2D { X = actor.Location.X, Y = actor.Location.Y },
					Health = health != null ? new HealthData { Current = health.HP, Max = health.MaxHP } : null,
					IsProducing = productionQueue?.CurrentItem() != null
				});
			}

			return buildings;
		}

		List<EnemyData> GetEnemies()
		{
			var enemies = new List<EnemyData>();

			foreach (var actor in world.Actors.Where(a =>
				a.Owner != player &&
				a.Owner != null &&
				!a.IsDead &&
				a.IsInWorld))
			{
				try
				{
					if (player.Shroud != null && !player.Shroud.IsExplored(actor.Location))
						continue;

					var health = actor.TraitOrDefault<IHealth>();

					enemies.Add(new EnemyData
					{
						Id = actor.ActorID,
						Type = actor.Info.Name,
						Position = new Position2D { X = actor.CenterPosition.X, Y = actor.CenterPosition.Y },
						Cell = new Cell2D { X = actor.Location.X, Y = actor.Location.Y },
						Owner = actor.Owner?.PlayerName ?? "Unknown",
						Health = health != null ? new HealthData { Current = health.HP, Max = health.MaxHP } : null,
						IsBuilding = actor.Info.HasTraitInfo<BuildingInfo>()
					});
				}
				catch (Exception)
				{
					// Skip actors with invalid state (e.g., no valid location yet)
					continue;
				}
			}

			return enemies;
		}
	}

	// Data structures for JSON serialization
	public class GameStateData
	{
		public int Tick { get; set; }
		public int Cash { get; set; }
		public int Resources { get; set; }
		public int PowerProvided { get; set; }
		public int PowerDrained { get; set; }
		public List<UnitData> Units { get; set; }
		public List<BuildingData> Buildings { get; set; }
		public List<EnemyData> Enemies { get; set; }
	}

	public class UnitData
	{
		public uint Id { get; set; }
		public string Type { get; set; }
		public Position2D Position { get; set; }
		public Cell2D Cell { get; set; }
		public HealthData Health { get; set; }
		public bool IsIdle { get; set; }
		public bool CanMove { get; set; }
		public bool CanAttack { get; set; }
	}

	public class BuildingData
	{
		public uint Id { get; set; }
		public string Type { get; set; }
		public Cell2D Cell { get; set; }
		public HealthData Health { get; set; }
		public bool IsProducing { get; set; }
	}

	public class EnemyData
	{
		public uint Id { get; set; }
		public string Type { get; set; }
		public Position2D Position { get; set; }
		public Cell2D Cell { get; set; }
		public string Owner { get; set; }
		public HealthData Health { get; set; }
		public bool IsBuilding { get; set; }
	}

	public class Position2D
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

	public class Cell2D
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

	public class HealthData
	{
		public int Current { get; set; }
		public int Max { get; set; }
	}
}
