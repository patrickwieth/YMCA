#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.CA.Widgets.Logic
{
	public class TemplateMenuLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public TemplateMenuLogic(Widget widget, World world, ModData modData)
		{
			widget.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;
		}
	}
}
