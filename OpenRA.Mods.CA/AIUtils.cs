﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA
{
	public enum BuildingType { Building, Defense, Refinery, Fragile, BaseCrawl }

	public enum WaterCheck { NotChecked, EnoughWater, NotEnoughWater, DontCheck }

	public static class AIUtils
	{
		public static bool IsAreaAvailable<T>(World world, Player player, Map map, int radius, HashSet<string> terrainTypes)
		{
			var cells = world.ActorsHavingTrait<T>().Where(a => a.Owner == player);

			// TODO: Properly check building foundation rather than 3x3 area.
			return cells.Select(a => map.FindTilesInCircle(a.Location, radius)
				.Count(c => map.Contains(c) && terrainTypes.Contains(map.GetTerrainInfo(c).Type) &&
					Util.AdjacentCells(world, Target.FromCell(world, c))
						.All(ac => terrainTypes.Contains(map.GetTerrainInfo(ac).Type))))
							.Any(availableCells => availableCells > 0);
		}

		public static IEnumerable<ProductionQueue> FindQueues(Player player, string category)
		{
			return player.World.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category && a.Trait.Enabled)
				.Select(a => a.Trait);
		}

		public static IEnumerable<Actor> GetActorsWithTrait<T>(World world)
		{
			return world.ActorsHavingTrait<T>();
		}

		public static int CountActorsWithNameAndTrait<T>(string actorName, Player owner)
		{
			return owner.World.ActorsHavingTrait<T>().Count(a => a.Owner == owner && a.Info.Name == actorName);
		}

		public static int CountActorByCommonName<TTraitInfo>(ActorIndex.OwnerAndNamesAndTrait<TTraitInfo> actorIndex) where TTraitInfo : ITraitInfoInterface
		{
			return actorIndex.Actors.Count(a => !a.IsDead);
		}

		public static int CountBuildingByCommonName(HashSet<string> buildings, Player owner)
		{
			return GetActorsWithTrait<Building>(owner.World)
				.Count(a => a.Owner == owner && buildings.Contains(a.Info.Name));
		}

		public static List<Actor> FindEnemiesByCommonName(HashSet<string> commonNames, Player player)
		{
			return player.World.Actors.Where(a => !a.IsDead && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy &&
				commonNames.Contains(a.Info.Name)).ToList();
		}

		public static ActorInfo GetInfoByCommonName(HashSet<string> names, Player owner)
		{
			return owner.World.Map.Rules.Actors.Where(k => names.Contains(k.Key)).Random(owner.World.LocalRandom).Value;
		}

		public static void BotDebug(string s, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				TextNotificationsManager.Debug(s, args);
		}

		public static bool PathExist(Actor unit, CPos destination, Actor ignoreActor, BlockedByActor blockedByActor = BlockedByActor.Immovable)
		{
			var mobile = unit.TraitOrDefault<Mobile>();
			if (mobile == null)
			{
				// We consider other IMove ignore all blockers
				if (unit.TraitsImplementing<IMove>().Any())
					return true;
				else
					return false;
			}

			if (mobile.PathFinder.FindPathToTargetCell(unit, new List<CPos> { unit.Location }, destination, blockedByActor, ignoreActor: ignoreActor, laneBias: false).Count > 0)
				return true;
			else
				return false;
		}
	}
}
