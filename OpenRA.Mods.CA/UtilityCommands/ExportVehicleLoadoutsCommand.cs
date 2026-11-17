using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Support;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--export-vehicle-loadouts ACTOR...", "Read-only resolved vehicle loadout inventory. Not compiler input or buildability proof.")]
	public sealed class ExportVehicleLoadoutsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--export-vehicle-loadouts";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length > 1;
		static readonly string[] Prefixes = { "Armament", "Turreted", "Attack", "AmmoPool", "ReloadAmmoPool", "PointDefense", "Armor", "Cargo",
			"RenderSprites", "RenderVoxels", "WithSpriteTurret", "WithVoxelTurret", "WithVoxelBarrel", "WithVoxelBody", "WithFacingSpriteBody",
			"WithMirageSpriteBody", "WithModifiedPalette", "GrantConditionOnDeploy", "DeployOnAttack", "Transforms", "TransformOnCondition",
			"MissileSpawner", "GrantConditionOnPrerequisite", "Power", "FirepowerMultiplier", "ReloadDelayMultiplier", "RangeMultiplier", "Targetable", "Mobile", "Aircraft", "Carryall", "AutoCarryall", "Carryable", "Rearmable",
			"Carrier", "DroneSpawner", "MissileSpawnerSlave", "AirstrikeMaster", "ParaDrop", "FallsToEarth" };
		static readonly string[] Fields = { "Name", "Turret", "Turrets", "Armaments", "Armament", "Weapon", "LocalOffset", "LocalYaw", "FireDelay",
			"Offset", "TurnSpeed", "RequiresCondition", "PauseOnCondition", "TargetRelationships", "ForceTargetRelationships", "FacingTolerance",
			"TargetTypes", "Ammo", "InitialAmmo", "AmmoCondition", "Delay", "Count", "ResetOnFire", "PointDefenseTypes", "ValidRelationships", "Type", "Types",
			"MaxWeight", "InitialUnits", "PassengerConditions", "LoadingCondition", "LoadedCondition", "PortOffsets", "PortYaws", "PortCones",
			"Image", "FactionImages", "Scale", "Sequence", "Palette", "PlayerPalette", "IsPlayerPalette", "Actor", "Actors", "RespawnTicks",
			"ArmamentNames", "SpawnOffset", "SpawnAllAtOnce", "DeployedCondition", "UndeployedCondition", "UndeployOnMove", "UndeployOnPickup",
			"AllowedTerrainTypes", "Condition", "Prerequisites", "Amount", "Modifier", "IsTraitDisabled",
			"Speed", "Locomotor", "CruiseAltitude", "CanHover", "VTOL", "CanSlide", "LandableTerrainTypes", "IdleBehavior", "MinAirborneAltitude",
			"RearmActors", "AmmoPools", "LoadedCondition", "BeforeLoadDelay", "BeforeUnloadDelay", "DropRange", "LandingSearchRadius",
			"LaunchOffset", "SpawnOffsets", "SpawnTime", "LaunchDelay", "LaunchSounds", "LoadedConditions", "InitialActorCount",
			"GatherCellRange", "SlaveDisposalOnKill", "SlaveDisposalOnOwnerChange", "SlavesHaveFreeWill", "DockingDistance", "RearmTicks", "SpawnIsMissile", "LaunchingCondition", "LaunchingTicks", "InstantRepair", "SpawnContainConditions" };

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rows = new JArray();
			foreach (var name in args.Skip(1))
			{
				var actor = utility.ModData.DefaultRules.Actors[name.ToLowerInvariant()];
				var build = actor.TraitInfoOrDefault<BuildableInfo>();
				var prereqs = build?.Prerequisites ?? Array.Empty<string>();
				var traits = new JArray();
				foreach (var t in actor.TraitInfos<TraitInfo>().Where(t => Prefixes.Any(p => t.GetType().Name.StartsWith(p, StringComparison.Ordinal))))
				{
					var fields = new JObject();
					// Display serialization only: especially dictionaries are documentation strings, not round-trip YAML.
					foreach (var field in FieldSaver.Save(t).Nodes.Where(n => Fields.Contains(n.Key)))
					{
						var value = t.GetType().GetField(field.Key)?.GetValue(t);
						fields[field.Key] = value is VariableExpression expression ? expression.Expression : field.Value.Value;
					}
					traits.Add(new JObject { ["type"] = t.GetType().Name, ["instance"] = t.InstanceName, ["fields"] = fields });
				}
				var mobile = actor.TraitInfoOrDefault<MobileInfo>();
				var aircraft = actor.TraitInfoOrDefault<AircraftInfo>();
				rows.Add(new JObject
				{
					["actor"] = name, ["cost"] = actor.TraitInfoOrDefault<ValuedInfo>()?.Cost,
					["hp"] = actor.TraitInfoOrDefault<HealthInfo>()?.HP,
					["speed"] = aircraft?.Speed ?? mobile?.Speed,
					["locomotor"] = mobile?.Locomotor,
					["movement_kind"] = aircraft != null ? "aircraft" : mobile != null ? "mobile" : "non-mobile",
					["trait_types"] = new JArray(actor.TraitInfos<TraitInfo>().Select(t => t.GetType().Name).Distinct().OrderBy(t => t, StringComparer.Ordinal)),
					["prerequisites"] = new JArray(prereqs),
					["production_status"] = build == null ? "no-buildable" : prereqs.Contains("~disabled") ? "disabled" : prereqs.Contains("~botplayer") ? "bot-gated" : "gated-candidate",
					["traits"] = traits
				});
			}
			Console.WriteLine(rows.ToString());
		}
	}
}
