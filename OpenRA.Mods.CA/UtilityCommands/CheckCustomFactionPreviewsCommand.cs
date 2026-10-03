using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-custom-faction-previews", "Validate preview-only trait cloning without a GPU. Not a visual render test.")]
	public sealed class CheckCustomFactionPreviewsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-custom-faction-previews";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			using var stream = utility.ModData.DefaultFileSystem.Open("ca|modular/designer-catalog.json");
			using var reader = new StreamReader(stream);
			var compiler = new CustomFactionDesign(reader.ReadToEnd());
			var rules = utility.ModData.DefaultRules;
			var count = 0;
			foreach (var faction in CustomFactionDesign.BaseFactions)
			{
				var roster = compiler.NewRoster(faction);
				var p = compiler.Profile(roster, roster.Designs[0]);
				foreach (var hull in compiler.CompatibleOptions(p, "chassis"))
				{
					compiler.SelectPart(p, "chassis", hull);
					var source = rules.Actors[compiler.PreviewActor(p).ToLowerInvariant()];
					if (hull.StartsWith("nod-combat-", StringComparison.Ordinal) || hull.StartsWith("china-combat-", StringComparison.Ordinal) || compiler.UsesStockArmaments(p))
					{
						var values = compiler.Calculate(p);
						var mobile = source.TraitInfo<MobileInfo>();
						var output = compiler.Rules(p);
						if (values.Cost != source.TraitInfo<ValuedInfo>().Cost || values.Hp != source.TraitInfo<HealthInfo>().HP ||
							values.Speed != mobile.Speed || values.Turn != mobile.TurnSpeed.Angle ||
							!output.Contains("Locomotor: " + mobile.Locomotor + "\n", StringComparison.Ordinal) ||
							!output.Contains("Type: " + source.TraitInfo<ArmorInfo>().Type + "\n", StringComparison.Ordinal))
							throw new InvalidOperationException("Stock baseline mismatch: " + source.Name);
						var weaponRules = compiler.Weapons(p);
						if (compiler.UsesStockArmaments(p))
						{
							if (weaponRules.Length != 0 || output.Contains("\tArmament", StringComparison.Ordinal) || output.Contains("\tRenderSprites:", StringComparison.Ordinal))
								throw new InvalidOperationException("Stock armaments/graphics were overridden: " + source.Name);
						}
						else foreach (var armament in source.TraitInfos<ArmamentInfo>())
							if (!weaponRules.Contains("Inherits: " + armament.Weapon + "\n", StringComparison.OrdinalIgnoreCase))
								throw new InvalidOperationException("Missing stock weapon channel: " + source.Name + "/" + armament.Weapon);
					}
					var before = source.TraitInfos<ConditionalTraitInfo>().Select(t => t.EnabledByDefault).ToArray();
					var preview = CustomVehiclePreviewWidget.BuildPreviewActor(utility.ModData, rules, source, faction);
					if (!before.SequenceEqual(source.TraitInfos<ConditionalTraitInfo>().Select(t => t.EnabledByDefault)))
						throw new InvalidOperationException("Preview mutated source rules: " + source.Name);
					if (preview.TraitInfos<WithVoxelBodyInfo>().GroupBy(t => t.Sequence).Any(g => g.Count() > 1))
						throw new InvalidOperationException("Overlapping voxel body variants: " + source.Name);
					if (source.Name == "choverlord" && preview.TraitInfos<WithVoxelBodyInfo>().Single().Sequence != "emperor")
						throw new InvalidOperationException("Tank-General preview did not select Emperor body.");
					if (source.Name == "spec" && (preview.TraitInfos<WithSpriteTurretInfo>().Any() || preview.TraitInfos<WithFacingSpriteBodyInfo>().Count() != 1))
						throw new InvalidOperationException("Specter preview did not select its undeployed body.");
					if (source.Name == "ssm" && preview.TraitInfos<WithSpriteTurretInfo>().Single().Sequence != "turret")
						throw new InvalidOperationException("SSM preview did not select its loaded turret.");
					Console.WriteLine(faction + "/" + source.Name + ": " + preview.TraitInfos<IRenderActorPreviewSpritesInfo>().Count() +
						" sprite / " + preview.TraitInfos<IRenderActorPreviewVoxelsInfo>().Count() + " voxel components");
					count++;
				}
			}
			Console.WriteLine("Validated " + count + " preview bindings. GPU/palettes/assets still require an interactive test.");
		}
	}
}
