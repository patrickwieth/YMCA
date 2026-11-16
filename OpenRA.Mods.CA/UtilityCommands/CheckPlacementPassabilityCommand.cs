using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckPlacementPassabilityCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-placement-passability";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || (args.Length == 2 && args[1] == "--all-sprites");
		[Desc("Check passable placement markers, invalid priority, airfield runway and Internet Center borders.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			foreach (var row in JArray.Parse(File.ReadAllText("art-source/bib-occupancy/ground-review/masks.json")))
			{
				var name = (string)row["actor"];
				var actor = mod.DefaultRules.Actors[name];
				var b = actor.TraitInfo<BuildingInfo>();
				var preview = actor.TraitInfo<FootprintPlaceBuildingPreviewInfo>();
				if (b.Footprint.Any(c => c.Value == FootprintCellType.OccupiedPassable || c.Value == FootprintCellType.OccupiedPassableTransitOnly) && preview.PassableSequence != "build-valid-passable") throw new InvalidDataException(name + ": passable preview not enabled.");
				foreach (var cell in b.Footprint)
				{
					var expected = cell.Value == FootprintCellType.OccupiedPassable || cell.Value == FootprintCellType.OccupiedPassableTransitOnly;
					if (FootprintPlaceBuildingPreviewPreview.IsPassableCell(b, cell.Key, PlaceBuildingCellType.Valid) != expected)
						throw new InvalidDataException(name + ": incorrect passable marker classification.");
					foreach (var state in new[] { PlaceBuildingCellType.Invalid, PlaceBuildingCellType.Invalid | PlaceBuildingCellType.Valid, PlaceBuildingCellType.LineBuild, PlaceBuildingCellType.None })
						if (FootprintPlaceBuildingPreviewPreview.IsPassableCell(b, cell.Key, state)) throw new InvalidDataException(name + ": invalid/line cell must not become pale green.");
				}
				if (FootprintPlaceBuildingPreviewPreview.IsPassableCell(b, new CVec(100, 100), PlaceBuildingCellType.Valid)) throw new InvalidDataException("Missing footprint cell classified as passable.");
			}

			var airfield = mod.DefaultRules.Actors["afld.gdi"];
			var building = airfield.TraitInfo<BuildingInfo>();
			if (building.Tiles(CPos.Zero).Count() != 20 || building.Footprint.Any(c => c.Key.X < 0 && c.Value != FootprintCellType.Empty))
				throw new InvalidDataException("Airfield still reserves the extra upper cells.");
			for (var y = -1; y <= 3; y++)
				if (building.Footprint[new CVec(3, y)] != (y == 1 ? FootprintCellType.Occupied : FootprintCellType.OccupiedPassable))
					throw new InvalidDataException("Airfield right border/radar footing mismatch.");
			for (var x = 0; x < 4; x++)
				if (building.Footprint[new CVec(x, 3)] != FootprintCellType.OccupiedPassable) throw new InvalidDataException("Airfield lower-left border blocked.");
			var exit = airfield.TraitInfos<ExitInfo>().Single();
			var landingX = 724 + 362 * (building.Dimensions.X - building.Dimensions.Y) + building.LocalCenterOffset.X + exit.SpawnOffset.X;
			var landingY = 362 * (building.Dimensions.X + building.Dimensions.Y) + building.LocalCenterOffset.Y + exit.SpawnOffset.Y;
			if (landingX != 1448 || landingY != 2896 || exit.ExitCell != new CVec(2, 1) || building.Footprint[exit.ExitCell] != FootprintCellType.OccupiedPassable)
				throw new InvalidDataException("Aircraft production/ReturnToBase landing is not at runway center2,1.");
			var internet = mod.DefaultRules.Actors["chinternetcenter"].TraitInfo<BuildingInfo>();
			if (internet.Tiles(CPos.Zero).Count() != 16 || internet.Footprint.Count(c => c.Value == FootprintCellType.Occupied) != 7 ||
				internet.Footprint[new CVec(-1, 1)] != FootprintCellType.OccupiedPassable || internet.Footprint[new CVec(1, -1)] != FootprintCellType.OccupiedPassable)
				throw new InvalidDataException("Internet Center extra side clearances mismatch.");

			foreach (var name in new[] { "hand", "sign" })
				if (mod.DefaultRules.Actors[name].TraitInfo<BuildingInfo>().Footprint.Count(c => c.Value == FootprintCellType.Occupied) != 4)
					throw new InvalidDataException(name + ": expected two-by-two solid building.");
			foreach (var name in new[] { "proc", "proc.scrin", "proc.td.nod", "weap.nod" })
				if (mod.DefaultRules.Actors[name].TraitInfo<BuildingInfo>().Tiles(CPos.Zero).Count() != (name == "weap.nod" ? 12 : 9))
					throw new InvalidDataException(name + ": invisible extra placement cell remains.");
			var alliedFactory = mod.DefaultRules.Actors["weap"].TraitInfo<BuildingInfo>();
			for (var x = 0; x < 4; x++)
				if (alliedFactory.Footprint[new CVec(x, 2)] != FootprintCellType.OccupiedPassable)
					throw new InvalidDataException("Allied factory four-cell lower-left border must be normally passable.");

			var overlay = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "overlay");
			var names = new[] { "Defaults", "build-valid", "build-valid-passable", "build-invalid" };
			var selected = new MiniYamlNode("overlay", new MiniYaml(null, overlay.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			var normal = sequences["build-valid"];
			var pale = sequences["build-valid-passable"];
			if (pale.Scale != normal.Scale || pale.ZOffset != normal.ZOffset || pale.GetAlpha(0) != .55f || pale.GetAlpha(0) >= normal.GetAlpha(0) || pale.GetSprite(0).Size != normal.GetSprite(0).Size)
				throw new InvalidDataException("Passable marker depth, size or opacity mismatch.");
			if (args.Contains("--all-sprites"))
			{
				using var all = new SequenceSet(mod.DefaultFileSystem, mod, "RUBBERDUCK-TEMPERATE", null);
				all.LoadSprites();
				Console.WriteLine("Full default sequence set loaded without missing sprite files.");
			}
			Console.WriteLine("Passable and service cells use pale markers; invalid cells retain red priority. Real marker sprites loaded at matching size/depth with55% relative opacity. Airfield border/runway landing and Internet Center side clearances passed.");
		}
	}
}
