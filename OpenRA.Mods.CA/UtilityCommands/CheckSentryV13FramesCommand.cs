using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	[Desc("--check-sentry-v13-frames", "Load native32 and retained V13 sheets through the engine sprite loaders.")]
	public sealed class CheckSentryV13FramesCommand : IUtilityCommand
	{
		// Headless test fixture: supply only the sequence lookup, not a GPU/world.
		// Exercise real Animation.PlayThen/Tick, including the completion callback
		// that WithMuzzleOverlay uses to hide the flash before rendering.
		static void CheckFlashLifetime(IReadOnlyDictionary<string, ISpriteSequence> sequences)
		{
			const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
			var set = (SequenceSet)RuntimeHelpers.GetUninitializedObject(typeof(SequenceSet));
			typeof(SequenceSet).GetField("images", Fields).SetValue(set,
				new Dictionary<string, IReadOnlyDictionary<string, ISpriteSequence>> { ["fixture"] = sequences });
			var animation = (Animation)RuntimeHelpers.GetUninitializedObject(typeof(Animation));
			typeof(Animation).GetField("sequences", Fields).SetValue(animation, set);
			typeof(Animation).GetProperty("Name").SetValue(animation, "fixture");
			var visible = true;
			animation.PlayThen("muzzle-mg", () => visible = false);
			animation.Tick(); // attack and overlay update may occur in the same tick
			if (!visible) throw new InvalidDataException("Sentry flash expires before its first render (40ms tick).");
			animation.Tick();
			if (visible) throw new InvalidDataException("Sentry flash overlaps the next barrel at full Gatling speed.");
			Console.WriteLine("Sentry flash: survives first40ms engine tick, expires at80ms; alternating barrels remain visible.");
		}

		string IUtilityCommand.Name => "--check-sentry-v13-frames";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;

		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			// Validate trait dependencies for the separate, non-buildable test actor too.
			var prototype = modData.DefaultRules.Actors["sentry.soviet"];
			_ = prototype.TraitsInConstructOrder().ToArray();
			var arms = prototype.TraitInfos<ArmamentInfo>().ToArray();
			if (arms.Length != 4 || arms.Any(a => a.Name != "secondary" || a.MuzzleSequence != "muzzle-mg") || !prototype.TraitsInConstructOrder().Any(t => t.GetType().Name == "WithMuzzleOverlayInfo"))
				throw new InvalidDataException("Sentry ground-only Gatling/muzzle not bound.");
			for (var stage = 0; stage < 4; stage++)
			{
				var conditions = new Dictionary<string, int> { ["gatling-ground"] = stage };
				var active = arms.Where(a => a.RequiresCondition.Evaluate(conditions)).ToArray();
				if (active.Length != 1 || active[0].Weapon != "ChinaMGatt." + stage + "G") throw new InvalidDataException("Sentry Gatling stage selection.");
				var weapon = modData.DefaultRules.Weapons[active[0].Weapon.ToLowerInvariant()];
				if (weapon.ValidTargets.Overlaps(new BitSet<TargetableType>("Air")) || weapon.ReloadDelay != new[] { 12, 8, 4, 2 }[stage]) throw new InvalidDataException("Sentry AA or cadence regression.");
			}
			var ramp = prototype.TraitInfos<GrantConditionOnAttackCAInfo>().Single();
			if (ramp.Condition != "gatling-ground" || !ramp.ArmamentNames.SetEquals(new[] { "secondary" }) || !ramp.RequiredShotsPerInstance.SequenceEqual(new[] { 1, 3, 5 }) || ramp.MaximumInstances != 3 || ramp.RevokeDelay != 40 || ramp.RevokeOnNewTarget || !ramp.RevokeAll)
				throw new InvalidDataException("Sentry must use the Chinese ground Gatling spin-up/cooldown.");
			var arm = arms[0];
			if (!prototype.HasTraitInfo<SharedBarrelSequenceInfo>() || arms.Any(a => a is not SharedBarrelArmamentInfo || a.LocalOffset.Length != 2 || !a.LocalOffset.SequenceEqual(arm.LocalOffset)))
				throw new InvalidDataException("All Gatling stages must share the same two calibrated muzzles.");
			var alternating = new SharedBarrelSequence();
			// One phase survives arbitrary stage/cooldown/target transitions.
			foreach (var shot in Enumerable.Range(0, 100))
				if (alternating.TakeNext() != shot % 2) throw new InvalidDataException("Barrel alternation regressed.");
			if (modData.DefaultRules.Actors["ftur"].TraitInfo<ArmamentInfo>().Weapon != "FireballLauncher")
				throw new InvalidDataException("Stock flame tower changed.");
			Console.WriteLine("Sentry: Chinese ground Gatling stages0G–3G,12/8/4/2 cadence,1/3/5 spin-up,40-tick cooldown; no AA armaments.");
			var nodes = MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Sequences, null);
			var node = nodes.Single(n => n.Key == "ftur.soviet.v13");
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => n.Key == "muzzle-mg").ToArray()));
			using var cache = new SpriteCache(modData.DefaultFileSystem, modData.SpriteLoaders, modData.SpriteSequenceLoader.BgraSheetSize, modData.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = modData.SpriteSequenceLoader.ParseSequences(modData, "TEMPERAT", cache, selected);
			cache.LoadReservations(modData);
			foreach (var seq in sequences.Values) seq.ResolveSprites(cache);
			if (sequences["muzzle-mg"].Length != 2) throw new InvalidDataException("MG flash frames.");
			CheckFlashLifetime(sequences);
			var body = prototype.TraitInfo<BodyOrientationInfo>();
			var socket = prototype.TraitInfo<TurretedInfo>().Offset;
			var grid = modData.Manifest.Get<MapGrid>();
			if (body.UseClassicPerspectiveFudge) throw new InvalidDataException("Sentry actual-grid projection requires no classic fudge.");
			double error = 0;
			for (var barrel = 0; barrel < 2; barrel++)
				for (var i = 0; i < 32; i++)
				{
					var angle = i * Math.PI / 16;
					var side = barrel == 0 ? 1 : -1;
					var muzzle = body.LocalToWorld(socket + arm.LocalOffset[barrel].Rotate(WRot.FromYaw(new WAngle(i * 32))));
					// Independent centers of both six-bore clusters, frozen native Blender model.
					var dx = muzzle.X * (double)grid.TileSize.Width / grid.TileScale - (-.228759 - 48.373334 * Math.Sin(angle) + side * 7.64444 * Math.Cos(angle));
					var dy = (muzzle.Y - muzzle.Z) * (double)grid.TileSize.Height / grid.TileScale - (-33.915095 - 24.186666 * Math.Cos(angle) - side * 3.82222 * Math.Sin(angle));
					error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
				}
			if (error > .6) throw new InvalidDataException("Sentry MG muzzle projection: " + error);
			Console.WriteLine($"SENTRY.SOVIET: ground Gatling, shared alternating barrels and short flashes loaded; actual-grid64 muzzle projections max {error:F3}px; stock FTUR unchanged. Live firing pending.");
			foreach (var item in new[]
			{
				(File: "sentry-v13-native32.png", Count: 32, Width: 120, Height: 120),
				(File: "sentry-base-native.png", Count: 1, Width: 74, Height: 43),
				(File: "sentry-v13.png", Count: 8, Width: 240, Height: 240)
			})
			{
				var filename = "bits/soviet/buildings/experimental/" + item.File;
				using var stream = modData.DefaultFileSystem.Open(filename);
				var frames = FrameLoader.GetFrames(stream, modData.SpriteLoaders, filename, out _);
				if (frames.Length != item.Count)
					throw new InvalidDataException($"{filename}: engine loaded {frames.Length} frames instead of {item.Count}.");

				foreach (var frame in frames)
					if (frame.Size.Width != item.Width || frame.Size.Height != item.Height ||
						frame.FrameSize != frame.Size || frame.Offset != float2.Zero || frame.Data.Length != item.Width * item.Height * 4)
						throw new InvalidDataException(filename + ": invalid native frame size, offset or pixel data.");

				Console.WriteLine($"{item.File}: {frames.Length} frames, each {item.Width}x{item.Height}, zero embedded offset.");
			}

			for (var i = 0; i < 32; i++)
			{
				var yaw = new WAngle(i * 32);
				var direction = new WVec(-yaw.Sin(), -yaw.Cos(), 0);
				if (OpenRA.Mods.Common.Util.IndexFacing(direction.Yaw, 32) != i)
					throw new InvalidDataException("Unexpected engine facing convention.");
			}
		}
	}
}
