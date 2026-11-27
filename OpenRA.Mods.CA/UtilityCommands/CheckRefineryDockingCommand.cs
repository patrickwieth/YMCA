using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckRefineryDockingCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-refinery-docking";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		public static readonly string[] Refineries = { "proc", "soviet_refinery", "proc.td", "proc.td.nod", "proc.scrin", "chsupply" };
		static bool Passable(BuildingInfo b, CVec c) => !b.Footprint.TryGetValue(c, out var type) ||
			type != FootprintCellType.Occupied && type != FootprintCellType.OccupiedUntargetable;
		static void CheckCell(BuildingInfo b, CVec cell, string label, bool external = false)
		{
			if (external)
			{
				if (b.Footprint.ContainsKey(cell)) throw new InvalidDataException(label + ": external bay lies inside the building site.");
				return;
			}

			if (!b.Footprint.TryGetValue(cell, out var reserved) || reserved == FootprintCellType.Empty || !Passable(b, cell)) throw new InvalidDataException(label + ": bay/spawn is not reserved and passable.");
			var seen = new HashSet<CVec>(); var todo = new Queue<CVec>(); todo.Enqueue(cell);
			while (todo.Count > 0)
			{
				var c = todo.Dequeue();
				if (!seen.Add(c) || !Passable(b, c)) continue;
				if (!b.Footprint.ContainsKey(c)) return;
				foreach (var d in new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) }) todo.Enqueue(c + d);
			}
			throw new InvalidDataException(label + ": no way out.");
		}
		public static void Check(ModData mod)
		{
			var helper = mod.DefaultRules.Actors["refinery.dockhelper"];
			if (helper.TraitInfo<DockHostInfo>().DockOffset != WVec.Zero || !Passable(helper.TraitInfo<BuildingInfo>(), CVec.Zero))
				throw new InvalidDataException("Auxiliary docks must not block their own service cells.");
			foreach (var name in Refineries)
			{
				var actor = mod.DefaultRules.Actors[name]; var b = actor.TraitInfo<BuildingInfo>();
				var external = name == "chsupply";
				var cell = external ? new CVec(2, 3) : new CVec(1, 2);
				var center = new WVec(362 * (b.Dimensions.X - b.Dimensions.Y), 362 * (b.Dimensions.X + b.Dimensions.Y - 2), 0) + b.LocalCenterOffset;
				var expected = new WVec(724 * (cell.X - cell.Y), 724 * (cell.X + cell.Y), 0);
				var dock = actor.TraitInfo<DockHostInfo>();
				if (center + dock.DockOffset != expected || dock.IsDragRequired)
					throw new InvalidDataException(name + ": dock is not centered on the service cell or still drags off it.");
				CheckCell(b, cell, name, external);
				var bays = new HashSet<CVec> { cell };
				foreach (var spawn in actor.TraitInfos<SpawnNeighboringActorsInfo>().Where(s => s.ActorTypes.Contains("refinery.dockhelper")))
					foreach (var location in spawn.Locations)
					{
						CheckCell(b, location, name + " helper", external);
						if (!bays.Add(location)) throw new InvalidDataException(name + ": overlapping docks.");
					}
				foreach (var spawn in actor.TraitInfos<FreeActorInfo>().Where(s => mod.DefaultRules.Actors[s.Actor.ToLowerInvariant()].HasTraitInfo<HarvesterInfo>()))
					CheckCell(b, spawn.SpawnOffset, name + " free harvester", external);
				if (external && (b.Dimensions.X != 3 || b.Dimensions.Y != 3 ||
					!bays.SetEquals(new[] { new CVec(2, 3), new CVec(1, 3), new CVec(0, -1), new CVec(1, -1) })))
					throw new InvalidDataException("China requires a 3x3 site and two separate external bays on each side.");
				Console.WriteLine(name + ": main=" + cell + ", reachable bays=" + bays.Count + ", free spawns passable.");
			}
		}
		[Desc("Check resolved isometric refinery docks, auxiliary bays and free harvester spawns.")]
		void IUtilityCommand.Run(Utility utility, string[] args) { Game.ModData = utility.ModData; Check(utility.ModData); }
	}
}
