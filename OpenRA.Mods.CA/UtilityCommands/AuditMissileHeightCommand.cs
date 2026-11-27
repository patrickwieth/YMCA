using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.CA.Lint;
using OpenRA.Mods.Common.Projectiles;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class AuditMissileHeightCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--audit-missile-height";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2 && args[1] == "check";
		[Desc("Audit resolved missile height policies and actual actor armament bindings.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			var rows = rules.Weapons.Where(p => p.Value.Projectile is MissileCAInfo or MissileInfo).OrderBy(p => p.Key).Select(p =>
			{
				var projectile = p.Value.Projectile;
				object Field(string name) => projectile.GetType().GetField(name)?.GetValue(projectile);
				return new { Weapon = p.Key, Projectile = projectile.GetType().Name, Range = p.Value.Range.Length,
					Aware = Field("TerrainHeightAware"), Relative = Field("CruiseRelativeToTerrain"), Cruise = Field("CruiseAltitude"), BoundTo = Field("BoundToTerrainType"),
					Speed = Field("Speed"), Limit = Field("RangeLimit"), Targets = p.Value.ValidTargets.ToString(),
					Actors = rules.Actors.Where(a => a.Value.TraitInfos<ArmamentInfo>().Any(t => t.Weapon.Equals(p.Key, StringComparison.OrdinalIgnoreCase))).Select(a => a.Key).ToArray() };
			}).ToArray();
			Console.WriteLine(JsonConvert.SerializeObject(rows, Formatting.Indented));
			if (args.Length == 2)
			{
				if (!utility.ModData.ObjectCreator.GetTypesImplementing<OpenRA.Mods.Common.Lint.ILintRulesPass>().Contains(typeof(CheckMissileCruiseHeight)))
					throw new InvalidDataException("Missile-height guard is missing from normal rules lint discovery.");
				new CheckMissileCruiseHeight().Run(error => throw new InvalidDataException(error), _ => { }, utility.ModData, rules);
				// Negative controls verify that the guard catches a future unsafe definition,
				// while retaining the deliberate water-only exception.
				var unsafeCA = new MissileCAInfo();
				FieldLoader.LoadField(unsafeCA, "CruiseRelativeToTerrain", "false");
				var water = new MissileInfo();
				FieldLoader.LoadField(water, "BoundToTerrainType", "Water");
				if (!CheckMissileCruiseHeight.IsUnsafe(new MissileInfo()) ||
					!CheckMissileCruiseHeight.IsUnsafe(unsafeCA) || CheckMissileCruiseHeight.IsUnsafe(water))
					throw new InvalidDataException("Missile-height lint negative controls failed.");
			}
		}
	}
}
