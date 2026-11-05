using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class CheckSelectionTraitsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-selection-traits";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2;
		[Desc("[MAP]", "Reject concrete actors with multiple ISelectableInfo implementations, including inherited/map-local rules.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			void Check(Ruleset rules)
			{
				var errors = 0; var count = 0;
				foreach (var actor in rules.Actors.Values.Where(a => !a.Name.StartsWith("^", StringComparison.Ordinal)))
				{
					var traits = actor.TraitInfos<ISelectableInfo>().ToArray(); count++;
					if (traits.Length <= 1)
					{
						// Exercise the exact lookup that crashed, including modifier paths.
						foreach (var modifiers in new[] { Modifiers.None, Modifiers.Ctrl, Modifiers.Alt }) actor.SelectionPriority(modifiers);
						continue;
					}
					Console.WriteLine($"DUPLICATE SELECTION {actor.Name}: {string.Join(", ", traits.Select(t => t.GetType().Name))}"); errors++;
				}
				if (errors != 0) throw new InvalidDataException($"{errors} actor definitions have ambiguous selection traits.");
				Console.WriteLine($"SELECTION PASS {count} concrete actor definitions have at most one selection trait.");
			}
			if (args.Length == 1) Check(utility.ModData.DefaultRules);
			else
			{
				using var stream = File.OpenRead(args[1]); using var zip = new ZipFileLoader.ReadOnlyZipFile(stream, args[1]);
				using var map = new Map(utility.ModData, zip); Check(map.Rules);
			}
		}
	}
}
