using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits.Render;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.UtilityCommands
{
	public sealed class CheckBuildingPresentationCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-building-presentation";
		bool IUtilityCommand.ValidateArguments(string[] args) => args.Length == 1;
		const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

		static Animation Animation(IReadOnlyDictionary<string, ISpriteSequence> sequences)
		{
			var set = (SequenceSet)RuntimeHelpers.GetUninitializedObject(typeof(SequenceSet));
			typeof(SequenceSet).GetField("images", Fields).SetValue(set,
				new Dictionary<string, IReadOnlyDictionary<string, ISpriteSequence>> { ["fixture"] = sequences });
			var animation = (Animation)RuntimeHelpers.GetUninitializedObject(typeof(Animation));
			typeof(Animation).GetField("sequences", Fields).SetValue(animation, set);
			typeof(Animation).GetField("facingFunc", Fields).SetValue(animation, (Func<WAngle>)(() => WAngle.Zero));
			typeof(Animation).GetProperty("Name").SetValue(animation, "fixture");
			return animation;
		}

		static int CompletionTicks(IReadOnlyDictionary<string, ISpriteSequence> sequences, string sequence, bool frozen, bool reverse)
		{
			var animation = Animation(sequences);
			if (frozen) animation.RenderSequenceOverride = () => sequences["idle"];
			var complete = false;
			if (reverse) animation.PlayBackwardsThen(sequence, () => complete = true);
			else animation.PlayThen(sequence, () => complete = true);
			for (var tick = 1; tick <= 10000; tick++)
			{
				if (frozen && (!ReferenceEquals(animation.Image, sequences["idle"].GetSprite(0)) || RenderSprites.ShouldSuppressBuildingAnimation(animation)))
					throw new InvalidDataException("Static body presentation did not retain idle frame zero.");
				animation.Tick();
				if (complete) return tick;
			}
			throw new InvalidDataException("Animation completion callback never fired.");
		}

		[Desc("Check static building presentation with unchanged clocks, placement markers, vehicle boxes and requested Soviet changes.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var mod = Game.ModData = utility.ModData;
			var nodes = MiniYaml.Load(mod.DefaultFileSystem, mod.Manifest.Sequences, null).ToDictionary(n => n.Key);
			using var cache = new SpriteCache(mod.DefaultFileSystem, mod.SpriteLoaders, mod.SpriteSequenceLoader.BgraSheetSize, mod.SpriteSequenceLoader.IndexedSheetSize);
			var selectedNames = new[] { "Defaults", "idle", "damaged-idle", "construction", "build" };
			var node = nodes["fact.soviet"];
			var selected = new MiniYamlNode(node.Key, new MiniYaml(null, node.Value.Nodes.Where(n => selectedNames.Contains(n.Key)).ToArray()));
			var sequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in sequences.Values) sequence.ResolveSprites(cache);
			// Exercise the actual render-wrapper visibility gate and condition observer,
			// not just YAML policy: hidden construction must still finish normally.
			foreach (var reverse in new[] { false, true })
			{
				var renderer = (RenderSprites)RuntimeHelpers.GetUninitializedObject(typeof(RenderSprites));
				typeof(RenderSprites).GetField("Info").SetValue(renderer, mod.DefaultRules.Actors["sfac"].TraitInfo<RenderSpritesInfo>());
				var observer = renderer.GetVariableObservers().Single();
				void Incomplete(int count) => observer.Notifier(null, new Dictionary<string, int> { ["build-incomplete"] = count });
				var body = Animation(sequences);
				new WithSpriteBodyInfo().ConfigureStaticPresentation(body, () => DamageState.Undamaged, true);
				var bib = Animation(sequences);
				bib.PlayRepeating("idle");
				var wrapperType = typeof(RenderSprites).GetNestedType("AnimationWrapper", BindingFlags.NonPublic);
				object Wrap(Animation animation) => Activator.CreateInstance(wrapperType, new object[] { new AnimationWithOffset(animation, null, null), null, false, true, (Func<bool>)(() => (bool)typeof(RenderSprites).GetField("hideBody", Fields).GetValue(renderer)) });
				var bodyWrapper = Wrap(body);
				var bibWrapper = Wrap(bib);
				bool Visible(object wrapper) => (bool)wrapperType.GetProperty("IsVisible").GetValue(wrapper);
				var finished = false;
				void Finish() { finished = true; Incomplete(0); }
				Incomplete(1);
				if (reverse) body.PlayBackwardsThen("construction", Finish);
				else body.PlayThen("construction", Finish);
				var ticks = 0;
				while (!finished && ticks++ < 1000)
				{
					if (Visible(bodyWrapper) || !Visible(bibWrapper)) throw new InvalidDataException("Scrin construction must show the bib, not the body.");
					body.Tick();
				}
				if (!finished || !Visible(bodyWrapper) || !Visible(bibWrapper) || ticks != CompletionTicks(sequences, "construction", false, reverse))
					throw new InvalidDataException("Hidden Scrin construction completion/visibility regression.");
			}
			foreach (var state in new[] { "construction", "build" }.Where(sequences.ContainsKey))
				foreach (var reverse in new[] { false, true })
				{
					var normal = CompletionTicks(sequences, state, false, reverse);
					var frozen = CompletionTicks(sequences, state, true, reverse);
					if (normal != frozen) throw new InvalidDataException("Construction/sell/production callback timing changed.");
					Console.WriteLine(state + (reverse ? " reverse" : " forward") + ": static/native completion both " + normal + " ticks.");
				}
			foreach (var name in new[] { "fact.soviet", "afac.nod", "chpower", "sfac", "chbunker" })
			{
				var actor = mod.DefaultRules.Actors[name];
				if (!actor.TraitInfo<RenderSpritesInfo>().SuppressBuildingAnimations || !actor.TraitInfos<WithSpriteBodyInfo>().Any(b => b.UseIdleFrameForRendering))
					throw new InvalidDataException(name + ": static presentation missing.");
				if (actor.TraitInfo<WithMakeAnimationInfo>().Condition != "build-incomplete")
					throw new InvalidDataException(name + ": construction completion condition changed.");
			}
			var scrinBuildings = new[] { "sfac", "sfac.nukular", "reac", "rea2", "proc.scrin", "port", "wsph", "nerv", "scrt", "grav", "sign", "srep", "silo.scrin", "mani", "rift", "ptur", "shar", "scol", "scol.temp" };
			foreach (var name in new[] { "fact.soviet", "fact", "afac", "afac.nod", "chpower", "chpower2" }.Concat(scrinBuildings))
			{
				var actor = mod.DefaultRules.Actors[name];
				var info = actor.TraitInfo<RenderSpritesInfo>();
				var scrin = scrinBuildings.Contains(name);
				if (info.AnimatedBuildingSequences.Contains("construction") == scrin)
					throw new InvalidDataException(name + ": faction construction presentation regression.");
				foreach (var reverse in new[] { false, true })
				{
					var animation = Animation(sequences);
					// Use the resolved faction policy with a common real construction fixture.
					new WithSpriteBodyInfo().ConfigureStaticPresentation(animation, () => DamageState.Undamaged, true, info.AnimatedBuildingSequences);
					var complete = false;
					if (reverse) animation.PlayBackwardsThen("construction", () => complete = true);
					else animation.PlayThen("construction", () => complete = true);
					var ticks = 0;
					while (!complete && ticks++ < 1000)
					{
						var expected = scrin ? sequences["idle"].GetSprite(0) : sequences["construction"].GetSprite(animation.CurrentFrame);
						if (!ReferenceEquals(animation.Image, expected) || RenderSprites.ShouldSuppressBuildingAnimation(animation))
							throw new InvalidDataException(name + ": incorrect live construction frame.");
						animation.Tick();
					}
					if (ticks != CompletionTicks(sequences, "construction", false, reverse))
						throw new InvalidDataException(name + ": construction callback timing changed.");
					animation.PlayRepeating("build");
					animation.Tick();
					if (!ReferenceEquals(animation.Image, sequences["idle"].GetSprite(0)))
						throw new InvalidDataException(name + ": obsolete production animation re-enabled.");
				}
			}
			Console.WriteLine("China construction stages animate for non-Scrin factions only; production stays static, forward/reverse clocks unchanged.");
			foreach (var name in new[] { "harv.scrin", "harv", "1tnk" })
			{
				var actor = mod.DefaultRules.Actors[name];
				if (actor.TraitInfo<RenderSpritesInfo>().SuppressBuildingAnimations || actor.TraitInfo<IsometricSelectionDecorationsForUnitsInfo>().Height != 24)
					throw new InvalidDataException(name + ": vehicle animation/decoration regression.");
			}
			var obeliskNode = nodes["obli.custom"];
			selectedNames = new[] { "Defaults", "idle", "damaged-idle", "active" };
			selected = new MiniYamlNode(obeliskNode.Key, new MiniYaml(null, obeliskNode.Value.Nodes.Where(n => selectedNames.Contains(n.Key)).ToArray()));
			var obeliskSequences = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var sequence in obeliskSequences.Values) sequence.ResolveSprites(cache);
			var charge = Animation(obeliskSequences);
			charge.PlayRepeating("active");
			mod.DefaultRules.Actors["obli"].TraitInfo<WithChargeSpriteBodyInfo>().ConfigureStaticPresentation(charge, () => DamageState.Undamaged, true);
			charge.Tick();
			if (!ReferenceEquals(charge.Image, obeliskSequences["idle"].GetSprite(0)) || RenderSprites.ShouldSuppressBuildingAnimation(charge) || charge.CurrentSequence.Name != "active")
				throw new InvalidDataException("Charged building must keep its new idle art visible without changing its charge controller.");
			var powerNode = nodes["powr.soviet"];
			selected = new MiniYamlNode(powerNode.Key, new MiniYaml(null, powerNode.Value.Nodes.Where(n => n.Key == "Defaults" || n.Key == "icon").ToArray()));
			var powerIcon = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected)["icon"];
			cache.LoadReservations(mod);
			powerIcon.ResolveSprites(cache);
			if (powerIcon.Scale != 1 || powerIcon.GetSprite(0).Offset.X != 0 || powerIcon.GetSprite(0).Offset.Y != 0)
				throw new InvalidDataException("Soviet power-plant cameo inherited a building offset.");
			Console.WriteLine("Soviet power-plant cameo: native centered sidebar sprite loaded.");
			var overlay = nodes["overlay"];
			selectedNames = new[] { "Defaults", "build-valid", "build-invalid", "target-valid" };
			selected = new MiniYamlNode(overlay.Key, new MiniYaml(null, overlay.Value.Nodes.Where(n => selectedNames.Contains(n.Key)).ToArray()));
			var markers = mod.SpriteSequenceLoader.ParseSequences(mod, "RUBBERDUCK-TEMPERATE", cache, selected);
			cache.LoadReservations(mod);
			foreach (var marker in markers.Values) marker.ResolveSprites(cache);
			foreach (var name in new[] { "build-valid", "build-invalid" })
				if (markers[name].Scale != 2 || markers[name].GetSprite(0).Size.X * markers[name].Scale != 120 || markers[name].GetSprite(0).Size.Y * markers[name].Scale != 60 || markers[name].ZOffset != 4096)
					throw new InvalidDataException("Placement marker scale/depth: " + name);
			if (markers["target-valid"].Scale != 1) throw new InvalidDataException("Unrelated targeting marker changed.");
			var footprint = RuntimeHelpers.GetUninitializedObject(typeof(FootprintPlaceBuildingPreviewPreview));
			typeof(FootprintPlaceBuildingPreviewPreview).GetField("validZOffset", Fields).SetValue(footprint, 4096);
			typeof(FootprintPlaceBuildingPreviewPreview).GetField("blockedZOffset", Fields).SetValue(footprint, 4096);
			var above = (PlaceBuildingCellType)typeof(FootprintPlaceBuildingPreviewPreview).GetProperty("SequenceFootprintOverPreview", Fields).GetValue(footprint);
			if (above != (PlaceBuildingCellType.Valid | PlaceBuildingCellType.Invalid | PlaceBuildingCellType.LineBuild))
				throw new InvalidDataException("Placement markers are not above previews.");
			var nuclear = mod.DefaultRules.Actors["apwr.soviet"];
			var explosion = nuclear.TraitInfos<FireWarheadsOnDeathInfo>().Single(w => w.Weapon == "CHNukeCannon.Impact");
			if (nuclear.TraitInfo<ValuedInfo>().Cost != 1500 || nuclear.TraitInfo<PowerInfo>().Amount != 800 ||
				explosion.Type != ExplosionType.CenterPosition || explosion.Chance != 100 || explosion.DamageThreshold != 0 ||
				!ReferenceEquals(explosion.WeaponInfo, mod.DefaultRules.Weapons["chnukecannon.impact"]) || explosion.EmptyWeapon != explosion.Weapon)
				throw new InvalidDataException("Soviet nuclear plant balance/single-impact death regression.");
			if (mod.DefaultRules.Actors["apwr"].TraitInfo<ValuedInfo>().Cost != 500 || mod.DefaultRules.Actors["tpwr"].TraitInfo<ValuedInfo>().Cost != 600)
				throw new InvalidDataException("Unrelated power plants changed.");
			foreach (var name in new[] { "blimp", "promotion.airship" })
				if (!mod.DefaultRules.Actors[name].TraitInfo<BuildableInfo>().Prerequisites.Contains("~mode.operational"))
					throw new InvalidDataException(name + ": operational-only visibility gate missing.");
			Console.WriteLine("Static presentation/clocks, double-size foreground markers, vehicle decorations, operational Airship and Soviet nuclear death impact checked. Live review pending.");
		}
	}
}
