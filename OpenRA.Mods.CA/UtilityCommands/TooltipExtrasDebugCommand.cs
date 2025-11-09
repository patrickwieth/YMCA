using System;
using System.Linq;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
    [Desc("--tooltipextras <actor>", "Dump TooltipExtras info for the specified actor.")]
    public sealed class TooltipExtrasDebugCommand : IUtilityCommand
    {
        string IUtilityCommand.Name => "--tooltipextras";

        bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2;

        void IUtilityCommand.Run(Utility utility, string[] args)
        {
            var actorName = args[1];
            var rules = utility.ModData.DefaultRules;
            if (!rules.Actors.TryGetValue(actorName.ToLowerInvariant(), out var actor))
            {
                Console.WriteLine($"Actor '{actorName}' not found.");
                var matches = rules.Actors.Keys.Where(k => k.Contains(actorName, StringComparison.OrdinalIgnoreCase)).Take(10);
                foreach (var match in matches)
                    Console.WriteLine($"  maybe: {match}");
                return;
            }

            var extras = actor.TraitInfos<TooltipExtrasInfo>().ToArray();
            Console.WriteLine($"Actor {actor.Name} extras count={extras.Length}");
            foreach (var info in extras)
            {
                Console.WriteLine($"  standard={info.IsStandard} strengths='{info.Strengths}' weaknesses='{info.Weaknesses}' attributes='{info.Attributes}'");
            }
        }
    }
}
