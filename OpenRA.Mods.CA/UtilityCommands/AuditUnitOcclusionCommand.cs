using System;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class AuditUnitOcclusionCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--audit-unit-occlusion";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Audit resolved mobile and aircraft actors independently of their selection templates.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rows = utility.ModData.DefaultRules.Actors
				.Where(p => !p.Key.StartsWith("^", StringComparison.Ordinal) &&
					(p.Value.HasTraitInfo<MobileInfo>() || p.Value.HasTraitInfo<AircraftInfo>()))
				.OrderBy(p => p.Key).Select(p => new
				{
					Actor = p.Key,
					Name = p.Value.TraitInfos<TooltipInfo>().FirstOrDefault()?.Name,
					Aircraft = p.Value.HasTraitInfo<AircraftInfo>(),
					Building = p.Value.HasTraitInfo<BuildingInfo>(),
					Selectable = p.Value.HasTraitInfo<SelectableInfo>(),
					Buildable = p.Value.HasTraitInfo<BuildableInfo>(),
					Enabled = p.Value.TraitInfoOrDefault<WithRockOcclusionInfo>()?.Enabled == true
				}).ToArray();
			Console.WriteLine(JsonConvert.SerializeObject(rows, Formatting.Indented));
		}
	}
}
