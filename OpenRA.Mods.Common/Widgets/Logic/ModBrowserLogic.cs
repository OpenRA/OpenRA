#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModBrowserLogic : ChromeLogic
	{
		readonly Widget modList;
		readonly ButtonWidget modTemplate;
		readonly ModMetadata[] allMods;
		readonly Dictionary<string, Sprite> previews = new Dictionary<string, Sprite>();
		readonly Dictionary<string, Sprite> logos = new Dictionary<string, Sprite>();
		readonly Cache<ModMetadata, bool> modInstallStatus;
		readonly Cache<string, bool> modPrerequisitesFulfilled;
		readonly Widget modChooserPanel;
		readonly ButtonWidget loadButton;
		readonly SheetBuilder sheetBuilder;
		ModMetadata selectedMod;
		string selectedAuthor;
		string selectedDescription;
		int modOffset = 0;

		[ObjectCreator.UseCtor]
		public ModBrowserLogic(Widget widget)
		{
			modInstallStatus = new Cache<ModMetadata, bool>(IsModInstalled);
			modPrerequisitesFulfilled = new Cache<string, bool>(Game.IsModInstalled);

			modChooserPanel = widget;
			loadButton = modChooserPanel.Get<ButtonWidget>("LOAD_BUTTON");
			loadButton.OnClick = () => LoadMod(selectedMod);
			loadButton.IsDisabled = () => selectedMod.Id == Game.ModData.Manifest.Mod.Id;

			modChooserPanel.Get<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			modList = modChooserPanel.Get("MOD_LIST");
			modTemplate = modList.Get<ButtonWidget>("MOD_TEMPLATE");

			modChooserPanel.Get<LabelWidget>("MOD_DESC").GetText = () => selectedDescription;
			modChooserPanel.Get<LabelWidget>("MOD_TITLE").GetText = () => selectedMod.Title;
			modChooserPanel.Get<LabelWidget>("MOD_AUTHOR").GetText = () => selectedAuthor;
			modChooserPanel.Get<LabelWidget>("MOD_VERSION").GetText = () => selectedMod.Version;

			var prevMod = modChooserPanel.Get<ButtonWidget>("PREV_MOD");
			prevMod.OnClick = () => { modOffset -= 1; RebuildModList(); };
			prevMod.IsVisible = () => modOffset > 0;

			var nextMod = modChooserPanel.Get<ButtonWidget>("NEXT_MOD");
			nextMod.OnClick = () => { modOffset += 1; RebuildModList(); };
			nextMod.IsVisible = () => modOffset + 5 < allMods.Length;

			modChooserPanel.Get<RGBASpriteWidget>("MOD_PREVIEW").GetSprite = () =>
			{
				Sprite ret = null;
				previews.TryGetValue(selectedMod.Id, out ret);
				return ret;
			};

			sheetBuilder = new SheetBuilder(SheetType.BGRA);
			allMods = ModMetadata.AllMods.Values.Where(m => !m.Hidden)
				.OrderBy(m => m.Title)
				.ToArray();

			// Load preview images, and eat any errors
			foreach (var mod in allMods)
			{
				try
				{
					using (var preview = new Bitmap(Platform.ResolvePath(".", "mods", mod.Id, "preview.png")))
						if (preview.Width == 296 && preview.Height == 196)
							previews.Add(mod.Id, sheetBuilder.Add(preview));
				}
				catch (Exception) { }

				try
				{
					using (var logo = new Bitmap(Platform.ResolvePath(".", "mods", mod.Id, "logo.png")))
						if (logo.Width == 96 && logo.Height == 96)
							logos.Add(mod.Id, sheetBuilder.Add(logo));
				}
				catch (Exception) { }
			}

			ModMetadata initialMod;
			ModMetadata.AllMods.TryGetValue(Game.Settings.Game.PreviousMod, out initialMod);
			SelectMod(initialMod != null && initialMod.Id != "modchooser" ? initialMod : ModMetadata.AllMods["ra"]);

			RebuildModList();
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

			loadButton.Text = !modPrerequisitesFulfilled[mod.Id] ? "Install mod" :
				modInstallStatus[mod] ? "Load Mod" : "Install Assets";
		}

		void LoadMod(ModMetadata mod)
		{
			if (!modPrerequisitesFulfilled[mod.Id])
			{
				var widgetArgs = new WidgetArgs
				{
					{ "modId", mod.Id }
				};

				Ui.OpenWindow("INSTALL_MOD_PANEL", widgetArgs);
				return;
			}

			if (!modInstallStatus[mod])
			{
				var widgetArgs = new WidgetArgs
				{
					{ "continueLoading", () =>
						Game.RunAfterTick(() => Game.InitializeMod(Game.Settings.Game.Mod, new Arguments())) },
					{ "mirrorListUrl", mod.Content.PackageMirrorList },
					{ "modId", mod.Id }
				};

				Ui.OpenWindow("INSTALL_PANEL", widgetArgs);

				return;
			}

			Game.RunAfterTick(() =>
			{
				Ui.CloseWindow();
				sheetBuilder.Dispose();
				Game.InitializeMod(mod.Id, null);
			});
		}

		static bool IsModInstalled(ModMetadata mod)
		{
			return mod.Content.TestFiles.All(file => File.Exists(Path.GetFullPath(Platform.ResolvePath(file))));
		}
	}
}
