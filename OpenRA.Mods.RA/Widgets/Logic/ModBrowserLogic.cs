#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ModBrowserLogic
	{
		Mod currentMod;

		[ObjectCreator.UseCtor]
		public ModBrowserLogic(Widget widget, Action onSwitch, Action onExit)
		{
			var panel = widget;
			var modList = panel.Get<ScrollPanelWidget>("MOD_LIST");
			var loadButton = panel.Get<ButtonWidget>("LOAD_BUTTON");
			loadButton.OnClick = () => LoadMod(currentMod.Id, onSwitch);
			loadButton.IsDisabled = () => currentMod.Id == Game.CurrentMods.Keys.First();

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
			currentMod = Mod.AllMods[Game.modData.Manifest.Mods[0]];

			// Mod list
			var modTemplate = modList.Get<ScrollItemWidget>("MOD_TEMPLATE");

			foreach (var m in Mod.AllMods)
			{
				var mod = m.Value;
				var item = ScrollItemWidget.Setup(modTemplate, () => currentMod == mod, () => currentMod = mod);
				item.Get<LabelWidget>("TITLE").GetText = () => mod.Title;
				item.Get<LabelWidget>("VERSION").GetText = () => mod.Version;
				item.Get<LabelWidget>("AUTHOR").GetText = () => mod.Author;
				modList.AddChild(item);
			}
		}

		void LoadMod(string mod, Action onSwitch)
		{
			var mods = Mod.AllMods[mod].WithPrerequisites();

			Game.RunAfterTick(() =>
			{
				Ui.CloseWindow();
				onSwitch();
				Game.InitializeWithMods(mods);
			});
		}
	}
}
