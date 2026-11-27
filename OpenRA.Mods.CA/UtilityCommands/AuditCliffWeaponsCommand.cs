using System;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class AuditCliffWeaponsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--audit-cliff-weapons";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("List resolved damaging ranged projectile families for terrain-policy coverage review.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rows = utility.ModData.DefaultRules.Weapons.Where(p => p.Value.Projectile != null && p.Value.Range > WDist.Zero &&
				p.Value.Warheads.OfType<DamageWarhead>().Any(w => w.Damage > 0))
				.GroupBy(p => p.Value.Projectile.GetType().Name).OrderBy(g => g.Key)
				.Select(g => new { Projectile = g.Key, Weapons = g.Select(p => p.Key).OrderBy(k => k).ToArray() });
			Console.WriteLine(JsonConvert.SerializeObject(rows, Formatting.Indented));
		}
	}
}
