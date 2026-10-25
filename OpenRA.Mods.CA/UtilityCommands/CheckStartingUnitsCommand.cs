using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-starting-units", "Validate starting actors, placement traits and all faction/random-faction starting-class combinations.")]
	public sealed class CheckStartingUnitsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-starting-units";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			var world = rules.Actors[SystemActors.World];
			var groups = world.TraitInfos<StartingUnitsInfo>().ToArray();
			var locomotors = new HashSet<string>(world.TraitInfos<LocomotorInfo>().Select(l => l.Name));
			var failures = new List<string>();
			var references = 0;
			foreach (var group in groups)
			{
				var context = group.Class + "/" + string.Join(",", group.Factions.OrderBy(f => f));
				void Check(string name, string role)
				{
					references++;
					if (!rules.Actors.TryGetValue(name.ToLowerInvariant(), out var actor) || name.StartsWith("^", StringComparison.Ordinal))
					{
						failures.Add(context + ": " + role + " references missing/non-concrete actor " + name);
						return;
					}

					if (role == "SupportActors" && !actor.HasTraitInfo<IPositionableInfo>())
						failures.Add(context + ": " + name + " lacks IPositionableInfo");
					if (role == "SupportBuildings" && !actor.HasTraitInfo<BuildingInfo>())
						failures.Add(context + ": " + name + " lacks BuildingInfo");
					if (role == "SupportActors")
					{
						var mobile = actor.TraitInfoOrDefault<MobileInfo>();
						if (mobile != null && !locomotors.Contains(mobile.Locomotor))
							failures.Add(context + ": " + name + " references missing locomotor " + mobile.Locomotor);
					}
				}

				if (group.BaseActor != null) Check(group.BaseActor, "BaseActor");
				foreach (var actor in group.SupportActors) Check(actor, "SupportActors");
				foreach (var actor in group.SupportBuildings) Check(actor, "SupportBuildings");
				foreach (var actor in group.SupportProxyActors) Check(actor, "SupportProxyActors");
			}

			var factions = world.TraitInfos<FactionInfo>().ToDictionary(f => f.InternalName);
			var classes = groups.Select(g => g.Class).Distinct().OrderBy(c => c).ToArray();
			var spawn = world.TraitInfo<SpawnStartingUnitsInfo>();
			if (!classes.Contains(spawn.StartingUnitsClass)) failures.Add("Missing default starting class " + spawn.StartingUnitsClass);
			var concrete = new HashSet<string>();
			var randoms = new HashSet<string>();
			void CheckFaction(string name, HashSet<string> path, int depth)
			{
				if (!factions.TryGetValue(name, out var faction) || !faction.Selectable)
				{
					failures.Add("Unknown/non-selectable faction in random selection: " + name);
					return;
				}

				if (faction.RandomFactionMembers.Count == 0)
				{
					if (!concrete.Add(name)) return;
					foreach (var startClass in classes)
						if (!groups.Any(g => g.Class == startClass && g.Factions != null && g.Factions.Contains(name)))
							failures.Add("No starting units for faction " + name + " / " + startClass);
					Console.WriteLine("Faction " + name + ": " + string.Join(", ", classes));
					return;
				}

				randoms.Add(name);
				if (!path.Add(name) || depth >= 11)
				{
					failures.Add("Cyclic/too deeply nested random faction: " + name);
					return;
				}

				foreach (var member in faction.RandomFactionMembers.OrderBy(m => m))
					CheckFaction(member, new HashSet<string>(path), depth + 1);
			}

			foreach (var faction in factions.Values.Where(f => f.Selectable).OrderBy(f => f.InternalName))
				CheckFaction(faction.InternalName, new HashSet<string>(), 0);

			foreach (var failure in failures) Console.WriteLine(failure);
			if (failures.Count != 0) throw new InvalidOperationException("Invalid starting-unit references: " + failures.Count);
			Console.WriteLine("Validated " + groups.Length + " starting-unit groups / " + references +
				" actor references (base, support, buildings and proxies), including support placement/locomotors.");
			Console.WriteLine("Validated " + concrete.Count + " concrete factions x " + classes.Length + " start classes = " +
				(concrete.Count * classes.Length) + " combinations, and all " + randoms.Count + " random-faction selections.");
		}
	}
}
