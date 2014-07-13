#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class MenuButtonWidget : ButtonWidget
	{
		public readonly string MenuContainer = "INGAME_MENU";
		public readonly bool Pause = true;
		public readonly bool HideIngameUI = true;

		[ObjectCreator.UseCtor]
		public MenuButtonWidget(Ruleset modRules)
			: base(modRules) { }

		protected MenuButtonWidget(MenuButtonWidget other)
			: base(other)
		{
			MenuContainer = other.MenuContainer;
			Pause = other.Pause;
			HideIngameUI = other.HideIngameUI;
		}
	}
}
