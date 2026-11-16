using System;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckSonicArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-sonic-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		[Desc("Load approved Sonic parts/sequences and validate physical muzzle projection.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var actor = mod.DefaultRules.Actors["stwr"];
			_ = actor.TraitsInConstructOrder().ToArray();
			if (actor.TraitInfo<RenderSpritesInfo>().Image != "stwr.native" || !actor.HasTraitInfo<WithSpriteTurretInfo>())
				throw new InvalidDataException("Sonic parts not bound.");
			foreach (var item in new[] { ("sonic-base.png", 1), ("sonic-turret32.png", 32) })
			{
				var path = "bits/gdi/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != 224 || f.Size.Height != 224))
					throw new InvalidDataException("Incorrect native Sonic frames.");
			}
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "stwr.native");
			var names = new[] { "Defaults", "idle", "damaged-idle", "base", "damaged-base", "turret", "damaged-turret", "dead", "construction" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "TEMPERAT", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["turret"].Facings != 32 || sequences["base"].Length != 1 || sequences["construction"].Length != 10 || Math.Abs(sequences["turret"].Scale - .65f) > .0001f || Math.Abs(sequences["base"].Scale - .65f) > .0001f)
				throw new InvalidDataException("Incorrect Sonic sequence layout.");
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arm = actor.TraitInfo<ArmamentInfo>();
			if (arm.Weapon != "SonicPulse" || turret.TurnSpeed.Angle != 20)
				throw new InvalidDataException("Unexpected Sonic weapon or turning change.");
			var forward = .2 + 3.091 / Math.Sqrt(1 + .16 * .16);
			var height = 4.52 + 3.091 * .16 / Math.Sqrt(1 + .16 * .16);
			double maxError = 0;
			for (var i = 0; i < 32; i++)
			{
				var a = (12 + i) * Math.PI / 16;
				var x = forward * Math.Cos(a); var y = forward * Math.Sin(a);
				var muzzle = body.LocalToWorld(turret.Offset + arm.LocalOffset[0].Rotate(WRot.FromYaw(new WAngle(i * 32))));
				var dx = muzzle.X * 64.0 / 1448 - 13 * (x + y) / Math.Sqrt(2);
				var dy = (muzzle.Y - muzzle.Z) * 64.0 / 1448 - (13 * (x - y) / Math.Sqrt(8) - 13 * height * Math.Sqrt(3) / 2);
				maxError = Math.Max(maxError, Math.Sqrt(dx * dx + dy * dy));
			}
			if (maxError > .5) throw new InvalidDataException("Muzzle registration: " + maxError);
			Console.WriteLine($"Sonic original 1+32 frames at 65% scale, construction, damaged states and dependencies loaded; max muzzle error {maxError:F3}px. Live firing review pending.");
		}
	}
}
