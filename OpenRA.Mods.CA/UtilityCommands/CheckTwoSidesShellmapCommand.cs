using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckTwoSidesShellmapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-two-sides-shellmap";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;

		[Desc("MAP", "Validate shellmap factions, bot drivers, initial footprints, exits and actual sprite loading.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			using var package = new Folder(Platform.EngineDir).OpenPackage(Path.GetFullPath(args[1]), mod.ModFiles);
			using var map = new Map(mod, package);
			if (map.Visibility != MapVisibility.Shellmap && map.Visibility != 0)
				throw new InvalidDataException("Background map must not be offered as a playable lobby map.");
			var players = new MapPlayers(map.PlayerDefinitions).Players;
			if (players.Values.Any(p => p.Playable) || players["GDI"].Faction != "talon" || players["Soviets"].Faction != "russia")
				throw new InvalidDataException("Unexpected shellmap player layout.");
			foreach (var name in new[] { "GDI", "Soviets" })
				if (players[name].Bot != "normal" || !players[name].Enemies.Contains(name == "GDI" ? "Soviets" : "GDI"))
					throw new InvalidDataException(name + ": missing bot/enemy relationship.");
			if (!map.Rules.Actors["world"].HasTraitInfo<ShellmapBattleMonitorInfo>())
				throw new InvalidDataException("Missing shellmap-local bot driver.");
			var occupied = new Dictionary<CPos, string>();
			var factories = new List<(string Name, CPos Origin, ActorInfo Info)>();
			foreach (var node in map.ActorDefinitions)
			{
				if (!node.Key.StartsWith("GDI", StringComparison.Ordinal) && !node.Key.StartsWith("Soviets", StringComparison.Ordinal))
					continue;
				var actor = map.Rules.Actors[node.Value.Value.ToLowerInvariant()];
				_ = actor.TraitsInConstructOrder().ToArray();
				if (!actor.HasTraitInfo<BuildingInfo>())
					continue;
				var reference = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = reference.Get<LocationInit>().Value;
				var building = actor.TraitInfo<BuildingInfo>();
				foreach (var cell in building.Tiles(location))
				{
					if (!map.Contains(cell) || !building.TerrainTypes.Contains(map.GetTerrainInfo(cell).Type))
						throw new InvalidDataException(node.Key + ": invalid terrain at " + cell);
					if (occupied.ContainsKey(cell))
						throw new InvalidDataException(node.Key + " overlaps " + occupied[cell] + " at " + cell);
					occupied.Add(cell, node.Key);
				}
				factories.Add((node.Key, location, actor));
			}
			foreach (var factory in factories)
				foreach (var exit in factory.Info.TraitInfos<ExitInfo>())
					if (!map.Contains(factory.Origin + exit.ExitCell) ||
						(occupied.TryGetValue(factory.Origin + exit.ExitCell, out var obstruction) && obstruction != factory.Name))
						throw new InvalidDataException(factory.Name + ": obstructed production exit.");
			map.Sequences.LoadSprites();
			Console.WriteLine("Two Sides shellmap: GDI/Soviet bot factions, initial footprints/exits and complete sprite set passed.");
		}
	}
}
