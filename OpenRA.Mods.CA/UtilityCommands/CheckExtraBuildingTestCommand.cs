using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckExtraBuildingTestCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-extra-building-test";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 2;
		[Desc("Validate China/Scrin display buildings, sprite loads, terrain and non-overlapping map footprints.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			using var package = new Folder(Platform.EngineDir).OpenPackage(Path.GetFullPath(args[1]), mod.ModFiles);
			using var map = new Map(mod, package);
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).ToDictionary(n => n.Key);
			var positions = new Dictionary<CPos, List<string>>();
			var added = new List<(string Id, string Name, string Faction, ActorInfo Actor, CPos Location, CPos[] Cells)>();
			foreach (var node in map.ActorDefinitions)
			{
				var actor = map.Rules.Actors[node.Value.Value.ToLowerInvariant()];
				if (!actor.HasTraitInfo<BuildingInfo>()) continue;
				var reference = new ActorReference(node.Value.Value, node.Value.ToDictionary());
				var location = reference.Get<LocationInit>().Value;
				var building = actor.TraitInfo<BuildingInfo>();
				var cells = building.OccupiedTiles(location).ToArray();
				if (node.Value.Value.Equals("spen", StringComparison.OrdinalIgnoreCase) || node.Value.Value.Equals("chsyrd", StringComparison.OrdinalIgnoreCase))
				{
					var expectedImage = node.Value.Value.Equals("chsyrd", StringComparison.OrdinalIgnoreCase) ? "chsyrd.supplied" : "spen.soviet";
					if (actor.TraitInfo<RenderSpritesInfo>().Image != expectedImage)
						throw new InvalidDataException(node.Key + " must use " + expectedImage + ".");
					if (cells.Any(c => !map.Contains(c) || map.GetTerrainInfo(c).Type != "Water"))
						throw new InvalidDataException(node.Key + " shipyard lacks water.");
				}
				foreach (var cell in cells)
				{
					if (!positions.TryGetValue(cell, out var entries)) positions[cell] = entries = new List<string>();
					entries.Add(node.Key);
				}
				if (node.Key.StartsWith("TestChina", StringComparison.Ordinal) || node.Key.StartsWith("TestScrin", StringComparison.Ordinal))
					added.Add((node.Key, node.Value.Value, reference.Get<FactionInit>().Value, actor, location, cells));
			}
			if (added.Count(a => a.Id.StartsWith("TestChina", StringComparison.Ordinal)) != 18 || added.Count(a => a.Id.StartsWith("TestScrin", StringComparison.Ordinal)) != 17)
				throw new InvalidDataException("Missing China/Scrin building displays.");
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var item in added)
			{
				_ = item.Actor.TraitsInConstructOrder().ToArray();
				var building = item.Actor.TraitInfo<BuildingInfo>();
				if (item.Location.X + item.Location.Y >= 76) throw new InvalidDataException(item.Id + " is not above the existing displays.");
				foreach (var cell in item.Cells)
				{
					if (!map.Contains(cell)) throw new InvalidDataException(item.Id + " outside playable map.");
					if (positions[cell].Count > 1) throw new InvalidDataException(item.Id + " overlaps " + string.Join(",", positions[cell]));
					if (!building.TerrainTypes.Contains(map.GetTerrainInfo(cell).Type)) throw new InvalidDataException(item.Id + " invalid terrain at " + cell);
				}
				var image = item.Actor.TraitInfo<RenderSpritesInfo>().GetImage(item.Actor, item.Faction);
				var node = nodes[image];
				var names = new[] { "Defaults", "idle", "damaged-idle", "turret", "damaged-turret" };
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, map.Tileset, cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
				if (!sequences.ContainsKey("idle")) throw new InvalidDataException(item.Name + " missing idle sprite.");
				var china = item.Id.StartsWith("TestChina", StringComparison.Ordinal);
				var withoutBib = china
					? item.Name.StartsWith("chbunker", StringComparison.OrdinalIgnoreCase) || item.Name.Equals("chsyrd", StringComparison.OrdinalIgnoreCase)
					: item.Name.Equals("swal", StringComparison.OrdinalIgnoreCase);
				var bibs = item.Actor.TraitInfos<WithIdleOverlayInfo>()
					.Where(t => t.Image != null && (t.Image.StartsWith("sovietbib", StringComparison.Ordinal) ||
						t.Image.StartsWith("scrinbib", StringComparison.Ordinal))).ToArray();
				if (item.Actor.HasTraitInfo<WithBuildingBibInfo>())
					throw new InvalidDataException(item.Name + ": legacy bib still enabled.");
				if (bibs.Length != (withoutBib ? 0 : 1))
					throw new InvalidDataException(item.Name + ": expected " + (withoutBib ? "no decorative bib" : "exactly one decorative bib") +
						", found " + bibs.Length + ".");
				foreach (var bib in bibs)
				{
					var prefix = china ? "sovietbib" : "scrinbib";
					if (!bib.Image.StartsWith(prefix, StringComparison.Ordinal))
						throw new InvalidDataException(item.Name + ": expected " + prefix + ", found " + bib.Image + ".");
					var bibNode = nodes[bib.Image];
					var bibSequences = mod.SpriteSequenceLoader.ParseSequences(mod, map.Tileset, cache, bibNode);
					cache.LoadReservations(mod);
					bibSequences[bib.Sequence].ResolveSprites(cache);
				}
			}
			var oilNodes = map.ActorDefinitions.Where(n => n.Key == "TestOilPlatform" && n.Value.Value.Equals("oil_platform", StringComparison.OrdinalIgnoreCase)).ToArray();
			if (oilNodes.Length != 1) throw new InvalidDataException("Expected one oil-platform display.");
			var oil = new ActorReference("oil_platform", oilNodes[0].Value.ToDictionary());
			var oilLocation = oil.Get<LocationInit>().Value;
			var oilInfo = map.Rules.Actors["oil_platform"].TraitInfo<BuildingInfo>();
			for (var y = -1; y <= oilInfo.Dimensions.Y; y++) for (var x = -1; x <= oilInfo.Dimensions.X; x++)
			{
				var cell = oilLocation + new CVec(x, y);
				if (!map.Contains(cell) || map.GetTerrainInfo(cell).Type != "Water")
					throw new InvalidDataException("Oil platform needs a complete water footprint and border.");
				if (positions.TryGetValue(cell, out var occupants) && occupants.Any(id => id != "TestOilPlatform"))
					throw new InvalidDataException("Oil platform pool overlaps another building.");
			}
			var oilNode = nodes["oil_platform"];
			var oilStates = new MiniYamlNode(oilNode.Key, new MiniYaml(null, oilNode.Value.Nodes.Where(n => new[] { "Defaults", "idle", "damaged-idle" }.Contains(n.Key))));
			var oilSequences = mod.SpriteSequenceLoader.ParseSequences(mod, map.Tileset, cache, oilStates);
			cache.LoadReservations(mod);
			foreach (var sequence in oilSequences.Values) sequence.ResolveSprites(cache);
			Console.WriteLine("Oil Platform: body sprites loaded; separate 4x4 water footprint plus border, no overlaps.");
			Console.WriteLine("China18 / Scrin17: actual map/rules/sprites loaded; all new footprints inside bounds, on valid terrain and non-overlapping. Live review pending.");
		}
	}
}
