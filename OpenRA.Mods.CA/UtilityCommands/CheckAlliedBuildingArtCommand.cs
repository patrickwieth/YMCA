using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.HitShapes;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-allied-building-art", "Check Allied PNG bindings, foundation overlays and preserved footprints/power values.")]
	public sealed class CheckAlliedBuildingArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-allied-building-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			foreach (var entry in new[]
			{
				(Actor: "pbox", Image: "pbox.allies", Bib: "alliedbib1x1", X: 1, Y: 1),
				(Actor: "pris", Image: "pris.allies", Bib: "alliedbib1x1", X: 1, Y: 1),
				(Actor: "powr", Image: "powr.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "apwr", Image: "apwr.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "tent", Image: "tent.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "gap", Image: "gap.allies", Bib: "alliedbib2x2", X: 2, Y: 2),
				(Actor: "proc", Image: "proc.allies", Bib: "alliedbib3x3", X: 3, Y: 4),
				(Actor: "orep", Image: "orep.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "pdox", Image: "pdox.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "weat", Image: "weat.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "atek", Image: "atek.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "hpad", Image: "hpad.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "dome", Image: "dome.allies", Bib: "alliedbib3x3", X: 3, Y: 3),
				(Actor: "fix", Image: "fix.allies", Bib: "alliedbib3x3", X: 3, Y: 3)
			})
			{
				var actor = rules.Actors[entry.Actor];
				var building = actor.TraitInfo<BuildingInfo>();
				if (building.Dimensions.X != entry.X || building.Dimensions.Y != entry.Y || actor.TraitInfo<RenderSpritesInfo>().Image != entry.Image)
					throw new InvalidDataException(entry.Actor + ": footprint or image mismatch.");
				var bib = actor.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == entry.Bib);
				if (!bib.IsDecoration || bib.RequiresCondition?.Expression != "!build-incomplete" || actor.HasTraitInfo<WithBuildingBibInfo>())
					throw new InvalidDataException(entry.Actor + ": incorrect or duplicated foundation.");
				var selection = actor.TraitInfo<IsometricSelectableInfo>();
				if (entry.Actor == "tent")
				{
					if (selection.Bounds != null || selection.DecorationBounds != null || selection.DecorationHeight != 64)
						throw new InvalidDataException("Barracks selection must follow the new 3x2 occupied body.");
					continue;
				}

				var size = entry.Actor == "proc" || entry.Actor == "fix" ? 3 : entry.Bib == "alliedbib1x1" ? 1 : 2;
				var expectedBounds = entry.Actor == "proc" ? new[] { 4344, 4163, 0, -91 } :
					new[] { "powr", "apwr" }.Contains(entry.Actor) ? new[] { 2896, 2896, 0, -181 } : new[] { size * 1448, size * 1448 };
				if (selection.DecorationBounds == null || !selection.DecorationBounds.SequenceEqual(expectedBounds) || selection.DecorationHeight <= 0)
					throw new InvalidDataException(entry.Actor + ": selection decoration must match the intended building body.");
			}

			foreach (var name in new[] { "powr.soviet", "apwr.soviet", "tpwr", "weap.soviet", "fact.soviet", "fact.nukular.soviet", "dome.soviet", "hpad.soviet", "fix.soviet" })
				if (rules.Actors[name].TraitInfos<WithIdleOverlayInfo>().Any(i => i.Image?.StartsWith("alliedbib", StringComparison.Ordinal) == true))
					throw new InvalidDataException(name + ": Allied foundation leaked into a Soviet derivative.");

			foreach (var name in new[] { "powr", "apwr", "orep", "weat", "atek", "hpad", "dome", "pdox" })
			{
				var actor = rules.Actors[name];
				var building = actor.TraitInfo<BuildingInfo>();
				for (var y = 0; y < 3; y++)
					for (var x = 0; x < 3; x++)
						if (building.Footprint[new CVec(x, y)] != (x < 2 && y < 2 ? FootprintCellType.Occupied : FootprintCellType.OccupiedPassable))
							throw new InvalidDataException(name + ": both front bib sides must be passable.");
				if (building.OccupiedTiles(CPos.Zero).Count() != 4 || building.PathableTiles(CPos.Zero).Count() != 5 ||
					building.LocalCenterOffset != new WVec(0, -724, 0) ||
					actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(
						actor.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "alliedbib3x3").Offset.Rotate(WRot.None)) != new WVec(0, 724, 0))
					throw new InvalidDataException(name + ": incorrect pathable bib/body registration.");
				if (actor.TraitInfo<HitShapeInfo>().Type is not RectangleShape shape ||
					shape.TopLeft != new int2(-1024, -1024) || shape.BottomRight != new int2(1024, 1024))
					throw new InvalidDataException(name + ": collision shape must match the 2x2 body.");
			}

			foreach (var entry in new[] { (Name: "powr.soviet", X: 2, Y: 2), (Name: "apwr.soviet", X: 4, Y: 4),
				(Name: "tpwr", X: 4, Y: 4), (Name: "hpad.soviet", X: 2, Y: 3), (Name: "dome.soviet", X: 2, Y: 3) })
				if (rules.Actors[entry.Name].TraitInfo<BuildingInfo>().Dimensions != new CVec(entry.X, entry.Y))
					throw new InvalidDataException(entry.Name + ": Soviet site must remain unchanged.");
			if (rules.Actors["hpad.soviet"].TraitInfo<RenderSpritesInfo>().Image != "hpad.soviet")
				throw new InvalidDataException("Soviet Helipad must keep its native image.");

			var barracks = rules.Actors["tent"];
			var barracksBuilding = barracks.TraitInfo<BuildingInfo>();
			if (barracksBuilding.Dimensions != new CVec(3, 3) || barracksBuilding.LocalCenterOffset != new WVec(362, -362, 0) ||
				barracksBuilding.OccupiedTiles(CPos.Zero).Count() != 6 || barracksBuilding.PathableTiles(CPos.Zero).Count() != 3 ||
				barracks.TraitInfo<BodyOrientationInfo>().LocalToWorld(
					barracks.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "alliedbib3x3").Offset.Rotate(WRot.None)) != new WVec(-362, 362, 0) ||
				barracks.TraitInfo<HitShapeInfo>().Type is not RectangleShape barracksShape ||
				barracksShape.TopLeft != new int2(-1536, -1024) || barracksShape.BottomRight != new int2(1536, 1024))
				throw new InvalidDataException("Barracks requires a 3x2 body/collision on its unchanged 3x3 bib.");
			for (var y = 0; y < 3; y++)
				for (var x = 0; x < 3; x++)
					if (barracksBuilding.Footprint[new CVec(x, y)] != (y < 2 ? FootprintCellType.Occupied : FootprintCellType.OccupiedPassable))
						throw new InvalidDataException("Barracks front row must stay driveable.");
			if (barracks.TraitInfos<ExitInfo>().Any(e => e.ExitCell.Y != 2 || e.ExitCell.X < 0 || e.ExitCell.X >= 3))
				throw new InvalidDataException("Barracks exits must remain in the driveable front row.");

			var airfield = rules.Actors["afld.allies"];
			var airfieldBuilding = airfield.TraitInfo<BuildingInfo>();
			if (airfieldBuilding.Dimensions != new CVec(4, 3) || airfieldBuilding.OccupiedTiles(CPos.Zero).Count() != 6 ||
				airfieldBuilding.PathableTiles(CPos.Zero).Count() != 6 || airfieldBuilding.LocalCenterOffset != new WVec(0, -724, 0) ||
				airfield.TraitInfo<BodyOrientationInfo>().LocalToWorld(
					airfield.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "alliedbib4x3").Offset.Rotate(WRot.None)) != new WVec(0, 724, 0) ||
				airfield.TraitInfo<IsometricSelectableInfo>().DecorationBounds != null ||
				airfield.TraitInfo<IsometricSelectableInfo>().Bounds != null ||
				airfield.TraitInfo<IsometricSelectableInfo>().DecorationFootprint != new CVec(4, 2) ||
				airfield.TraitInfo<IsometricSelectableInfo>().DecorationFootprintExtraDepth != 181)
				throw new InvalidDataException("Allied airfield must have a 4x3 bib, unchanged 3x2 body and six passable apron cells.");
			for (var y = 0; y < 3; y++)
				for (var x = 0; x < 4; x++)
					if (airfieldBuilding.Footprint[new CVec(x, y)] != (x < 3 && y < 2 ? FootprintCellType.Occupied : FootprintCellType.OccupiedPassable))
						throw new InvalidDataException("Airfield apron must not block movement.");

			var factory = rules.Actors["weap"];
			var footprint = factory.TraitInfo<BuildingInfo>();
			if (footprint.Dimensions != new CVec(4, 3) || footprint.LocalCenterOffset != new WVec(362, -362, 0) ||
				factory.TraitInfo<RenderSpritesInfo>().Image != "weap.allies" ||
				factory.TraitInfo<BodyOrientationInfo>().LocalToWorld(
					factory.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "alliedbib4x3").Offset.Rotate(WRot.None)) != new WVec(-362, 362, 0) ||
				factory.TraitInfos<WithBuildingBibInfo>().Any())
				throw new InvalidDataException("Allied factory must have its 4x3 bib and unchanged rear 4x2 body pivot.");
			for (var y = 0; y < 3; y++)
				for (var x = 0; x < 4; x++)
					if (footprint.Footprint[new CVec(x, y)] != (y >= 2 ? FootprintCellType.OccupiedPassable : FootprintCellType.Occupied))
						throw new InvalidDataException("Factory must have two solid rear rows and one driveable apron row.");
			foreach (var entry in new[] {
				(Name: "fact", Center: new WVec(-362, -1086, 0), Bib: new WVec(362, 1086, 0), Solid: 9, Passable: 7),
				(Name: "fact.nukular", Center: new WVec(-362, -1086, 0), Bib: new WVec(362, 1086, 0), Solid: 9, Passable: 7)
			})
			{
				var actor = rules.Actors[entry.Name];
				var building = actor.TraitInfo<BuildingInfo>();
				var bib = actor.TraitInfos<WithIdleOverlayInfo>().Single(i => i.Image == "alliedbib4x4");
				if (building.Dimensions != new CVec(4, 4) || building.LocalCenterOffset != entry.Center ||
					building.OccupiedTiles(CPos.Zero).Count() != entry.Solid || building.PathableTiles(CPos.Zero).Count() != entry.Passable ||
					actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(bib.Offset.Rotate(WRot.None)) != entry.Bib ||
					!bib.IsDecoration || actor.TraitInfos<WithBuildingBibInfo>().Any())
					throw new InvalidDataException(entry.Name + ": 4x4 bib, passable apron or preserved body registration is incorrect.");
			}

			var conyard = rules.Actors["fact"].TraitInfo<BuildingInfo>();
			for (var y = 0; y < 4; y++)
				for (var x = 0; x < 4; x++)
				{
					var expected = x == 3 || y == 3 ? FootprintCellType.OccupiedPassable :
						(x == 2 && y == 0) || (y == 2 && x != 1) ? FootprintCellType.OccupiedUntargetable : FootprintCellType.Occupied;
					if (conyard.Footprint[new CVec(x, y)] != expected)
						throw new InvalidDataException("Conyard must preserve its original targetable 3x3 body.");
				}
			foreach (var name in new[] { "fact.soviet", "fact.nukular.soviet" })
			{
				var actor = rules.Actors[name];
				if (actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(3, 4) ||
					actor.TraitInfo<BuildingInfo>().LocalCenterOffset != new WVec(0, -724, 0) ||
					actor.HasTraitInfo<WithBuildingBibInfo>() ||
					!actor.TraitInfos<WithIdleOverlayInfo>().Any(i => i.Image == "sovietbib4x4"))
					throw new InvalidDataException(name + ": Soviet Conyard must preserve its footprint and use only the Soviet PNG bib.");
			}

			var factorySelection = factory.TraitInfo<IsometricSelectableInfo>();
			if (factorySelection.Bounds != null || factorySelection.DecorationBounds != null)
				throw new InvalidDataException("Factory selection must follow occupied cells, not its exit strip.");
			var exit = factory.TraitInfos<ExitInfo>().Single().ExitCell;
			if (exit.Y != 2 || exit.X < 0 || exit.X >= 4)
				throw new InvalidDataException("Factory exit must be in the front strip.");
			if (rules.Actors["weap.soviet"].TraitInfo<BuildingInfo>().Dimensions != new CVec(3, 3))
				throw new InvalidDataException("Soviet factory footprint must not change.");
			foreach (var entry in new[] { (Name: "powr", Height: 69), (Name: "fact", Height: 60),
				(Name: "fact.nukular", Height: 60), (Name: "fact.soviet", Height: 125), (Name: "fact.nukular.soviet", Height: 125),
				(Name: "proc", Height: 55), (Name: "afld.allies", Height: 83), (Name: "syrd", Height: 60),
				(Name: "pdox", Height: 105), (Name: "gap", Height: 183), (Name: "atek", Height: 108),
				(Name: "fix", Height: 38), (Name: "weap", Height: 76), (Name: "pris", Height: 171),
				(Name: "soviet_refinery", Height: 174), (Name: "fix.soviet", Height: 211),
				(Name: "weap.soviet", Height: 95), (Name: "chsyrd", Height: 30) })
				if (rules.Actors[entry.Name].TraitInfo<IsometricSelectableInfo>().DecorationHeight != entry.Height)
					throw new InvalidDataException(entry.Name + ": requested selection height or Soviet isolation changed.");
			if (rules.Actors["pbox"].TraitInfo<IsometricSelectableInfo>().DecorationHeight != 76)
				throw new InvalidDataException("Bunker selection box must remain unchanged when moving its art.");

			if (rules.Actors["powr"].TraitInfo<PowerInfo>().Amount != 100 || rules.Actors["apwr"].TraitInfo<PowerInfo>().Amount != 200)
				throw new InvalidDataException("Power output changed.");

			if (rules.Actors["apwr"].TraitInfo<IsometricSelectableInfo>().DecorationHeight != 90 ||
				rules.Actors["proc"].TraitInfo<IsometricSelectableInfo>().DecorationHeight != 55 ||
				rules.Actors["syrd"].TraitInfo<IsometricSelectableInfo>().DecorationHeight != 60)
				throw new InvalidDataException("Reviewed selection heights changed unexpectedly.");

			foreach (var name in new[] { "fact", "fact.nukular", "proc", "weap", "pbox", "pris", "powr", "apwr", "tent", "gap", "orep", "pdox", "weat", "atek", "hpad", "dome", "fix", "syrd", "afld.allies" })
			{
				var shadow = rules.Actors[name].TraitInfos<WithShadowInfo>().Single();
				if (shadow.Offset != new WVec(0, 90, 0) || shadow.ZOffset != -2048 ||
					shadow.ShadowColor.A != 112 || shadow.ShadowColor.R != 0 || shadow.ShadowColor.G != 0 || shadow.ShadowColor.B != 0 ||
					shadow.RequiresCondition?.Expression != "!build-incomplete")
					throw new InvalidDataException(name + ": shadow must be short, straight down, below the body and construction-gated.");
			}
			foreach (var name in new[] { "powr.soviet", "apwr.soviet", "tpwr", "dome.soviet", "hpad.soviet", "fact.soviet", "fact.nukular.soviet", "weap.soviet", "soviet_refinery", "fix.soviet", "chsyrd" })
				if (rules.Actors[name].HasTraitInfo<WithShadowInfo>())
					throw new InvalidDataException(name + ": Allied PNG shadow must not leak into non-Allied derivatives.");

			var sequences = MiniYaml.Load(utility.ModData.DefaultFileSystem, utility.ModData.Manifest.Sequences, null)
				.ToDictionary(n => n.Key, n => n.Value);
			foreach (var entry in new[]
			{
				(Image: "pbox.allies", File: "bunker.png"),
				(Image: "pris.allies", File: "prism tower.png"),
				(Image: "powr.allies", File: "small fusion power.png"),
				(Image: "apwr.allies", File: "fusion power 1.png"),
				(Image: "tent.allies", File: "barracks.png"),
				(Image: "weap.allies", File: "war factory2.png"),
				(Image: "orep.allies", File: "purifier.png"),
				(Image: "gap.allies", File: "euro_forces_gap_gene...-1070714859-0.png"),
				(Image: "proc.allies", File: "refinery.png"),
				(Image: "pdox.allies", File: "chronosphere.png"),
				(Image: "weat.allies", File: "weather control.png"),
				(Image: "atek.allies", File: "tech center.png"),
				(Image: "hpad.allies", File: "helipad.png"),
				(Image: "dome.allies", File: "radar.png")
			})
				foreach (var state in entry.Image == "atek.allies" ? new[] { "idle", "damaged-idle", "active", "damaged-active", "false-active", "damaged-false-active" } :
					new[] { "pris.allies", "pdox.allies", "weat.allies", "hpad.allies" }.Contains(entry.Image) ? new[] { "idle", "damaged-idle", "active", "damaged-active" } : new[] { "idle", "damaged-idle" })
				{
					var values = sequences[entry.Image].Nodes.Single(n => n.Key == state).Value.ToDictionary();
					var expected = "bits/allies/buildings/" + entry.File;
					if (values["Filename"].Value != expected || values["Anchor"].Value != "Bottom" ||
						values["Start"].Value != "0" || values["Length"].Value != "1" ||
						!utility.ModData.DefaultFileSystem.Exists("ca|" + expected))
						throw new InvalidDataException(entry.Image + "." + state + ": invalid PNG binding.");
				}

			foreach (var entry in new[]
			{
				(Image: "weap.allies", Offset: "-10,17"),
				(Image: "fact", Offset: "40,-3"), (Image: "fact.soviet", Offset: "-39,-54"),
				(Image: "pris.allies", Offset: "0,-6"), (Image: "powr.allies", Offset: "0,38"),
				(Image: "apwr.allies", Offset: "0,29"), (Image: "proc.allies", Offset: "0,-5"),
				(Image: "tent.allies", Offset: "4,-7"), (Image: "gap.allies", Offset: "5,-21"),
				(Image: "pdox.allies", Offset: "-4,35"), (Image: "hpad.allies", Offset: "0,7"),
				(Image: "dome.allies", Offset: "0,17"), (Image: "atek.allies", Offset: "0,32"),
				(Image: "weat.allies", Offset: "4,15"), (Image: "fix.allies", Offset: "6,-45"),
				(Image: "orep.allies", Offset: "0,32"),
				(Image: "syrd.allies", Offset: "0,-52"), (Image: "afld.allies.custom", Offset: "-20,-20")
			})
				foreach (var state in sequences[entry.Image].Nodes.Where(n => new[] { "idle", "damaged-idle", "active", "damaged-active", "false-active", "damaged-false-active" }.Contains(n.Key) || (entry.Image == "weap.allies" && n.Key == "place")))
					if (state.Value.ToDictionary()["Offset"].Value != entry.Offset)
						throw new InvalidDataException(entry.Image + "." + state.Key + ": final placement override was not applied.");

			foreach (var size in new[] { "1x1", "2x2", "3x3", "4x3", "4x4" })
			{
				var bib = sequences["alliedbib" + size].Nodes.Single(n => n.Key == "idle").Value.ToDictionary();
				if (bib["ZOffset"].Value != "-4096" || !utility.ModData.DefaultFileSystem.Exists("ca|" + bib["Filename"].Value))
					throw new InvalidDataException("Missing or incorrectly layered Allied bib: " + size);
			}
			foreach (var image in sequences)
				foreach (var sequence in image.Value.Nodes)
				{
					var values = sequence.Value.ToDictionary();
					if (values.TryGetValue("Filename", out var file) && file.Value.StartsWith("bits/allies/buildings/", StringComparison.Ordinal) &&
						values.TryGetValue("Scale", out var scale) && scale.Value != "1")
						throw new InvalidDataException(image.Key + "." + sequence.Key + ": Allied building PNGs must use scale 1.");
				}

			foreach (var name in new[] { "silo", "silo.soviet", "silo.td", "silo.scrin" })
				if (!rules.Actors[name].TraitInfo<BuildableInfo>().Prerequisites.Contains("~disabled"))
					throw new InvalidDataException(name + ": resource silos must be disabled for all factions.");
			foreach (var name in new[] { "mslo", "mslo.nod", "chsilo" })
				if (rules.Actors[name].TraitInfo<BuildableInfo>().Prerequisites.Contains("~disabled"))
					throw new InvalidDataException(name + ": missile/nuclear silos are not resource silos.");
			if (rules.Actors["dome"].TraitInfo<TooltipInfo>().Name != "Radar Station" ||
				rules.Actors["dome.soviet"].TraitInfo<TooltipInfo>().Name != "Radar Dome")
				throw new InvalidDataException("Only the Allied Radar is renamed to Radar Station.");

			if (!rules.Actors["gun"].TraitInfo<BuildableInfo>().Prerequisites.Contains("~disabled") ||
				rules.Actors["gun.nod"].TraitInfo<BuildableInfo>().Prerequisites.Contains("~disabled"))
				throw new InvalidDataException("Only the Allied Turret must be disabled, not the Nod derivative.");
			Console.WriteLine("Allied building art checks passed: supplied PNGs, separate bibs, damage-state bindings, approved footprints/exit strip and unchanged power output.");
		}
	}
}
