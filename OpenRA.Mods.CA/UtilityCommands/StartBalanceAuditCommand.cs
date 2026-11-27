using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OpenRA.Mods.CA.MapGeneration;

namespace OpenRA.Mods.CA.UtilityCommands
{
	sealed class StartBalanceAuditCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--start-balance-audit";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length >= 2 && args.Length <= 4;
		[Desc("OUTPUT.json", "Measure native cardinal-path economy access and nearby free building area.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var results = new List<object>();
			foreach (var preset in args.Length > 2 ? new[] { args[2] } : new[] { "plains", "mountain-valleys" })
			foreach (var players in args.Length > 3 ? new[] { int.Parse(args[3]) } : new[] { 2, 4, 8 })
			foreach (var seed in new[] { 42, 43 })
			{
				var options = DebugMapGeneratorCommand.ParseOptions(new[] { "audit", ".", $"preset={preset}", "mode=strategic",
					$"players={players}", $"seed={seed}", "tech=sparse", "resources=patches", "size=auto" });
				var plan = MapPlanGenerator.Generate(options);
				results.Add(new { Preset = preset, Players = players, Seed = seed, plan.GenerationAttempt, Starts = StartBalanceMetrics.Report(plan) });
				File.WriteAllText(args[1], JsonConvert.SerializeObject(results, Formatting.Indented));
				Console.WriteLine($"AUDITED {preset} {players}P {seed}");
			}
		}
	}
}
