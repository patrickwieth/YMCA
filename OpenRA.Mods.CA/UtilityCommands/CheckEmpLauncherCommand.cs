using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Projectiles;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckEmpLauncherCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-emp-launcher";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load fixed EMP launcher states and verify its target-independent launch geometry.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["patr"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.HasTraitInfo<TurretedInfo>() || !actor.HasTraitInfo<AttackOmniInfo>() || !actor.HasTraitInfo<ReloadAmmoOnSupportPowerChargedInfo>() || actor.HasTraitInfo<ReloadAmmoPoolInfo>())
				throw new InvalidDataException("EMP must have no rotating turret or independent reload timer.");
			if (actor.TraitInfo<AmmoPoolInfo>().InitialAmmo != 0 || actor.TraitInfo<AmmoPoolInfo>().Ammo != 1)
				throw new InvalidDataException("EMP ammunition state.");
			var power = actor.TraitInfos<SupportPowerInfo>().Single();
			if (power.ChargeInterval != 4500) throw new InvalidDataException("Changed support charge interval.");
			var arm = actor.TraitInfo<ArmamentInfo>();
			var missile = mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()].Projectile as MissileCAInfo;
			if (missile == null || arm.LocalYaw.Single().Angle != 256 || missile.MinimumLaunchAngle.Angle != 72 || missile.MaximumLaunchAngle.Angle != 72 || missile.HomingActivationDelay != 6 || !missile.InitializeFacingFromVelocity)
				throw new InvalidDataException("EMP fixed-launch/homing configuration.");
			var p = actor.TraitInfo<BodyOrientationInfo>().LocalToWorld(arm.LocalOffset.Single());
			var dx = p.X * 64.0 / 1448 - (86.86564299424184 - 128);
			var dy = (p.Y - p.Z) * 64.0 / 1448 - (32.392514395393476 - 176);
			if (Math.Sqrt(dx * dx + dy * dy) > .1) throw new InvalidDataException("EMP missile source registration.");
			var velocity = new WVec(0, -missile.MinimumLaunchSpeed.Length, 0)
				.Rotate(new WRot(missile.MinimumLaunchAngle, WAngle.Zero, WAngle.Zero))
				.Rotate(WRot.FromYaw(arm.LocalYaw[0]));
			var sx = (double)velocity.X; var sy = (double)(velocity.Y - velocity.Z);
			var alignment = (sx * -.89899286 + sy * -.43796329) / Math.Sqrt(sx * sx + sy * sy);
			if (velocity.Z <= 0 || alignment < .999) throw new InvalidDataException($"EMP launch does not match pictured angle: velocity {velocity}, alignment {alignment}.");
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null);
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			foreach (var image in new[] { "patr.gdi.png", missile.Image, "gdibib3x3.emp" })
			{
				var node = nodes.Single(n => n.Key == image);
				var names = new[] { "Defaults", "idle", "damaged-idle", "empty", "damaged-empty", "construction" };
				var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
				var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
				cache.LoadReservations(mod);
				foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
				if (sequences["idle"].Facings != (image == missile.Image ? 32 : 1)) throw new InvalidDataException("EMP native sequence facings.");
			}
			Console.WriteLine($"EMP: fixed loaded/empty launcher and missile frames loaded; muzzle error {Math.Sqrt(dx * dx + dy * dy):F3}px; launch alignment {alignment:F6}. Ammo refills on support charge event. Live firing pending.");
		}
	}
}
