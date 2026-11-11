using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-sentry-voxel", "Read the real Sentry VXL/HVA/palette and validate its independent actor dependencies.")]
	public sealed class CheckSentryVoxelCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-sentry-voxel";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			const string Prefix = "bits/soviet/buildings/experimental/sentry-v13-voxel";
			using var vs = mod.DefaultFileSystem.Open(Prefix + ".vxl");
			using var hs = mod.DefaultFileSystem.Open(Prefix + ".hva");
			var vxl = new VxlReader(vs);
			var hva = new HvaReader(hs, Prefix + ".hva");
			if (vxl.LimbCount != 1 || hva.LimbCount != 1 || hva.FrameCount != 1)
				throw new InvalidDataException("Expected one static voxel limb, rotated dynamically by the engine.");
			var limb = vxl.Limbs[0];
			if (limb.Name.TrimEnd('\0') != "turret" || limb.VoxelCount == 0 || limb.Type != NormalType.RedAlert2 || limb.Scale != 1)
				throw new InvalidDataException("Invalid Sentry voxel limb.");
			for (var i = 0; i < 3; i++)
				if (Math.Abs(limb.Bounds[i + 3] - limb.Bounds[i] - limb.Size[i]) > 0.001f)
					throw new InvalidDataException("Voxel bounds must match the native grid dimensions.");
			using var ps = mod.DefaultFileSystem.Open("bits/soviet/buildings/experimental/sentry-voxel-palette.png");
			var palette = new Png(ps);
			if (palette.Palette == null || palette.Palette.Length != 256)
				throw new InvalidDataException("Missing indexed source palette.");
			uint count = 0;
			foreach (var column in limb.VoxelMap)
			{
				if (column == null) continue;
				foreach (var item in column)
				{
					if (item.Key >= limb.Size[2] || item.Value.Color == 0 || item.Value.Normal >= 244)
						throw new InvalidDataException("Invalid voxel coordinate/color/normal.");
					count++;
				}
			}
			if (count != limb.VoxelCount) throw new InvalidDataException("Voxel count mismatch.");
			var actor = mod.DefaultRules.Actors["sentry.soviet.voxel"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (!actor.HasTraitInfo<RenderVoxelsInfo>() || !actor.HasTraitInfo<WithVoxelTurretInfo>() || actor.HasTraitInfo<WithSpriteTurretInfo>())
				throw new InvalidDataException("Voxel actor is not isolated from the sprite turret.");
			// The exported VXL +X axis is forward; check the actual engine camera,
			// not the clockwise compass assumption that broke the first sprite export.
			var camera = new WRot(WAngle.Zero, new WAngle(85) - new WAngle(256), new WAngle(256));
			var north = new WVec(1024, 0, 0).Rotate(camera);
			var west = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(new WAngle(256))).Rotate(camera);
			var east = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(new WAngle(768))).Rotate(camera);
			if (Math.Abs(north.X) > 4 || north.Y >= 0 || west.X >= 0 || east.X <= 0)
				throw new InvalidDataException("Voxel forward/camera orientation mismatch.");
			if (!mod.DefaultRules.ModelSequences.ContainsKey("sentry.soviet.voxel"))
				throw new InvalidDataException("Missing voxel model sequence.");
			Console.WriteLine($"Sentry VXL/HVA: {count} voxels, {limb.Size[0]}x{limb.Size[1]}x{limb.Size[2]}, valid palette and actor. Live GPU appearance remains untested.");
		}
	}
}
