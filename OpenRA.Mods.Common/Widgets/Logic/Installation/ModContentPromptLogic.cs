#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentPromptLogic : ChromeLogic
	{
		[FluentReference]
		const string Continue = "button-continue";

		[FluentReference]
		const string Quit = "button-quit";

		readonly ModContent content;
		readonly FluentBundle externalFluentBundle;
		bool requiredContentInstalled;

		[ObjectCreator.UseCtor]
		public ModContentPromptLogic(ModData modData, Widget widget, Manifest mod, ModContent content, Action continueLoading, string translationFilePath)
		{
			this.content = content;
			CheckRequiredContentInstalled();

			externalFluentBundle = new FluentBundle(Game.Settings.Player.Language, File.ReadAllText(translationFilePath), _ => { });

			var continueMessage = FluentProvider.GetString(Continue);
			var quitMessage = FluentProvider.GetString(Quit);

			var panel = widget.Get("CONTENT_PROMPT_PANEL");
			var headerTemplate = panel.Get<LabelWidget>("HEADER_TEMPLATE");
			var headerLines =
				!string.IsNullOrEmpty(content.InstallPromptMessage)
					? externalFluentBundle.GetString(content.InstallPromptMessage)
					: null;
			var headerHeight = 0;
			if (headerLines != null)
			{
				var label = (LabelWidget)headerTemplate.Clone();
				label.GetText = () => headerLines;
				label.IncreaseHeightToFitCurrentText();
				panel.AddChild(label);

				headerHeight += label.Bounds.Height;
			}

			panel.Bounds.Height += headerHeight;
			panel.Bounds.Y -= headerHeight / 2;

			var advancedButton = panel.Get<ButtonWidget>("ADVANCED_BUTTON");
			advancedButton.Bounds.Y += headerHeight;
			advancedButton.OnClick = () =>
			{
				Ui.OpenWindow("CONTENT_PANEL", new WidgetArgs
				{
					{ "onCancel", CheckRequiredContentInstalled },
					{ "mod", mod },
					{ "content", content },
					{ "translationFilePath", translationFilePath },
				});
			};

			var quickButton = panel.Get<ButtonWidget>("QUICK_BUTTON");
			quickButton.IsVisible = () => !string.IsNullOrEmpty(content.QuickDownload);
			quickButton.Bounds.Y += headerHeight;
			quickButton.OnClick = () =>
			{
				var modObjectCreator = new ObjectCreator(mod, Game.Mods);
				var modPackageLoaders = modObjectCreator.GetLoaders<IPackageLoader>(mod.PackageFormats, "package");
				var modFileSystem = new FS(mod.Id, Game.Mods, modPackageLoaders);

				var modFileSystemLoader = modObjectCreator.GetLoader<IFileSystemLoader>(mod.FileSystem.Value, "filesystem");
				FieldLoader.Load(modFileSystemLoader, mod.FileSystem);
				modFileSystemLoader.Mount(modFileSystem, modObjectCreator);
				modFileSystem.TrimExcess();

				var downloadYaml = MiniYaml.Load(modFileSystem, content.Downloads, null);
				modFileSystem.UnmountAll();

				var download = downloadYaml.FirstOrDefault(n => n.Key == content.QuickDownload);
				if (download == null)
					throw new InvalidOperationException($"Mod QuickDownload `{content.QuickDownload}` definition not found.");

				Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", new WidgetArgs
				{
					{ "download", new ModContent.ModDownload(download.Value, modObjectCreator) },
					{ "onSuccess", continueLoading }
				});
			};

			var quitButton = panel.Get<ButtonWidget>("QUIT_BUTTON");
			quitButton.GetText = () => requiredContentInstalled ? continueMessage : quitMessage;
			quitButton.Bounds.Y += headerHeight;
			quitButton.OnClick = () =>
			{
				if (requiredContentInstalled)
					continueLoading();
				else
					Game.Exit();
			};

			Game.RunAfterTick(Ui.ResetTooltips);
		}

		void CheckRequiredContentInstalled()
		{
			requiredContentInstalled = content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));
		}
	}
}
