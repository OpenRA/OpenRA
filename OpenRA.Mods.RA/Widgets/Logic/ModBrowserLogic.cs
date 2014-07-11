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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ModBrowserLogic
	{
		readonly Widget modList;
		readonly ButtonWidget modTemplate;
		readonly ModMetadata[] allMods;
		readonly Dictionary<string, Sprite> previews = new Dictionary<string, Sprite>();
		readonly Dictionary<string, Sprite> logos = new Dictionary<string, Sprite>();
		ModMetadata selectedMod;
		string selectedAuthor;
		string selectedDescription;
		int modOffset = 0;

		[ObjectCreator.UseCtor]
		public ModBrowserLogic(Widget widget)
		{
			var panel = widget;
			var loadButton = panel.Get<ButtonWidget>("LOAD_BUTTON");
			loadButton.OnClick = () => LoadMod(selectedMod);
			loadButton.IsDisabled = () => selectedMod.Id == Game.modData.Manifest.Mod.Id;

			panel.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			modList = panel.Get("MOD_LIST");
			modTemplate = modList.Get<ButtonWidget>("MOD_TEMPLATE");

			panel.Get<LabelWidget>("MOD_DESC").GetText = () => selectedDescription;
			panel.Get<LabelWidget>("MOD_TITLE").GetText = () => selectedMod.Title;
			panel.Get<LabelWidget>("MOD_AUTHOR").GetText = () => selectedAuthor;
			panel.Get<LabelWidget>("MOD_VERSION").GetText = () => selectedMod.Version;

			var prevMod = panel.Get<ButtonWidget>("PREV_MOD");
			prevMod.OnClick = () => { modOffset -= 1; RebuildModList(); };
			prevMod.IsVisible = () => modOffset > 0;

			var nextMod = panel.Get<ButtonWidget>("NEXT_MOD");
			nextMod.OnClick = () => { modOffset += 1; RebuildModList(); };
			nextMod.IsVisible = () => modOffset + 5 < allMods.Length;

			panel.Get<RGBASpriteWidget>("MOD_PREVIEW").GetSprite = () =>
			{
				Sprite ret = null;
				previews.TryGetValue(selectedMod.Id, out ret);
				return ret;
			};

			var sheetBuilder = new SheetBuilder(SheetType.BGRA);
			allMods = ModMetadata.AllMods.Values.Where(m => m.Id != "modchooser")
				.OrderBy(m => m.Title)
				.ToArray();

			// Load preview images, and eat any errors
			foreach (var mod in allMods)
			{
				try
				{
					using (var preview = new Bitmap(new[] { "mods", mod.Id, "preview.png" }.Aggregate(Path.Combine)))
						if (preview.Width == 296 && preview.Height == 196)
							previews.Add(mod.Id, sheetBuilder.Add(preview));
				}
				catch (Exception) { }

				try
				{
					using (var logo = new Bitmap(new[] { "mods", mod.Id, "logo.png" }.Aggregate(Path.Combine)))
						if (logo.Width == 96 && logo.Height == 96)
							logos.Add(mod.Id, sheetBuilder.Add(logo));
				}
				catch (Exception) { }
			}

			ModMetadata initialMod = null;
			ModMetadata.AllMods.TryGetValue(Game.Settings.Game.PreviousMod, out initialMod);
			SelectMod(initialMod ?? ModMetadata.AllMods["ra"]);

			RebuildModList();
		}

		static void LoadMod(ModMetadata mod)
		{
			Game.RunAfterTick(() =>
			{
				Ui.CloseWindow();
				Game.InitializeMod(mod.Id, null);
			});
		}

		void RebuildModList()
		{
			modList.RemoveChildren();

			var width = modTemplate.Bounds.Width;
			var height = modTemplate.Bounds.Height;
			var innerMargin = modTemplate.Bounds.Left;
			var outerMargin = (modList.Bounds.Width - Math.Min(5, allMods.Length) * width - 4 * innerMargin) / 2;
			var stride = width + innerMargin;

			for (var i = 0; i < 5; i++)
			{
				var j = i + modOffset;
				if (j >= allMods.Length)
					break;

				var mod = allMods[j];
				var item = modTemplate.Clone() as ButtonWidget;
				item.Bounds = new Rectangle(outerMargin + i * stride, 0, width, height);
				item.IsHighlighted = () => selectedMod == mod;
				item.OnClick = () => SelectMod(mod);
				item.OnDoubleClick = () => LoadMod(mod);
				item.OnKeyPress = e =>
				{
					if (e.MultiTapCount == 2)
						LoadMod(mod);
					else
						SelectMod(mod);
				};

				item.TooltipText = mod.Title;

				if (j < 9)
					item.Key = new Hotkey((Keycode)((int)Keycode.NUMBER_1 + j), Modifiers.None);

				Sprite logo = null;
				logos.TryGetValue(mod.Id, out logo);
				item.Get<RGBASpriteWidget>("MOD_LOGO").GetSprite = () => logo;
				item.Get("MOD_NO_LOGO").IsVisible = () => logo == null;

				modList.AddChild(item);
			}
		}

		void SelectMod(ModMetadata mod)
		{
			selectedMod = mod;
			selectedAuthor = "By " + (mod.Author ?? "unknown author");
			selectedDescription = (mod.Description ?? "").Replace("\\n", "\n");
			var selectedIndex = Array.IndexOf(allMods, mod);
			if (selectedIndex - modOffset > 4)
				modOffset = selectedIndex - 4;
		}
	}
}
