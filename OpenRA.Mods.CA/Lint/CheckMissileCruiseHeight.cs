using System;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Projectiles;

namespace OpenRA.Mods.CA.Lint
{
	public sealed class CheckMissileCruiseHeight : ILintRulesPass
	{
		internal static bool IsUnsafe(IProjectileInfo projectile) => projectile switch
		{
			MissileCAInfo missile => missile.BoundToTerrainType != "Water" && !missile.CruiseRelativeToTerrain && !missile.TerrainHeightAware,
			MissileInfo missile => missile.BoundToTerrainType != "Water" && !missile.CruiseRelativeToTerrain && !missile.TerrainHeightAware,
			_ => false
		};

		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			foreach (var weapon in rules.Weapons)
				if (IsUnsafe(weapon.Value.Projectile))
					emitError($"Weapon `{weapon.Key}` uses a sea-level missile cruise reference. Set CruiseRelativeToTerrain: true; use TerrainHeightAware only if obstacle lookahead is intended.");
		}
	}
}
