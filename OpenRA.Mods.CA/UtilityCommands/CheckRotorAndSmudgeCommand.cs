using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Cnc.FileFormats;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckRotorAndSmudgeCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-rotor-and-smudge";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Validate Chinook voxel-hub registration and legacy smudge palettes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var world = mod.DefaultRules.Actors["world"];
			if (world.TraitInfos<SmudgeLayerInfo>().Any(s => s.Palette != "legacy-smudge"))
				throw new InvalidDataException("Legacy smudges must not use the isometric terrain palette.");
			var palettes = world.TraitInfos<IProvidesCursorPaletteInfo>().Where(p => (string)p.GetType().GetField("Name")?.GetValue(p) == "legacy-smudge").ToArray();
			if (palettes.Length != 9) throw new InvalidDataException("Smudge tileset coverage.");
			using var source = mod.DefaultFileSystem.Open("bits/palettes/temperat.pal");
			var expectedPalette = new ImmutablePalette(source, new[] { 0 }, new[] { 3, 4 });
			foreach (var palette in palettes)
			{
				var loaded = palette.ReadPalette(mod.DefaultFileSystem);
				var tileset = ((ITilesetSpecificPaletteInfo)palette).Tileset;
				if (tileset.Contains("TEMPERATE"))
					for (var i = 0; i < 256; i++)
						if (loaded.GetColor(i) != expectedPalette.GetColor(i)) throw new InvalidDataException("Incorrect RA smudge palette index.");
			}
			// Independently inspect the actual voxel's two upper pylon surfaces.
			using var stream = mod.DefaultFileSystem.Open("chinook.vxl");
			var limb = new VxlReader(stream).Limbs[0];
			var grid = mod.Manifest.Get<MapGrid>();
			foreach (var name in new[] { "tran", "tran.eagle", "tran.paradrop" })
			{
				var actor = mod.DefaultRules.Actors[name];
				_ = actor.TraitsInConstructOrder().ToArray();
				var body = actor.TraitInfo<BodyOrientationInfo>();
				var rotors = actor.TraitInfos<WithVoxelRotorOverlayInfo>().ToArray();
				if (rotors.Length != 4) throw new InvalidDataException("Chinook needs both airborne and grounded rotor pairs.");
				double error = 0;
				foreach (var rotor in rotors)
				{
					var front = rotor.Offset.X > 0;
					var hubX = front ? 30.5 : -39.0;
					var hubZ = front ? 15.5 : 19.5;
					var x = (int)(hubX - limb.Bounds[0]);
					var y = (int)(-limb.Bounds[1]);
					if (Math.Abs(limb.VoxelMap[x, y].Keys.Max() + 1 + limb.Bounds[2] - hubZ) > .01)
						throw new InvalidDataException("Rotor hub no longer matches source voxel surface.");
					for (var i = 0; i < 32; i++)
					{
						var yaw = new WAngle(i * 32);
						var p = WithVoxelRotorOverlayInfo.ProjectOffset(body, rotor.Offset, WRot.FromYaw(yaw), grid);
						var angle = i * Math.PI / 16;
						var pitch = body.CameraPitch.Angle * Math.PI / 512;
						var dx = p.X * (double)grid.TileSize.Width / grid.TileScale + hubX * .65 * Math.Sin(angle);
						var dy = (p.Y - p.Z) * (double)grid.TileSize.Height / grid.TileScale + hubX * .65 * Math.Sin(pitch) * Math.Cos(angle) + hubZ * .65 * Math.Cos(pitch);
						error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
					}
				}
				if (error > .3) throw new InvalidDataException("Chinook hub projection error: " + error);
				Console.WriteLine($"{name}: both voxel hubs, air/ground,32 facings checked; max error{error:F3}px.");
			}
			Console.WriteLine("Scorch/crater:9 tileset palettes loaded; isometric maps use original RA indices and shadows. Live review pending.");
		}
	}
}
