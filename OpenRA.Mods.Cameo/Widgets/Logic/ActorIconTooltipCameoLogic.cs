#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA;
using OpenRA.Mods.AS.Widgets;
using OpenRA.Mods.CA.Tooltips;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class ActorIconTooltipCameoLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ActorIconTooltipCameoLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<BasicUnit> getTooltipUnit)
		{
			widget.IsVisible = () => getTooltipUnit() != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var extrasLabel = widget.Get<LabelWidget>("EXTRAS");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var extrasFont = Game.Renderer.Fonts[extrasLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];

			BasicUnit lastUnit = null;
			var descLabelPadding = descLabel.Bounds.Height;
			var descLabelY = descLabel.Bounds.Y;

			tooltipContainer.BeforeRender = () =>
			{
				var unit = getTooltipUnit();
				if (unit == null || unit == lastUnit)
					return;

				var actor = unit.Actor;
				var world = actor?.World;
				var renderPlayer = world?.RenderPlayer ?? world?.LocalPlayer;
				var stance = renderPlayer == null ? PlayerRelationship.None : actor?.Owner.RelationshipWith(renderPlayer);
				var tooltip = unit.Tooltips?.FirstEnabledTraitOrDefault();
				var name = tooltip?.TooltipInfo.TooltipForPlayerStance(stance.Value) ?? actor?.Info.Name ?? unit.ActorInfo.Name;

				nameLabel.Text = name;
				var nameSize = font.Measure(name);

				var rules = world?.Map?.Rules ?? Game.ModData.DefaultRules;
				var resolvedActor = TooltipExtrasResolver.ResolveActorWithExtras(rules, unit.ActorInfo, requireStandard: false);
				var tooltipExtras = resolvedActor.TraitInfos<TooltipExtrasInfo>().ToArray();
				Log.Write("debug", $"Cameo actor extras actor={unit.ActorInfo.Name} resolved={resolvedActor.Name} count={tooltipExtras.Length}");
				var extrasText = TooltipExtrasFormatter.Format(tooltipExtras);
				extrasLabel.Text = extrasText;

				var extraOffset = 0;
				if (!string.IsNullOrEmpty(extrasText))
				{
					var extraSize = extrasFont.Measure(extrasText);
					extrasLabel.Visible = true;
					extrasLabel.Bounds.Height = extraSize.Y;
					extraOffset = extraSize.Y;
				}
				else
				{
					extrasLabel.Visible = false;
					extrasLabel.Bounds.Height = 0;
				}

				descLabel.Bounds.Y = descLabelY + extraOffset;

				var descSize = int2.Zero;
				if (unit.TooltipDescriptions != null && unit.TooltipDescriptions.Any())
				{
					var descText = string.Join("\n",
						unit.TooltipDescriptions
							.Where(td => td.IsTooltipVisible(renderPlayer))
							.Select(td => FluentProvider.GetMessage(td.TooltipText)));

					descLabel.Text = descText;
					descSize = descFont.Measure(descText);
					descLabel.Bounds.Width = descSize.X;
					descLabel.Bounds.Height = descSize.Y + descLabelPadding;
				}
				else if (unit.BuildableInfo != null && !string.IsNullOrEmpty(unit.BuildableInfo.Description))
				{
					var descText = FluentProvider.GetMessage(unit.BuildableInfo.Description);
					descLabel.Text = descText;
					descSize = descFont.Measure(descText);
					descLabel.Bounds.Width = descSize.X;
					descLabel.Bounds.Height = descSize.Y + descLabelPadding;
				}
				else
				{
					descLabel.Text = string.Empty;
					descLabel.Bounds.Height = 0;
				}

				var leftWidth = Math.Max(nameSize.X, descSize.X);
				widget.Bounds.Width = leftWidth + 2 * nameLabel.Bounds.X;

				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;
				widget.Bounds.Height = leftHeight;

				lastUnit = unit;
			};
		}
	}
}
