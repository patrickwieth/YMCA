using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using OpenRA.FileFormats;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckGdiMissileTowerCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-gdi-missile-tower";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1 || args.Length == 2;

		// Headless fixture for the actual renderer transform. Only GPU slice allocation is omitted.
		static Voxel TransformFixture(VxlReader reader, HvaReader hva)
		{
#pragma warning disable SYSLIB0050
			var model = (Voxel)FormatterServices.GetUninitializedObject(typeof(Voxel));
#pragma warning restore SYSLIB0050
			const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
			var field = typeof(Voxel).GetField("limbData", Flags);
			var type = field.FieldType.GetElementType();
			var limb = Activator.CreateInstance(type);
			type.GetField("Scale").SetValue(limb, reader.Limbs[0].Scale);
			type.GetField("Bounds").SetValue(limb, reader.Limbs[0].Bounds);
			type.GetField("Size").SetValue(limb, reader.Limbs[0].Size);
			var limbs = Array.CreateInstance(type, 1);
			limbs.SetValue(limb, 0);
			field.SetValue(model, limbs);
			typeof(Voxel).GetField("transforms", Flags).SetValue(model, hva.Transforms);
			typeof(Voxel).GetField("frames", Flags).SetValue(model, 1u);
			typeof(Voxel).GetField("limbs", Flags).SetValue(model, 1u);
			return model;
		}

		[Desc("Check actual ATWR voxel slice transform, bearing and muzzle registration; optional projection JSON path.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["atwr"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var renderer = actor.TraitInfo<RenderVoxelsInfo>();
			if (renderer.Image != "atwr.missile.voxel" || renderer.Palette != "gdi-missile-source" || Math.Abs(renderer.Scale - .85f) > .0001f || !actor.HasTraitInfo<WithVoxelTurretInfo>())
				throw new InvalidDataException("ATWR voxel binding/scale mismatch.");
			if (!mod.DefaultRules.ModelSequences.ContainsKey(renderer.Image)) throw new InvalidDataException("Missing voxel sequence.");
			using var vs = mod.DefaultFileSystem.Open("bits/gdi/buildings/missile-tower-turret.vxl");
			using var hs = mod.DefaultFileSystem.Open("bits/gdi/buildings/missile-tower-turret.hva");
			var voxel = new VxlReader(vs);
			var hva = new HvaReader(hs, "missile-tower-turret.hva");
			if (voxel.LimbCount != 1 || hva.LimbCount != 1 || hva.FrameCount != 1 || voxel.Limbs[0].VoxelCount != 115067)
				throw new InvalidDataException("Unexpected VXL/HVA.");
			var limb = voxel.Limbs[0];
			var transform = TransformFixture(voxel, hva).TransformationMatrix(0, 0);
			var gridOrigin = new float[] { 40, 46, 1, 1 };
			var pivot = OpenRA.Mods.Cnc.Util.MatrixVectorMultiply(transform, gridOrigin);
			if (pivot.Take(3).Any(x => Math.Abs(x) > .00001f) || Math.Abs(limb.Scale - .8f) > .00001f || Enumerable.Range(0, 3).Any(i => limb.Bounds[i + 3] - limb.Bounds[i] != limb.Size[i]))
				throw new InvalidDataException("Raw voxel slice vertices are oversized or displaced from the bearing. Bounds-only scaling is invalid.");
			using var ps = mod.DefaultFileSystem.Open("bits/gdi/buildings/missile-tower-palette.png");
			using var bs = mod.DefaultFileSystem.Open("bits/gdi/buildings/missile-tower-base.png");
			var palette = new Png(ps); var png = new Png(bs);
			if (palette.Palette?.Length != 256 || png.Width != 128 || png.Height != 128) throw new InvalidDataException("Invalid PNG.");
			var mount = actor.TraitInfo<TurretedInfo>().Offset;
			if (mount != new WVec(0, 11, 1527)) throw new InvalidDataException("Bearing mount mismatch.");
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var camera = new WRot(WAngle.Zero, body.CameraPitch - new WAngle(256), new WAngle(256));
			var radians = 27 * Math.PI / 180;
			var forward = 10 + 40 * Math.Cos(radians) - 17 * Math.Sin(radians);
			var height = 18 + 40 * Math.Sin(radians) + 17 * Math.Cos(radians);
			var maximumError = 0.0;
			foreach (var arm in actor.TraitInfos<ArmamentInfo>().Where(x => x.Name != "flare"))
				for (var i = 0; i < 64; i++)
					for (var side = 0; side < 2; side++)
					{
						var gridTip = new float[] { (float)(forward * 1.25 + 40), (side == 0 ? -17 : 17) * 1.25f + 46, (float)(height * 1.25 + 1), 1 };
						var tip = OpenRA.Mods.Cnc.Util.MatrixVectorMultiply(transform, gridTip);
						var rotation = WRot.FromYaw(new WAngle(i * 16));
						var vertex = new WVec((int)Math.Round(tip[0] * renderer.Scale * 1024), (int)Math.Round(tip[1] * renderer.Scale * 1024), (int)Math.Round(tip[2] * renderer.Scale * 1024)).Rotate(rotation).Rotate(camera);
						var actual = body.LocalToWorld(arm.LocalOffset[side].Rotate(rotation));
						var dx = vertex.X / 1024.0 - actual.X * 128.0 / 1448;
						var dy = vertex.Y / 1024.0 - (actual.Y - actual.Z) * 64.0 / 1448;
						maximumError = Math.Max(maximumError, Math.Sqrt(dx * dx + dy * dy));
					}
			if (maximumError > .3) throw new InvalidDataException($"Missile tip mismatch: {maximumError:F3}px.");
			if (args.Length == 2)
			{
				var offset = body.LocalToWorld(mount);
				var matrices = Enumerable.Range(0, 32).Select(i => new[] { new WVec(1024, 0, 0), new WVec(0, 1024, 0), new WVec(0, 0, 1024) }
					.Select(v => v.Rotate(WRot.FromYaw(new WAngle(i * 32))).Rotate(camera)).Select(v => new[] { v.X / 1024f, v.Y / 1024f, v.Z / 1024f }).ToArray()).ToArray();
				File.WriteAllText(args[1], JsonConvert.SerializeObject(new { transform, matrices, scale = renderer.Scale, mount = new[] { offset.X * 128f / 1448, (offset.Y - offset.Z) * 64f / 1448 } }, Formatting.Indented));
			}
			Console.WriteLine($"Actual Voxel.TransformationMatrix maps raw slice bearing to zero; scale .85; centered PNG mount; muzzle maximum {maximumError:F3}px. Live GPU review pending.");
		}
	}
}
