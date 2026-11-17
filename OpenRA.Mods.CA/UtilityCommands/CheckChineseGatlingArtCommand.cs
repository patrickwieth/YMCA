using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckChineseGatlingArtCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-chinese-gatling-art";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		static void CheckFlash(IReadOnlyDictionary<string, ISpriteSequence> sequences)
		{
			const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
			var set = (SequenceSet)RuntimeHelpers.GetUninitializedObject(typeof(SequenceSet));
			typeof(SequenceSet).GetField("images", Fields).SetValue(set,
				new Dictionary<string, IReadOnlyDictionary<string, ISpriteSequence>> { ["fixture"] = sequences });
			var animation = (Animation)RuntimeHelpers.GetUninitializedObject(typeof(Animation));
			typeof(Animation).GetField("sequences", Fields).SetValue(animation, set);
			typeof(Animation).GetProperty("Name").SetValue(animation, "fixture");
			var visible = true;
			animation.PlayThen("muzzle", () => visible = false);
			if (RenderSprites.ShouldSuppressBuildingAnimation(animation))
				throw new InvalidDataException("Static building presentation must retain weapon muzzle flashes.");
			animation.Tick();
			if (!visible) throw new InvalidDataException("Gatling flash expires before its first render.");
			animation.Tick();
			if (visible) throw new InvalidDataException("Gatling flash exceeds its80ms twin-barrel window.");
		}
		[Desc("Validate native Chinese Gatling,1x1 bib, eight weapon stages and shared twin-gun flashes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var binding = JObject.Parse(File.ReadAllText("art-source/chinese gatling/integration/binding.json"));
			var bodySize = binding["body_frame_size"]?.Values<int>().ToArray() ?? new[] { 320, 320 };
			var bodyScale = (float)(binding["body_scale"] ?? binding["scale"]);
			var bodyOffset = binding["body_offset"].Values<float>().ToArray();
			var actor = mod.DefaultRules.Actors["chgatling"];
			_ = actor.TraitsInConstructOrder().ToArray();
			var body = actor.TraitInfo<BodyOrientationInfo>();
			var turret = actor.TraitInfo<TurretedInfo>();
			var arms = actor.TraitInfos<ArmamentInfo>().ToArray();
			if (actor.TraitInfo<BuildingInfo>().Dimensions != new CVec(1, 1) || actor.TraitInfo<RenderSpritesInfo>().Image != "chgatling.native" ||
				actor.TraitInfo<WithSpriteBodyInfo>().Sequence != "base" || !actor.HasTraitInfo<SharedBarrelSequenceInfo>() ||
				body.UseClassicPerspectiveFudge || body.QuantizedFacings != 32 || turret.TurnSpeed.Angle != 32 ||
				!actor.TraitInfos<WithIdleOverlayInfo>().Any(t => t.Image == "sovietbib1x1") || arms.Length != 8 ||
				arms.Any(a => !(a is SharedBarrelArmamentInfo) || a.LocalOffset.Length != 2))
				throw new InvalidDataException("Chinese Gatling attachment/stage/1x1 foundation regression.");
			var reloads = new[] { 12, 8, 4, 2 };
			for (var i = 0; i < 4; i++)
				foreach (var suffix in new[] { "", "G" })
				{
					var arm = arms.Single(a => a.Weapon == "ChinaMGatt." + i + suffix);
					var weapon = mod.DefaultRules.Weapons[arm.Weapon.ToLowerInvariant()];
					var targets = suffix == "" ? new BitSet<TargetableType>("Air") : new BitSet<TargetableType>("Ground", "Water");
					if (weapon.Burst != 1 || weapon.ReloadDelay != reloads[i] || !weapon.ValidTargets.Equals(targets))
						throw new InvalidDataException("China Gatling targeting/spin-up profile changed: " + arm.Weapon);
				}
			var phase = new SharedBarrelSequence();
			for (var i = 0; i < 64; i++)
				if (phase.TakeNext() != i % 2) throw new InvalidDataException("Shared left/right phase failed.");
			foreach (var item in new[] { ("gatling-base.png", 1), ("gatling-turret32.png", 32) })
			{
				var path = "bits/china/buildings/" + item.Item1;
				using var stream = mod.DefaultFileSystem.Open(path);
				var frames = FrameLoader.GetFrames(stream, mod.SpriteLoaders, path, out _);
				var expected = item.Item2 == 1 ? bodySize : new[] { 320, 320 };
				if (frames.Length != item.Item2 || frames.Any(f => f.Size.Width != expected[0] || f.Size.Height != expected[1] || f.Type == SpriteFrameType.Indexed8))
					throw new InvalidDataException("Native Gatling frame layout: " + path);
			}
			var node = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).Single(n => n.Key == "chgatling.native");
			var names = new[] { "Defaults", "idle", "damaged-idle", "base", "damaged-base", "dead", "turret", "damaged-turret", "muzzle", "icon", "construction" };
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => names.Contains(n.Key)).ToArray()));
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			if (sequences["turret"].Facings != 32 || sequences["damaged-turret"].Facings != 32 ||
				sequences["base"].Scale != bodyScale || sequences["turret"].Scale != .65f ||
				Math.Abs(sequences["base"].GetSprite(0).Offset.X - bodyOffset[0]) > .0001 ||
				Math.Abs(sequences["base"].GetSprite(0).Offset.Y - bodyOffset[1]) > .0001 || sequences["turret"].GetSprite(0).Offset.Y != -34 ||
				sequences["construction"].Length != 10 || sequences["muzzle"].Length != 2 || sequences["muzzle"].Facings != 8)
				throw new InvalidDataException("Chinese Gatling sequence/placement mismatch.");
			for (var i = 0; i < 8; i++)
				for (var j = 0; j < 2; j++) _ = sequences["muzzle"].GetSprite(j, new WAngle(i * 128));
			using (var flashStream = mod.DefaultFileSystem.Open("minigun.shp"))
			{
				var frames = FrameLoader.GetFrames(flashStream, mod.SpriteLoaders, "minigun.shp", out _);
				for (var i = 0; i < 8; i++)
					for (var j = 0; j < 2; j++)
						if (frames[i * 6 + j * 2].Data == null || !frames[i * 6 + j * 2].Data.Any(p => p > 4))
							throw new InvalidDataException("Selected Gatling flash frame is empty/shadow-only. Visible frames: " +
								string.Join(",", frames.Select((f, n) => (f, n)).Where(p => p.f.Data != null && p.f.Data.Any(v => v > 4)).Select(p => p.n)));
			}
			CheckFlash(sequences);
			var grid = mod.Manifest.Get<MapGrid>();
			var residual = 30 * .65 - turret.Offset.Z * (double)grid.TileSize.Height / grid.TileScale;
			double error = 0;
			foreach (var arm in arms)
				for (var i = 0; i < 32; i++)
					for (var j = 0; j < 2; j++)
					{
						var point = body.LocalToWorld(turret.Offset + arm.LocalOffset[j].Rotate(WRot.FromYaw(new WAngle(i * 32))));
						var angle = (4 + i) * Math.PI / 16;
						var lateral = j == 0 ? -1.25 : 1.25;
						var x = lateral * Math.Cos(angle) - 3.025 * Math.Sin(angle);
						var y = lateral * Math.Sin(angle) + 3.025 * Math.Cos(angle);
						var dx = point.X * (double)grid.TileSize.Width / grid.TileScale - 28 * .65 * (x + y) / Math.Sqrt(2);
						var dy = (point.Y - point.Z) * (double)grid.TileSize.Height / grid.TileScale -
							(28 * .65 * ((x - y) / Math.Sqrt(8) - 3.5775 * Math.Sqrt(3) / 2) + residual);
						error = Math.Max(error, Math.Sqrt(dx * dx + dy * dy));
					}
			if (error > .4) throw new InvalidDataException("Gatling muzzle registration: " + error);
			Console.WriteLine($"Chinese Gatling native32:1x1, fixed Soviet bib, AA/ground spin-up preserved, shared twin-gun phase,80ms flash;512 muzzle projections max{error:F3}px. Live review pending.");
		}
	}
}
