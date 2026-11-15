using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-mindcontrol-targets", "Audit resolved actors against every mind-controller armament, including conditional target types.")]
	public sealed class CheckMindControlTargetsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-mindcontrol-targets";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			var rules = utility.ModData.DefaultRules;
			foreach (var name in new[] { "dtrk", "ttrk", "atomparabomb", "bixi.missile", "icbm", "rice_passengershell",
				"rice_shell", "teslaparabomb", "v3", "v3.kirov", "v3b", "v4" })
			{
				var actor = rules.Actors[name];
				if (actor.HasTraitInfo<MindControllableCAInfo>())
					throw new InvalidOperationException(name + ": unexpectedly made mind-controllable.");

				if (!actor.TraitInfos<TargetableInfo>().Any(t => t.RequiresCondition == null && t.TargetTypes.Contains("MindControlImmune")))
					throw new InvalidOperationException(name + ": missing unconditional mind-control immunity.");
			}

			foreach (var name in new[] { "enslaveinfantry", "enslavevehicle" })
				if (!rules.Weapons[name].InvalidTargets.Contains("MindControlImmune"))
					throw new InvalidOperationException(name + ": does not exclude immune targets.");

			// Ordinary biological-driver vehicles must still be eligible for takeover.
			var tank = rules.Actors["mtnk"];
			if (!tank.HasTraitInfo<MindControllableCAInfo>() ||
				tank.TraitInfos<TargetableInfo>().Any(t => t.RequiresCondition == null && t.TargetTypes.Contains("MindControlImmune")))
				throw new InvalidOperationException("Battle Tank mind-control eligibility changed.");

			// Positive and negative controls for the conditional target audit itself.
			var enslave = rules.Weapons["enslavevehicle"];
			if (!CanBeTargeted(tank.TraitInfos<ITargetableInfo>().ToArray(), enslave, out _))
				throw new InvalidOperationException("Audit incorrectly rejects an ordinary control target.");
			foreach (var name in new[] { "harv", "harv.scrin", "har2", "harv.chrono", "chharv", "soviet_miner" })
				if (CanBeTargeted(rules.Actors[name].TraitInfos<ITargetableInfo>().ToArray(), enslave, out _))
					throw new InvalidOperationException(name + ": harvester immunity does not cover its conditional target types.");
			var unprotectedMissile = rules.Actors["v3"].TraitInfos<ITargetableInfo>()
				.Where(t => !t.GetTargetTypes().Contains("MindControlImmune")).ToArray();
			if (!CanBeTargeted(unprotectedMissile, enslave, out var missileState) || missileState != "airborne=0")
				throw new InvalidOperationException("Audit failed to detect the original grounded-missile vulnerability.");

			var weapons = rules.Actors.Values.Where(a => !a.Name.StartsWith("^", StringComparison.Ordinal))
				.SelectMany(a => a.TraitInfos<MindControllerCAInfo>().SelectMany(c =>
					a.TraitInfos<ArmamentInfo>().Where(arm => c.ArmamentNames.Contains(arm.Name)).Select(arm => arm.Weapon.ToLowerInvariant())))
				.Distinct().OrderBy(w => w).ToArray();
			Console.WriteLine("Mind-control weapons: " + string.Join(", ", weapons));
			var failures = 0;
			foreach (var actor in rules.Actors.Values.OrderBy(a => a.Name))
			{
				if (actor.Name.StartsWith("^", StringComparison.Ordinal) || actor.HasTraitInfo<MindControllableCAInfo>()) continue;
				var targets = actor.TraitInfos<ITargetableInfo>().ToArray();
				foreach (var name in weapons)
				{
					var weapon = rules.Weapons[name];
					if (!CanBeTargeted(targets, weapon, out var state)) continue;
					Console.WriteLine("MISSING CONTROL TRAIT: " + actor.Name + " <- " + name + " [" + state + "]");
					failures++;
				}
			}

			if (failures > 0) throw new InvalidOperationException(failures + " potentially targetable actor/weapon pairs lack MindControllableCA.");
			Console.WriteLine("Mind-control regression checks passed: " +
				rules.Actors.Values.Count(a => !a.Name.StartsWith("^", StringComparison.Ordinal)) + " concrete actors / " +
				weapons.Length + " controller weapons; no missing-trait targets. Truck/projectile immunity, conditional harvester immunity and MTNK eligibility checked.");
		}
		static bool CanBeTargeted(ITargetableInfo[] targets, WeaponInfo weapon, out string state)
		{
			state = "";
			var relevant = targets.Where(t => t.GetTargetTypes().Overlaps(weapon.ValidTargets) ||
				t.GetTargetTypes().Overlaps(weapon.InvalidTargets)).ToArray();
			if (!relevant.Any(t => t.GetTargetTypes().Overlaps(weapon.ValidTargets))) return false;
			if (relevant.Any(t => (!(t is ConditionalTraitInfo c) || c.RequiresCondition == null) &&
				t.GetTargetTypes().Overlaps(weapon.InvalidTargets))) return false;

			var conditions = relevant.OfType<ConditionalTraitInfo>().Where(t => t.RequiresCondition != null)
				.Select(t => t.RequiresCondition).ToArray();
			var variables = conditions.SelectMany(c => c.Variables).Distinct().OrderBy(v => v).ToArray();
			// Enumerate Boolean states jointly, not each target provider in isolation.
			// For example, the Scrin harvester's water target cannot be enabled while
			// its normal Vehicle + MindControlImmune target is disabled.
			// Do not silently certify numeric/stack-count predicates with only 0/1 samples.
			if (variables.Length > 16 || conditions.Any(c => Regex.IsMatch(c.Expression, @"[<>=+*/%?:^0-9]")))
				throw new InvalidOperationException("Target-condition audit requires review: " + string.Join("; ", conditions.Select(c => c.Expression)));

			var symbols = new Dictionary<string, int>();
			for (var mask = 0; mask < (1 << variables.Length); mask++)
			{
				for (var i = 0; i < variables.Length; i++) symbols[variables[i]] = (mask >> i) & 1;
				var enabled = relevant.Where(t => !(t is ConditionalTraitInfo c) || c.RequiresCondition == null ||
					c.RequiresCondition.Evaluate(symbols)).ToArray();
				if (enabled.Any(t => t.GetTargetTypes().Overlaps(weapon.ValidTargets)) &&
					!enabled.Any(t => t.GetTargetTypes().Overlaps(weapon.InvalidTargets)))
				{
					state = string.Join(", ", symbols.Select(kv => kv.Key + "=" + kv.Value));
					return true;
				}
			}

			return false;
		}
	}
}
