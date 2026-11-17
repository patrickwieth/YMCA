using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckLibertyShellmapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-liberty-shellmap";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("MAP", "Validate Liberty Island's real rules, terrain, weapon ranges and sprites before activation.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			using var package = new Folder(Platform.EngineDir).OpenPackage(Path.GetFullPath(args[1]), mod.ModFiles);
			using var map = new Map(mod, package);
			if (map.InvalidCustomRules) throw new InvalidDataException("Invalid Liberty rules", map.InvalidCustomRulesException);
			if (map.Tileset != "RUBBERDUCK-TEMPERATE" || (map.Visibility != 0 && map.Visibility != MapVisibility.Shellmap))
				throw new InvalidDataException("Unexpected tileset or visibility.");
			var types = map.ActorDefinitions.Select(n => n.Value.Value.ToLowerInvariant()).ToArray();
			if (types.Count(t => t == "aegis_cruiser") != 10 || types.Count(t => t == "agun") != 6 || types.Count(t => t == "liberty.statue") != 1)
				throw new InvalidDataException("Expected ten cruisers, six flaks and one statue.");
			var occupied = new HashSet<CPos>();
			foreach (var node in map.ActorDefinitions)
			{
				var actor = map.Rules.Actors[node.Value.Value.ToLowerInvariant()];
				_ = actor.TraitsInConstructOrder().ToArray();
				var reference = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = reference.Get<LocationInit>().Value;
				if (!map.Contains(location)) throw new InvalidDataException(node.Key + " is outside the map.");
				if (actor.Name == "aegis_cruiser" && map.GetTerrainInfo(location).Type != "Water")
					throw new InvalidDataException(node.Key + " is not on water.");
				var building = actor.TraitInfoOrDefault<BuildingInfo>();
				if (building == null) continue;
				foreach (var c in building.Tiles(location))
					if (!map.Contains(c) || !building.TerrainTypes.Contains(map.GetTerrainInfo(c).Type) || !occupied.Add(c))
						throw new InvalidDataException(node.Key + " has an invalid/overlapping building footprint at " + c);
			}
			foreach (var name in new[] { "kirovbomb", "kirovteslabomb", "kirovclusterbomb", "kirovnukebomb" })
			{
				var weapon = map.Rules.Weapons[name];
				if (weapon.MinRange.Length >= weapon.Range.Length) throw new InvalidDataException(name + " cannot attack inside its range.");
			}
			map.Sequences.LoadSprites();
			foreach (var sequence in new[] { "idle", "damaged-idle", "construction", "dead", "icon" })
				_ = map.Sequences.GetSequence("liberty.statue", sequence);
			Console.WriteLine("Liberty: actual rules, ten naval placements, six flaks, statue footprint, bomb ranges and complete sprite set passed.");
		}
	}
}
