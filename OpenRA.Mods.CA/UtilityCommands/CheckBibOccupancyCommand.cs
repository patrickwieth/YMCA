using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.CA.Traits.Render;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckBibOccupancyCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-bib-occupancy";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		static bool HasWayOut(BuildingInfo b, CVec start)
		{
			var visited = new HashSet<CVec>();
			var queue = new Queue<CVec>();
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				var cell = queue.Dequeue();
				if (!visited.Add(cell)) continue;
				if (!b.Footprint.TryGetValue(cell, out var type)) return true;
				if (type == FootprintCellType.Occupied || type == FootprintCellType.OccupiedUntargetable) continue;
				foreach (var delta in new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) })
					queue.Enqueue(cell + delta);
			}
			return false;
		}

		[Desc("Check reviewed building ground masks, reserved bibs, exits, service routes and fixed actor centers.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			foreach (var e in JArray.Parse(File.ReadAllText("art-source/bib-occupancy/ground-review/masks.json")))
			{
				var name = (string)e["actor"];
				var actor = mod.DefaultRules.Actors[name];
				_ = actor.TraitsInConstructOrder().ToArray();
				var b = actor.TraitInfo<BuildingInfo>();
				var tiles = b.Tiles(CPos.Zero).ToHashSet();
				foreach (var cell in e["bib_cells"])
					if (!tiles.Contains(new CPos((int)cell[0], (int)cell[1])))
						throw new InvalidDataException(name + ": unreserved bib cell.");
				var oldDims = e["old_dimensions"].Values<int>().ToArray();
				var oldCenter = e["old_center_offset"].Values<int>().ToArray();
				if (362 * (b.Dimensions.X - b.Dimensions.Y) + b.LocalCenterOffset.X != 362 * (oldDims[0] - oldDims[1]) + oldCenter[0] ||
					362 * (b.Dimensions.X + b.Dimensions.Y) + b.LocalCenterOffset.Y != 362 * (oldDims[0] + oldDims[1]) + oldCenter[1])
					throw new InvalidDataException(name + ": actor/art center moved during footprint expansion.");
				var expected = e["cells"].ToDictionary(p => new CVec((int)p["X"], (int)p["Y"]), p => (FootprintCellType)((string)p["Type"])[0]);
				// Explicitly reserve the fourth Chinese service bay, without changing any body cells.
				if (name == "chsupply") expected[new CVec(3, 1)] = FootprintCellType.OccupiedPassable;
				var actual = b.Footprint.Where(p => p.Value != FootprintCellType.Empty).ToDictionary(p => p.Key, p => p.Value);
				if (expected.Count != actual.Count || expected.Any(p => !actual.TryGetValue(p.Key, out var value) || value != p.Value))
					throw new InvalidDataException(name + ": resolved mask differs from individually reviewed cells.");
				foreach (var dock in e["dock_cells"])
					if (b.Footprint.TryGetValue(new CVec((int)dock[0], (int)dock[1]), out var dockType) &&
						(dockType == FootprintCellType.Occupied || dockType == FootprintCellType.OccupiedUntargetable))
						throw new InvalidDataException(name + ": docking destination blocked.");
				var exits = actor.TraitInfos<ExitInfo>().Select(x => string.Join(",", x.ExitCell.X, x.ExitCell.Y, x.SpawnOffset.X, x.SpawnOffset.Y, x.SpawnOffset.Z));
				var oldExits = e["exits"].Select(x => string.Join(",", x["Cell"].Concat(x["Spawn"]).Values<int>()));
				if (!exits.SequenceEqual(oldExits)) throw new InvalidDataException(name + ": production exit moved.");
				var groundProducers = new[] { "tent", "barr", "pyle", "hand", "port", "bio.nod", "weap", "weap.td", "weap.nod", "weap.soviet", "wsph", "chbarracks", "chcommand", "chcommand.nukular", "chweap", "syrd", "syrd.gdi", "spen", "spen.nod", "chsyrd", "kenn" };
				if (groundProducers.Contains(name) && !actor.TraitInfos<ExitInfo>().Any(x => HasWayOut(b, x.ExitCell)))
					throw new InvalidDataException(name + ": no passable unit exit route.");
				foreach (var service in b.Footprint.Where(x => x.Value == FootprintCellType.OccupiedPassableTransitOnly))
					if (!HasWayOut(b, service.Key)) throw new InvalidDataException(name + ": service/delivery cell enclosed by walls.");
				var docks = actor.TraitInfos<DockHostInfo>().Select(x => string.Join(",", x.DockOffset.X, x.DockOffset.Y, x.DockOffset.Z));
				if (!CheckRefineryDockingCommand.Refineries.Contains(name) && !docks.SequenceEqual(e["docks"].Select(x => string.Join(",", x.Values<int>()))))
					throw new InvalidDataException(name + ": docking offset moved.");
			}
			foreach (var name in new[] { "sfac", "sfac.nukular", "reac", "rea2", "proc.scrin", "port", "wsph", "nerv", "scrt", "grav", "sign", "srep", "silo.scrin", "mani", "rift", "ptur", "shar", "scol", "scol.temp" })
			{
				var actor = mod.DefaultRules.Actors[name];
				if (!actor.TraitInfo<RenderSpritesInfo>().HideBodyWhileBuilding || actor.TraitInfos<WithIdleOverlayInfo>().Single(x => x.Image?.StartsWith("scrinbib", StringComparison.Ordinal) == true).RequiresCondition != null)
					throw new InvalidDataException(name + ": expected immediate bib and presentation-only construction hiding.");
			}
			foreach (var height in new[] { 24, 48, 96, 128 })
			{
				var bounds = IsometricSelectionDecorationsForUnits.ProjectBounds(new Rectangle(-45, -22, 90, 44), new Rectangle(-32, -height + 12, 64, height), 24);
				if (bounds.BoundingRect.Height < height || bounds.BoundingRect.Width < 64 || bounds.Vertices[1].Y >= bounds.Vertices[2].Y)
					throw new InvalidDataException("Unit selection prism did not fit the body.");
			}
			CheckRefineryDockingCommand.Check(mod);
			Console.WriteLine("Reviewed ground masks, bib reservations, actor centers, unchanged production exits, reachable refinery docks, Scrin bib-first policy and body-fitted unit prisms checked.");
		}
	}
}
