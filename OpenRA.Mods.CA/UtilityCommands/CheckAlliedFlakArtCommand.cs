using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-allied-flak-art", "Validate approved Allied flak parts and physical muzzle registration.")]
	public sealed class CheckAlliedFlakArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-allied-flak-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["agun"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "agun.native" ||
				!actor.HasTraitInfo<WithSpriteTurretInfo>() || actor.HasTraitInfo<WithEmbeddedTurretSpriteBodyInfo>())
				throw new InvalidDataException("Allied flak parts not separated.");
			foreach (var item in new[] { ("flak-base.png", 1), ("flak-turret32.png", 32) })
			{
				var path = "bits/allies/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 256 || f.Size.Height != 256 || f.FrameSize != f.Size))
					throw new InvalidDataException("Incorrect native flak frames.");
			}

			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arm = actor.TraitInfo<ArmamentInfo>();
			if (arm.Weapon != "ZSU-23" || arm.LocalOffset.Length != 4 || turret.TurnSpeed.Angle != 60)
				throw new InvalidDataException("Unexpected firing or turning change.");
			var grid = mod.Manifest.Get<MapGrid>();
			if (body.UseClassicPerspectiveFudge || mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()].Burst != 1)
				throw new InvalidDataException("Flak requires actual-grid coordinates and cycling single shots.");
			var pitch = 37 * Math.PI / 180;
			var t = .56 + (3.92 - .56) * .8 - .16 - .28;
			double maxError = 0;
			for (var slot = 0; slot < 4; slot++)
			{
				var xx = slot == 0 || slot == 3 ? -.43 : .43;
				var zz = slot < 2 ? .34 : -.34;
				var yy = -.30 - Math.Cos(pitch) * t + Math.Sin(pitch) * zz;
				var z = 3.54 + Math.Sin(pitch) * t + Math.Cos(pitch) * zz;
				for (var i = 0; i < 32; i++)
				{
					var angle = (20 + i) * Math.PI / 16;
					var x = xx * Math.Cos(angle) - yy * Math.Sin(angle);
					var y = xx * Math.Sin(angle) + yy * Math.Cos(angle);
					var muzzle = body.LocalToWorld(turret.Offset + arm.LocalOffset[slot].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					var dx = muzzle.X * (double)grid.TileSize.Width / grid.TileScale - 20 * (x + y) / Math.Sqrt(2);
					var dy = (muzzle.Y - muzzle.Z) * (double)grid.TileSize.Height / grid.TileScale - (20 * (x - y) / Math.Sqrt(8) - 20 * z * Math.Sqrt(3) / 2);
					maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
				}
			}

			if (maxError > 1) throw new InvalidDataException($"Allied muzzle error: {maxError:F3}px");
			Console.WriteLine($"Allied flak: native base+32 turret frames, traits and four sequential muzzle slots valid ({maxError:F3}px).");
		}
	}
}
