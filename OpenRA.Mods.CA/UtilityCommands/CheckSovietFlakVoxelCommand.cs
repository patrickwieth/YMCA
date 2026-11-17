using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.CA.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-soviet-flak-voxel", "Load the three real flak voxel parts and check axial runtime recoil.")]
	public sealed class CheckSovietFlakVoxelCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-soviet-flak-voxel";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			foreach (var part in new[] { "base", "turret", "barrel" })
			{
				var prefix = "bits/soviet/buildings/experimental/flak-" + part;
				using var vs = mod.DefaultFileSystem.Open(prefix + ".vxl");
				using var hs = mod.DefaultFileSystem.Open(prefix + ".hva");
				var vxl = new VxlReader(vs);
				var hva = new HvaReader(hs, prefix + ".hva");
				if (vxl.LimbCount != 1 || hva.LimbCount != 1 || hva.FrameCount != 1 || vxl.Limbs[0].VoxelCount == 0)
					throw new InvalidDataException("Expected one nonempty static model for " + part);
				Console.WriteLine($"Flak {part}: {vxl.Limbs[0].VoxelCount} actual voxels, one HVA pose.");
			}

			var actor = mod.DefaultRules.Actors["flak.soviet.voxel"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var arm = actor.TraitInfo<ArmamentInfo>();
			var barrel = actor.TraitInfo<WithVoxelBarrelAxisInfo>();
			var offset = WithVoxelBarrelAxis.RecoilOffset(barrel.RecoilDirection, arm.Recoil);
			if (offset.X != -81 || offset.Y != 0 || offset.Z != -63)
				throw new InvalidDataException("Incorrect inclined recoil vector: " + offset);
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var camera = new WRot(WAngle.Zero, body.CameraPitch - new WAngle(256), new WAngle(256));
			var expected = new WVec((int)Math.Round(-.24 * 20 * Math.Cos(42 * Math.PI / 180) * 1024), 0,
				(int)Math.Round(-.24 * 20 * Math.Sin(42 * Math.PI / 180) * 1024));
			double maxError = 0;
			for (var i = 0; i < 32; i++)
			{
				var yaw = WRot.FromYaw(new WAngle(i * 32));
				var world = body.LocalToWorld(offset.Rotate(yaw));
				var screen = expected.Rotate(yaw).Rotate(camera);
				var dx = world.X * 64.0 / 1448 - screen.X / 1024.0;
				var dy = (world.Y - world.Z) * 64.0 / 1448 - screen.Y / 1024.0;
				maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
			}
			if (maxError > .15) throw new InvalidDataException($"Voxel recoil projection mismatch: {maxError:F3}px");
			Console.WriteLine($"Recoil versus voxel camera: maximum {maxError:F3}px over32 facings.");
			if (!actor.HasTraitInfo<WithVoxelBodyInfo>() || !actor.HasTraitInfo<WithVoxelTurretInfo>())
				throw new InvalidDataException("Missing independent base or turret.");
			if (actor.HasTraitInfo<BuildableInfo>())
				throw new InvalidDataException("Art-test actor unexpectedly added to production.");
			Console.WriteLine("Soviet flak voxel actor and axial recoil validated. Live GPU/firing appearance still needs a game run.");
		}
	}
}
