using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--export-combat-baselines ACTOR...", "Export resolved stock data for reviewed, immutable combat assemblies.")]
	public sealed class ExportCombatBaselinesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-combat-baselines";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length > 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			var rows = new JArray();
			foreach (var name in args.Skip(1))
			{
				var a = rules.Actors[name.ToLowerInvariant()];
				var m = a.TraitInfo<MobileInfo>();
				rows.Add(new JObject
				{
					["actor"] = name, ["label"] = a.TraitInfos<TooltipInfo>().First().Name, ["cost"] = a.TraitInfo<ValuedInfo>().Cost, ["hp"] = a.TraitInfo<HealthInfo>().HP,
					["speed"] = m.Speed, ["turn"] = m.TurnSpeed.Angle, ["locomotor"] = m.Locomotor,
					["armor"] = a.TraitInfoOrDefault<ArmorInfo>()?.Type ?? "None",
					["image"] = a.TraitInfo<RenderSpritesInfo>().Image ?? a.Name,
					["prerequisites"] = new JArray(a.TraitInfo<BuildableInfo>().Prerequisites),
					["weapons"] = new JArray(a.TraitInfos<ArmamentInfo>().Select(t => t.Weapon)),
					["traits"] = new JArray(a.TraitInfos<OpenRA.Traits.TraitInfo>().Select(t => t.GetType().Name + (t.InstanceName == null ? "" : "@" + t.InstanceName)))
				});
			}
			Console.WriteLine(rows.ToString());
		}
	}
}
