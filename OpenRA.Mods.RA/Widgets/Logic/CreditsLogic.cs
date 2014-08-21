#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#endregion

using System;
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class CreditsLogic
	{
		[ObjectCreator.UseCtor]
		public CreditsLogic(Widget widget,  Action onExit)
		{
			var panel = widget.Get("CREDITS_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			var scrollPanel = panel.Get<ScrollPanelWidget>("CREDITS_DISPLAY");
			var template = scrollPanel.Get<LabelWidget>("CREDITS_TEMPLATE");
			scrollPanel.RemoveChildren();

			var lines = GlobalFileSystem.Open("AUTHORS").ReadAllLines();
			foreach (var l in lines)
			{
				// Improve the formatting
				var line = l.Replace("\t", "    ").Replace("*", "\u2022");
				var label = template.Clone() as LabelWidget;
				label.GetText = () => line;
				scrollPanel.AddChild(label);
			}
		}
	}
}
