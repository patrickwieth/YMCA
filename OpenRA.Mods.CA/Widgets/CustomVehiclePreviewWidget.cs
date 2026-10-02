using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Modular;
using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets
{
	// Uses the actor render pipeline (sprites AND voxel models), not the sprite-only asset browser.
	// No actors are spawned, no orders issued, and no shared TraitInfo objects are modified.
	public sealed class CustomVehiclePreviewWidget : Widget
	{
		readonly ModData modData;
		readonly WorldRenderer wr;
		IActorPreview[] previews = Array.Empty<IActorPreview>();
		IFinalizedRenderable[] renderables = Array.Empty<IFinalizedRenderable>();
		string selection;
		WAngle facing = new WAngle(384);
		Rectangle bounds;
		long elapsed, lastTime;
		public bool Rotating = true;
		public string Status { get; private set; } = "Grafik-Vorlage";

		[ObjectCreator.UseCtor]
		public CustomVehiclePreviewWidget(ModData modData, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			wr = worldRenderer;
		}

		public static ActorInfo BuildPreviewActor(ModData modData, Ruleset rules, ActorInfo source, string faction)
		{
			// Static, initial presentation only: no battle, cargo, upgrade or damage simulation.
			var variables = new Dictionary<string, int> { [faction] = 1 };
			// GrantConditionInfo is internal to Common; inspect its serialized public fields.
			foreach (var grant in source.TraitInfos<ConditionalTraitInfo>().Where(t => t.GetType().Name == "GrantConditionInfo" && t.RequiresCondition == null))
			{
				var condition = FieldSaver.Save(grant).Nodes.FirstOrDefault(n => n.Key == "Condition")?.Value.Value;
				if (!string.IsNullOrEmpty(condition)) variables[condition] = 1;
			}
			foreach (var ammo in source.TraitInfos<AmmoPoolInfo>())
				if (!string.IsNullOrEmpty(ammo.AmmoCondition))
					variables[ammo.AmmoCondition] = ammo.InitialAmmo < 0 ? ammo.Ammo : Math.Min(ammo.InitialAmmo, ammo.Ammo);

			var traits = new List<TraitInfo>();
			foreach (var trait in source.TraitInfos<TraitInfo>())
			{
				if (trait is ConditionalTraitInfo conditional &&
					(trait is IRenderActorPreviewSpritesInfo || trait is IRenderActorPreviewVoxelsInfo))
				{
					if (conditional.RequiresCondition != null && !conditional.RequiresCondition.Evaluate(variables))
						continue;
					// The stock preview APIs use EnabledByDefault. Clone render-only infos with their
					// resolved initial condition removed; never alter the live rules' condition fields.
					var clone = modData.ObjectCreator.CreateObject<ConditionalTraitInfo>(trait.GetType().Name);
					// Copy public configuration fields, not private runtime/cache state. A YAML roundtrip
					// is unsafe here: null expressions and dictionary fields are not lossless in FieldSaver.
					foreach (var field in trait.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
						field.SetValue(clone, field.Name == nameof(ConditionalTraitInfo.RequiresCondition) ? null : field.GetValue(trait));
					clone.RulesetLoaded(rules, source);
					traits.Add(clone);
				}
				else
					traits.Add(trait);
			}
			return new ActorInfo(source.Name, traits.ToArray());
		}

		public void SetVehicle(string actorName, string faction)
		{
			var key = actorName + ":" + faction;
			if (key == selection) return;
			selection = key;
			previews = Array.Empty<IActorPreview>();
			renderables = Array.Empty<IFinalizedRenderable>();
			elapsed = 0; lastTime = Game.RunTime; facing = new WAngle(384);
			try
			{
				var actor = BuildPreviewActor(modData, wr.World.Map.Rules, wr.World.Map.Rules.Actors[actorName.ToLowerInvariant()], faction);
				var owner = wr.World.LocalPlayer ?? wr.World.WorldActor.Owner;
				var td = new TypeDictionary { new OwnerInit(owner), new FactionInit(faction), new DynamicFacingInit(() => facing) };
				var init = new ActorPreviewInitializer(actor, wr, td);
				previews = actor.TraitInfos<IRenderActorPreviewInfo>().SelectMany(p => p.RenderPreview(init)).ToArray();
				// Fit all headings once, avoiding size pumping while rotating and accounting for turrets.
				var allBounds = new List<Rectangle>();
				for (var yaw = 0; yaw < 1024; yaw += 32)
				{
					facing = new WAngle(yaw);
					foreach (var preview in previews) { preview.Tick(); allBounds.AddRange(preview.ScreenBounds(wr, WPos.Zero)); }
				}
				bounds = allBounds.Union();
				facing = new WAngle(384);
				if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("No visible preview geometry.");
				Status = actor.TraitInfos<RenderVoxelsInfo>().Any() ? "Voxel-Modell + Anbauteile" : "Sprite-Modell + Anbauteile";
			}
			catch (Exception e) { Fail(e); }
		}

		void Fail(Exception e)
		{
			previews = Array.Empty<IActorPreview>();
			renderables = Array.Empty<IFinalizedRenderable>();
			Status = "Vorschau nicht verfuegbar";
			Log.Write("debug", "Designer preview " + selection + ": " + e);
		}

		public override void Tick()
		{
			var now = Game.RunTime;
			if (Rotating) elapsed += Math.Max(0, now - lastTime);
			lastTime = now;
			facing = new WAngle(CustomPreviewMath.Facing(elapsed));
			try { foreach (var p in previews) p.Tick(); }
			catch (Exception e) { Fail(e); }
		}

		public override void PrepareRenderables()
		{
			if (previews.Length == 0) return;
			try
			{
				var scale = CustomPreviewMath.Fit(bounds.Width, bounds.Height, RenderBounds.Width - 24, RenderBounds.Height - 24);
				var origin = new int2(RenderBounds.X + RenderBounds.Width / 2 - (int)((bounds.Left + bounds.Width / 2f) * scale),
					RenderBounds.Y + RenderBounds.Height / 2 - (int)((bounds.Top + bounds.Height / 2f) * scale));
				renderables = previews.SelectMany(p => p.RenderUI(wr, origin, scale))
					.OrderBy(WorldRenderer.RenderableZPositionComparisonKey).Select(r => r.PrepareRender(wr)).ToArray();
			}
			catch (Exception e) { Fail(e); }
		}

		public override void Draw()
		{
			Game.Renderer.EnableScissor(RenderBounds);
			Game.Renderer.EnableAntialiasingFilter();
			try { foreach (var r in renderables) r.Render(wr); }
			catch (Exception e) { Fail(e); }
			finally { Game.Renderer.DisableAntialiasingFilter(); Game.Renderer.DisableScissor(); }
		}
	}
}
