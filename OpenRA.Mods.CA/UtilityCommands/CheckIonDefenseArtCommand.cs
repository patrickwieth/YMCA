using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Cnc.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-ion-defense-art", "Load the separate native Ion Defense sheets and validate its render traits.")]
	public sealed class CheckIonDefenseArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-ion-defense-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["ion_defense"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (!actor.HasTraitInfo<WithSpriteTurretInfo>() || actor.HasTraitInfo<WithEmbeddedTurretSpriteBodyInfo>())
				throw new InvalidDataException("Expected a separate sprite turret and stationary body.");
			if (actor.TraitInfo<WithSpriteBodyInfo>().Sequence != "base" ||
				actor.TraitInfo<RenderSpritesInfo>().Image != "ion_defense.native")
				throw new InvalidDataException("Incorrect Ion Defense image/body binding.");
			foreach (var item in new[] { ("heavy-blaster-base.png", 1), ("heavy-blaster-turret32.png", 32) })
			{
				var filename = "bits/gdi/buildings/color-matched/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(filename);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, filename, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 160 || f.Size.Height != 160 ||
					f.FrameSize != f.Size || f.Offset != float2.Zero || f.Data.Length != 160 * 160 * 4))
					throw new InvalidDataException(filename + ": invalid native frames or embedded PNG metadata.");
				Console.WriteLine($"{item.Item1}: engine loaded {frames.Length} native 160x160 frame(s).");
			}

			using (var stream = mod.DefaultFileSystem.Open("bits/gdi/buildings/bib2x2.png"))
			{
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, "bits/gdi/buildings/bib2x2.png", out _);
				if (frames.Length != 1 || frames[0].Size.Width != 256 || frames[0].Size.Height != 128)
					throw new InvalidDataException("Ion Defense requires the native 2x2 GDI bib.");
			}

			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arm = actor.TraitInfo<ArmamentInfo>();
			var offset = body.LocalToWorld(turret.Offset);
			const double Pixels = 64.0 / 1448;
			var pivotX = offset.X * Pixels + 81.06066021392667 - 80 - 1;
			var pivotY = (offset.Y - offset.Z) * Pixels + 89.57754749356471 - 80 - 10;
			// Plinth polygon centroid projected at the unchanged .7 model-unit pivot height.
			var centerDx = pivotX - (87.10539547314792 - 87);
			var centerDy = pivotY - (93.99304267936189 - 20 * .05 * Math.Sqrt(3) / 2 - 104);
			var centerError = Math.Sqrt(centerDx * centerDx + centerDy * centerDy);
			if (centerError > .7)
				throw new InvalidDataException($"Ion Defense turret is off-center on its platform: {centerError:F3}px.");
			Console.WriteLine($"Ion Defense: native 2x2 bib loaded; platform centering error {centerError:F3}px.");
			double maxError = 0;
			for (var i = 0; i < 32; i++)
			{
				var muzzle = offset + body.LocalToWorld(arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
				var angle = ((20 + i) % 32) * Math.PI / 16;
				var x = 3.605 * Math.Sin(angle);
				var y = -3.605 * Math.Cos(angle);
				var expectedX = pivotX + 20 * (x + y) / Math.Sqrt(2);
				var expectedY = pivotY + 20 * (x - y) / Math.Sqrt(8) - 20 * 1.7225 * Math.Sqrt(3) / 2;
				var dx = muzzle.X * Pixels - expectedX;
				var dy = (muzzle.Y - muzzle.Z) * Pixels - expectedY;
				maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
			}

			if (maxError > 1.5)
				throw new InvalidDataException($"Ion Defense muzzle projection error: {maxError:F3}px.");
			Console.WriteLine($"Ion Defense: separate traits and 32 muzzle projections passed (max {maxError:F3}px). Live GPU/firing appearance remains untested.");
		}
	}
}
