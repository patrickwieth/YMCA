#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class ProductionTooltipLogicCA : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionTooltipLogicCA(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;
			var pm = player.PlayerActor.TraitOrDefault<PowerManager>();
			var pr = player.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var armorTypeLabel = widget.Get<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.Get<ImageWidget>("ARMORTYPE_ICON");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var strengthsLabel = widget.Get<LabelWidget>("STRENGTHS");
			var weaknessesLabel = widget.Get<LabelWidget>("WEAKNESSES");
			var attributesLabel = widget.Get<LabelWidget>("ATTRIBUTES");
			var versusLabel = widget.Get<LabelWidget>("VERSUS");
			var versusNoneLabel = widget.Get<LabelWidget>("VSNONE");
			var effectVersusNoneLabel = widget.Get<LabelWidget>("EFFECTVSNONE");
			var versusLightLabel = widget.Get<LabelWidget>("VSLIGHT");
			var effectVersusLightLabel = widget.Get<LabelWidget>("EFFECTVSLIGHT");
			var versusHeavyLabel = widget.Get<LabelWidget>("VSHEAVY");
			var effectVersusHeavyLabel = widget.Get<LabelWidget>("EFFECTVSHEAVY");
			var versusReflectorLabel = widget.Get<LabelWidget>("VSREFLECTOR");
			var effectVersusReflectorLabel = widget.Get<LabelWidget>("EFFECTVSREFLECTOR");
			var versusWoodLabel = widget.Get<LabelWidget>("VSWOOD");
			var effectVersusWoodLabel = widget.Get<LabelWidget>("EFFECTVSWOOD");
			var versusConcreteLabel = widget.Get<LabelWidget>("VSCONCRETE");
			var effectVersusConcreteLabel = widget.Get<LabelWidget>("EFFECTVSCONCRETE");

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));
			var requiresFormat = requiresLabel.Text;

			ActorInfo lastActor = null;
			Hotkey lastHotkey = Hotkey.Invalid;
			var lastPowerState = pm == null ? PowerState.Normal : pm.PowerState;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();
				if (tooltipIcon == null)
					return;

				var actor = tooltipIcon.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey != null ? tooltipIcon.Hotkey.GetValue() : Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey && (pm == null || pm.PowerState == lastPowerState))
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip != null ? tooltip.Name : actor.Name;
				var buildable = actor.TraitInfo<BuildableInfo>();

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				nameLabel.Text = name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				armorTypeLabel = GetArmorTypeLabel(armorTypeLabel, actor);
				var tooltipExtras = actor.TraitInfos<TooltipExtrasInfo>().FirstOrDefault(info => info.IsStandard);

				if (tooltipExtras != null)
				{
					strengthsLabel.Text = tooltipExtras.Strengths.Replace("\\n", "\n");
					weaknessesLabel.Text = tooltipExtras.Weaknesses.Replace("\\n", "\n");
					attributesLabel.Text = tooltipExtras.Attributes.Replace("\\n", "\n");

					versusLabel.Text = "";
					versusNoneLabel.Text = "";
					versusLightLabel.Text = "";
					versusHeavyLabel.Text = "";
					versusReflectorLabel.Text = "";
					versusWoodLabel.Text = "";
					versusConcreteLabel.Text = "";
					effectVersusNoneLabel.Text = "";
					effectVersusLightLabel.Text = "";
					effectVersusHeavyLabel.Text = "";
					effectVersusReflectorLabel.Text = "";
					effectVersusWoodLabel.Text = "";
					effectVersusConcreteLabel.Text = "";

					var armament = actor.TraitInfos<ArmamentInfo>().FirstOrDefault(a => a.Name == "primary");
					if (armament != null)
					{
						var weapon = armament.WeaponInfo;
						if (weapon != null)
						{
							var warheads = weapon.Warheads;
							if(warheads.Count > 0)
							{
								var dmgWarhead = warheads.OfType<DamageWarhead>().FirstOrDefault();
								if (dmgWarhead != null && dmgWarhead.Versus.Count() >= 6)
								{
									versusLabel.Text += "\\nEffective versus:";
									versusNoneLabel.Text += "Infantry: ";
									versusNoneLabel.TextColor = Color.LightSalmon;
									effectVersusNoneLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["None"], 70);
									effectVersusNoneLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["None"], 70);
									versusLightLabel.Text += "Light: ";
									versusLightLabel.TextColor = Color.Khaki;
									effectVersusLightLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Light"], 70);
									effectVersusLightLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Light"], 70);
									versusHeavyLabel.Text += "Heavy: ";
									versusHeavyLabel.TextColor = Color.Crimson;
									effectVersusHeavyLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Heavy"], 60);
									effectVersusHeavyLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Heavy"], 60);
									versusReflectorLabel.Text += "Reflector: ";
									versusReflectorLabel.TextColor = Color.SkyBlue;
									effectVersusReflectorLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Reflector"], 70);
									effectVersusReflectorLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Reflector"], 70);
									versusWoodLabel.Text += "Building: ";
									versusWoodLabel.TextColor = Color.IndianRed;
									effectVersusWoodLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Wood"], 60);
									effectVersusWoodLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Wood"], 60);
									versusConcreteLabel.Text += "Fortified: ";
									versusConcreteLabel.TextColor = Color.Gray;
									effectVersusConcreteLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Concrete"], 40);
									effectVersusConcreteLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Concrete"], 40);

									versusLabel.Text = versusLabel.Text.Replace("\\n", "\n");
								}
							}

						}
					}
				}
				else
				{
					strengthsLabel.Text = "";
					weaknessesLabel.Text = "";
					attributesLabel.Text = "";
					versusLabel.Text = "";
					versusNoneLabel.Text = "";
					versusLightLabel.Text = "";
					versusHeavyLabel.Text = "";
					versusReflectorLabel.Text = "";
					versusWoodLabel.Text = "";
					versusConcreteLabel.Text = "";
					effectVersusNoneLabel.Text = "";
					effectVersusLightLabel.Text = "";
					effectVersusHeavyLabel.Text = "";
					effectVersusReflectorLabel.Text = "";
					effectVersusWoodLabel.Text = "";
					effectVersusConcreteLabel.Text = "";
				}

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = "({0})".F(hotkey.DisplayString());

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.Text = hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith("~", StringComparison.Ordinal) && !s.StartsWith("!", StringComparison.Ordinal));

				var requiresSize = int2.Zero;
				if (prereqs.Any())
				{
					requiresLabel.Text = requiresFormat.F(prereqs.JoinWith(", "));
					requiresSize = requiresFont.Measure(requiresLabel.Text);
					requiresLabel.Visible = true;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height + (descLabel.Bounds.X / 2);
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var buildTime = tooltipIcon.ProductionQueue == null ? 0 : tooltipIcon.ProductionQueue.GetBuildTime(actor, buildable);
				var timeModifier = pm != null && pm.PowerState != PowerState.Normal ? tooltipIcon.ProductionQueue.Info.LowPowerModifier : 100;

				timeLabel.Text = formatBuildTime.Update((buildTime * timeModifier) / 100);
				timeLabel.TextColor = (pm != null && pm.PowerState != PowerState.Normal && tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100) ? Color.Red : Color.White;
				var timeSize = font.Measure(timeLabel.Text);

				costLabel.Text = cost.ToString();
				costLabel.GetColor = () => pr.Cash + pr.Resources >= cost ? Color.White : Color.Red;
				var costSize = font.Measure(costLabel.Text);

				var powerSize = new int2(0, 0);
				var power = 0;
				var armorTypeSize = armorTypeLabel.Text != "" ? font.Measure(armorTypeLabel.Text) : new int2(0, 0);
				armorTypeIcon.Visible = armorTypeSize.Y > 0;

				if (pm != null)
				{
					power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
					powerLabel.Text = power.ToString();
					powerLabel.GetColor = () => ((pm.PowerProvided - pm.PowerDrained) >= -power || power > 0)
						? Color.White : Color.Red;
					powerLabel.Visible = power != 0;
					powerIcon.Visible = power != 0;
					powerSize = font.Measure(powerLabel.Text);
				}

				if (armorTypeLabel.Text != "" && power != 0)
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = powerLabel.Bounds.Bottom;
				else
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = timeLabel.Bounds.Bottom;

				var extrasSpacing = descLabel.Bounds.X / 2;

				descLabel.Text = buildable.Description.Replace("\\n", "\n");
				var descSize = descFont.Measure(descLabel.Text);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y;

				var strengthsSize = strengthsLabel.Text != "" ? descFont.Measure(strengthsLabel.Text) : new int2(0, 0);
				var weaknessesSize = weaknessesLabel.Text != "" ? descFont.Measure(weaknessesLabel.Text) : new int2(0, 0);
				var attributesSize = attributesLabel.Text != "" ? descFont.Measure(attributesLabel.Text) : new int2(0, 0);

				strengthsLabel.Bounds.Y = descLabel.Bounds.Bottom + extrasSpacing;
				weaknessesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + extrasSpacing;
				attributesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + weaknessesSize.Y + extrasSpacing;
				var versusSize = versusLabel.Text != "" ? descFont.Measure(versusLabel.Text) : new int2(0, 0);
				var versusNoneSize = versusNoneLabel.Text != "" ? descFont.Measure(versusNoneLabel.Text) : new int2(0, 0);
				var versusLightSize = versusLightLabel.Text != "" ? descFont.Measure(versusLightLabel.Text) : new int2(0, 0);
				var versusHeavySize = versusHeavyLabel.Text != "" ? descFont.Measure(versusHeavyLabel.Text) : new int2(0, 0);
				var versusReflectorSize = versusReflectorLabel.Text != "" ? descFont.Measure(versusReflectorLabel.Text) : new int2(0, 0);
				var versusWoodSize = versusWoodLabel.Text != "" ? descFont.Measure(versusWoodLabel.Text) : new int2(0, 0);
				var versusConcreteSize = versusConcreteLabel.Text != "" ? descFont.Measure(versusConcreteLabel.Text) : new int2(0, 0);

				var textSpacing = descLabel.Bounds.Bottom + extrasSpacing;
				strengthsLabel.Bounds.Y = textSpacing;
				textSpacing += strengthsSize.Y;
				weaknessesLabel.Bounds.Y = textSpacing;
				textSpacing += weaknessesSize.Y;
				attributesLabel.Bounds.Y = textSpacing;
				textSpacing += attributesSize.Y;
				versusLabel.Bounds.Y = textSpacing;
				textSpacing += versusSize.Y;

				versusNoneLabel.Bounds.Y = textSpacing;
				effectVersusNoneLabel.Bounds.Y = textSpacing;
				textSpacing += versusNoneSize.Y;
				versusLightLabel.Bounds.Y = textSpacing;
				effectVersusLightLabel.Bounds.Y = textSpacing;
				textSpacing += versusLightSize.Y;
				versusHeavyLabel.Bounds.Y = textSpacing;
				effectVersusHeavyLabel.Bounds.Y = textSpacing;
				textSpacing += versusHeavySize.Y;
				versusReflectorLabel.Bounds.Y = textSpacing;
				effectVersusReflectorLabel.Bounds.Y = textSpacing;
				textSpacing += versusReflectorSize.Y;
				versusWoodLabel.Bounds.Y = textSpacing;
				effectVersusWoodLabel.Bounds.Y = textSpacing;
				textSpacing += versusWoodSize.Y;
				versusConcreteLabel.Bounds.Y = textSpacing;
				effectVersusConcreteLabel.Bounds.Y = textSpacing;
				textSpacing += versusConcreteSize.Y;

				descLabel.Bounds.Height += textSpacing + descLabelPadding;

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X, strengthsSize.X, weaknessesSize.X, attributesSize.X }.Aggregate(Math.Max);
				var rightWidth = new[] { powerSize.X, timeSize.X, costSize.X, armorTypeSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = armorTypeIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = armorTypeLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightHeight = armorTypeIcon.Bounds.Bottom + costIcon.Bounds.Top;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
				if (pm != null)
					lastPowerState = pm.PowerState;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return actorTooltip.Name;
			}

			return a;
		}

		Color GetEffectiveLabelColor(int effectValue, int neutralValue) {
			if (effectValue >= neutralValue * 2)
				return Color.Green;
			else if (effectValue >= neutralValue * 1.33)
				return Color.Aquamarine;
			else if (effectValue <= neutralValue * 0.5)
				return Color.Red;
			else if (effectValue <= neutralValue * 0.75)
				return Color.OrangeRed;
			else
				return Color.LightGray;
		}

		String GetEffectiveLabelText(int effectValue, int neutralValue) {
			if (effectValue >= neutralValue * 2)
				return "It's super effective!";
			else if (effectValue >= neutralValue * 1.33)
				return "effective";
			else if (effectValue <= neutralValue * 0.5)
				return "very low effectivenss.";
			else if (effectValue <= neutralValue * 0.75)
				return "not effective";
			else
				return "ok";
		}

		LabelWidget GetArmorTypeLabel(LabelWidget armorTypeLabel, ActorInfo actor)
		{
			var armor = actor.TraitInfos<ArmorInfo>().FirstOrDefault();
			armorTypeLabel.Text = armor != null ? armor.Type : "";

			// hard coded, specific to CA - find a better way to set user-friendly names and colors for armor types
			switch (armorTypeLabel.Text)
			{
				case "None":
					armorTypeLabel.Text = "Infantry";
					armorTypeLabel.TextColor = Color.LightSalmon;
					break;

				case "Light":
					armorTypeLabel.TextColor = Color.Khaki;
					break;

				case "Reflector":
					armorTypeLabel.TextColor = Color.SkyBlue;
					break;

				case "Heavy":
					armorTypeLabel.TextColor = Color.Crimson;
					break;

				case "Concrete":
					armorTypeLabel.Text = "Fortified";
					armorTypeLabel.TextColor = Color.Gray;
					break;

				case "Wood":
					armorTypeLabel.Text = "Building";
					armorTypeLabel.TextColor = Color.IndianRed;
					break;

				case "Brick":
					armorTypeLabel.Text = "Wall";
					armorTypeLabel.TextColor = Color.RosyBrown;
					break;

				default:
					armorTypeLabel.Text = "";
					break;
			}

			return armorTypeLabel;
		}
	}
}
