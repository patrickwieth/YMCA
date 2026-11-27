using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckCarryallRotorsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-carryall-rotors";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Check Carryall nacelle surfaces and rotor registration through a full rotation.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			using var source = mod.DefaultFileSystem.Open("V22.vxl");
			var limb = new VxlReader(source).Limbs[0];
			var grid = mod.Manifest.Get<MapGrid>();
			foreach (var name in new[] { "ocar", "ocar.eagle", "ocar.reinforce" })
			{
				var actor = mod.DefaultRules.Actors[name];
				_ = actor.TraitsInConstructOrder().ToArray();
				var body = actor.TraitInfo<BodyOrientationInfo>();
				var rotors = actor.TraitInfos<WithVoxelRotorOverlayInfo>().ToArray();
				if (rotors.Length != 4 || actor.TraitInfos<WithIdleOverlayInfo>().Any(r => r.GetType() == typeof(WithIdleOverlayInfo) && r.Sequence.Contains("rotor")))
					throw new InvalidDataException("Missing voxel rotor pair or duplicate terrain-projected rotors.");
				double error = 0;
				foreach (var rotor in rotors)
				{
					var hubY = Math.Sign(rotor.Offset.Y) * 54.0;
					const double hubX = 9, hubZ = 10, scale = .5;
					var x = (int)(hubX - limb.Bounds[0]); var y = (int)(-hubY - limb.Bounds[1]);
					if (Math.Abs(limb.VoxelMap[x, y].Keys.Max() + 1 + limb.Bounds[2] - hubZ) > .01)
						throw new InvalidDataException("V22 nacelle attachment surface changed.");
					for (var i = 0; i < 32; i++)
					{
						var p = WithVoxelRotorOverlayInfo.ProjectOffset(body, rotor.Offset, WRot.FromYaw(new WAngle(i * 32)), grid);
						var angle = i * Math.PI / 16; var pitch = body.CameraPitch.Angle * Math.PI / 512;
						var dx = p.X * (double)grid.TileSize.Width / grid.TileScale - scale * (hubY * Math.Cos(angle) - hubX * Math.Sin(angle));
						var dy = (p.Y - p.Z) * (double)grid.TileSize.Height / grid.TileScale + scale * ((hubX * Math.Cos(angle) + hubY * Math.Sin(angle)) * Math.Sin(pitch) + hubZ * Math.Cos(pitch));
						error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
					}
				}
				if (error > .3) throw new InvalidDataException("Carryall rotor projection error: " + error);
				Console.WriteLine($"{name}: air/ground nacelles, 32 facings; maximum attachment error {error:F3}px.");
			}
		}
	}
}
