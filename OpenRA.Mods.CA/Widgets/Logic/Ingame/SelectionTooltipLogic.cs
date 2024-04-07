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
	public class SelectionTooltipLogic : ChromeLogic
	{
		readonly World world;
		int selectionHash;
		readonly Widget widget;
		int originalDescLabelHeight;
		int iconMargin;

		[ObjectCreator.UseCtor]
		public SelectionTooltipLogic(Widget widget, World world)
		{
			this.world = world;
			this.widget = widget;
			widget.IsVisible = () => Game.Settings.Game.SelectionTooltip && !Game.Settings.Debug.PerfText;
			originalDescLabelHeight = -1;
			iconMargin = -1;
		}

		public override void Tick()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			UpdateTooltip();
			selectionHash = world.Selection.Hash;
		}

		void HideTooltip()
		{
			widget.Bounds.X = Game.Renderer.Resolution.Width;
			widget.Bounds.Y = Game.Renderer.Resolution.Height;
		}

		void UpdateTooltip()
		{
			if (world.Selection.Actors.Count() != 1)
			{
				HideTooltip();
				return;
			}

			var actor = world.Selection.Actors.First();
			if (actor == null || actor.Info == null || actor.IsDead || !actor.IsInWorld || actor.Disposed)
			{
				HideTooltip();
				return;
			}

			var mapRules = world.Map.Rules;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var armorTypeLabel = widget.Get<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.Get<ImageWidget>("ARMORTYPE_ICON");
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

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));

			if (originalDescLabelHeight == -1)
				originalDescLabelHeight = descLabel.Bounds.Height;

			if (iconMargin == -1)
				iconMargin = armorTypeIcon.Bounds.X;

			descLabel.Bounds.Height = originalDescLabelHeight;

			var descLabelPadding = descLabel.Bounds.Height;

			var tooltip = actor.TraitsImplementing<Tooltip>().FirstOrDefault(Exts.IsTraitEnabled);
			var name = tooltip != null ? tooltip.Info.Name : actor.Info.Name;

			nameLabel.Text = name;

			var nameSize = font.Measure(name);

			armorTypeLabel = GetArmorTypeLabel(armorTypeLabel, actor.Info);
			var tooltipExtras = actor.TraitsImplementing<TooltipExtras>().FirstOrDefault(Exts.IsTraitEnabled);

			if (tooltipExtras != null)
			{
				var tooltipExtrasInfo = tooltipExtras.Info;
				strengthsLabel.Text = tooltipExtrasInfo.Strengths.Replace("\\n", "\n");
				weaknessesLabel.Text = tooltipExtrasInfo.Weaknesses.Replace("\\n", "\n");
				attributesLabel.Text = tooltipExtrasInfo.Attributes.Replace("\\n", "\n");
				descLabel.Text = tooltipExtrasInfo.Description.Replace("\\n", "\n");
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

				var armament = actor.TraitsImplementing<Armament>().FirstOrDefault(a => a.Info.Name == "primary");
				if (armament != null)
				{
					var weapon = armament.Weapon;
					if (weapon != null)
					{
						var dmgWarhead = weapon.Warheads.OfType<DamageWarhead>().FirstOrDefault();
						if (dmgWarhead != null)
						{
							var fireRate = (weapon.Burst > 0 ? weapon.Burst : 1)*25.0 / weapon.ReloadDelay;
							var damage = dmgWarhead.Damage / 100;
							versusLabel.Text += "\\nDamage per Second: "+(int)(damage*fireRate)+"\\n\\nEffective versus:";
							if (dmgWarhead.Versus.ContainsKey("None")) {
								versusNoneLabel.Text += "Infantry: ";
								versusNoneLabel.TextColor = Color.LightSalmon;
								effectVersusNoneLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["None"], 70);
								effectVersusNoneLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["None"], 70);
							}
							if (dmgWarhead.Versus.ContainsKey("Light")) {
								versusLightLabel.Text += "Light: ";
								versusLightLabel.TextColor = Color.Khaki;
								effectVersusLightLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Light"], 70);
								effectVersusLightLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Light"], 70);
							}
							if (dmgWarhead.Versus.ContainsKey("Heavy")) {
								versusHeavyLabel.Text += "Heavy: ";
								versusHeavyLabel.TextColor = Color.Crimson;
								effectVersusHeavyLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Heavy"], 60);
								effectVersusHeavyLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Heavy"], 60);
							}
							if ( dmgWarhead.Versus.ContainsKey("Reflector")) {
								versusReflectorLabel.Text += "Reflector: ";
								versusReflectorLabel.TextColor = Color.SkyBlue;
								effectVersusReflectorLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Reflector"], 70);
								effectVersusReflectorLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Reflector"], 70);
							}
							if (dmgWarhead.Versus.ContainsKey("Wood")) {
								versusWoodLabel.Text += "Building: ";
								versusWoodLabel.TextColor = Color.IndianRed;
								effectVersusWoodLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Wood"], 60);
								effectVersusWoodLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Wood"], 60);
							}
							if (dmgWarhead.Versus.ContainsKey("Concrete")) {
								versusConcreteLabel.Text += "Fortified: ";
								versusConcreteLabel.TextColor = Color.Gray;
								effectVersusConcreteLabel.Text += GetEffectiveLabelText(dmgWarhead.Versus["Concrete"], 40);
								effectVersusConcreteLabel.TextColor = GetEffectiveLabelColor(dmgWarhead.Versus["Concrete"], 40);
							}
							versusLabel.Text = versusLabel.Text.Replace("\\n", "\n");
						}
					}
				}
			}
			else
			{
				strengthsLabel.Text = "";
				weaknessesLabel.Text = "";
				attributesLabel.Text = "";
				descLabel.Text = "";
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

			var armorTypeSize = armorTypeLabel.Text != "" ? font.Measure(armorTypeLabel.Text) : new int2(0, 0);
			armorTypeIcon.Visible = armorTypeSize.Y > 0;
			armorTypeLabel.Bounds.Y = armorTypeIcon.Bounds.Y;

			var extrasSpacing = descLabel.Bounds.X / 2;

			if (descLabel.Text == "")
			{
				var buildable = actor.Info.TraitInfoOrDefault<BuildableInfo>();

				if (buildable != null)
				{
					descLabel.Text = buildable.Description.Replace("\\n", "\n");
				}
			}

			var descSize = descLabel.Text != "" ? descFont.Measure(descLabel.Text) : new int2(0, 0);

			descLabel.Bounds.Width = descSize.X;
			descLabel.Bounds.Height = descSize.Y;

			var strengthsSize = strengthsLabel.Text != "" ? descFont.Measure(strengthsLabel.Text) : new int2(0, 0);
			var weaknessesSize = weaknessesLabel.Text != "" ? descFont.Measure(weaknessesLabel.Text) : new int2(0, 0);
			var attributesSize = attributesLabel.Text != "" ? descFont.Measure(attributesLabel.Text) : new int2(0, 0);
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

			var leftWidth = new[] { nameSize.X, descSize.X, strengthsSize.X, weaknessesSize.X, attributesSize.X, versusSize.X }.Aggregate(Math.Max);
			var rightWidth = new[] { armorTypeSize.X }.Aggregate(Math.Max);

			armorTypeIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
			armorTypeLabel.Bounds.X = armorTypeIcon.Bounds.Right + iconMargin;
			widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + armorTypeIcon.Bounds.Width + iconMargin;

			// Set the bottom margin to match the left margin
			var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

			// Set the bottom margin to match the top margin
			var rightHeight = armorTypeIcon.Bounds.Bottom;
			widget.Bounds.Height = Math.Max(leftHeight, rightHeight);
			widget.Bounds.X = Game.Renderer.Resolution.Width - widget.Bounds.Width - 12;
			widget.Bounds.Y = Game.Renderer.Resolution.Height - widget.Bounds.Height - 12;
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
			return $"{effectValue}%";
			/* // In the past there was text instead of just raw %
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
			*/
		}

		LabelWidget GetArmorTypeLabel(LabelWidget armorTypeLabel, ActorInfo actor)
		{
			var armor = actor.TraitInfos<ArmorInfo>().FirstOrDefault();
			armorTypeLabel.Text = armor != null ? armor.Type : "";

			// We also display Health in addition
			var health = actor.TraitInfos<HealthInfo>().FirstOrDefault();
			var healthText = health != null ? " - HP: "+health.HP/100 : "";

			switch (armorTypeLabel.Text)
			{
				case "None":
					armorTypeLabel.Text = "Infantry"+healthText;
					armorTypeLabel.TextColor = Color.LightSalmon;
					break;

				case "Light":
					armorTypeLabel.Text += healthText;
					armorTypeLabel.TextColor = Color.Khaki;
					break;

				case "Reflector":
					armorTypeLabel.Text += healthText;
					armorTypeLabel.TextColor = Color.SkyBlue;
					break;

				case "Heavy":
					armorTypeLabel.Text += healthText;
					armorTypeLabel.TextColor = Color.Crimson;
					break;

				case "Concrete":
					armorTypeLabel.Text = "Fortified"+healthText;
					armorTypeLabel.TextColor = Color.Gray;
					break;

				case "Wood":
					armorTypeLabel.Text = "Building"+healthText;
					armorTypeLabel.TextColor = Color.IndianRed;
					break;

				case "Brick":
					armorTypeLabel.Text = "Wall"+healthText;
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
