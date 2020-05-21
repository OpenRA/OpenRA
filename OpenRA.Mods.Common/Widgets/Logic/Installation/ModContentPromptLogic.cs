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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentPromptLogic : ChromeLogic
	{
		readonly ModContent content;
		bool requiredContentInstalled;

		[ObjectCreator.UseCtor]
		public ModContentPromptLogic(Widget widget, ModData modData, Manifest mod, ModContent content, Action continueLoading)
		{
			this.content = content;
			CheckRequiredContentInstalled();

			var panel = widget.Get("CONTENT_PROMPT_PANEL");
			var headerTemplate = panel.Get<LabelWidget>("HEADER_TEMPLATE");
			var headerLines = !string.IsNullOrEmpty(content.InstallPromptMessage) ? content.InstallPromptMessage.Replace("\\n", "\n").Split('\n') : new string[0];
			var headerHeight = 0;
			foreach (var l in headerLines)
			{
				var line = (LabelWidget)headerTemplate.Clone();
				line.GetText = () => l;
				line.Node.Top = (int)line.Node.LayoutY + headerHeight;
				line.Node.CalculateLayout();
				panel.AddChild(line);

				headerHeight += (int)headerTemplate.Node.LayoutHeight;
			}

			panel.Node.Height = (int)panel.Node.LayoutHeight + headerHeight;
			panel.Node.Top = (int)panel.Node.LayoutY - headerHeight / 2;
			panel.Node.CalculateLayout();

			var advancedButton = panel.Get<ButtonWidget>("ADVANCED_BUTTON");
			advancedButton.Node.Top = (int)advancedButton.Node.LayoutY + headerHeight;
			advancedButton.Node.CalculateLayout();
			advancedButton.OnClick = () =>
			{
				Ui.OpenWindow("CONTENT_PANEL", new WidgetArgs
				{
					{ "mod", mod },
					{ "content", content },
					{ "onCancel", CheckRequiredContentInstalled }
				});
			};

			var quickButton = panel.Get<ButtonWidget>("QUICK_BUTTON");
			quickButton.VisibilityFunction = () => !string.IsNullOrEmpty(content.QuickDownload);
			quickButton.Node.Top = (int)quickButton.Node.LayoutY + headerHeight;
			quickButton.Node.CalculateLayout();
			quickButton.OnClick = () =>
			{
				var modObjectCreator = new ObjectCreator(mod, Game.Mods);
				var modPackageLoaders = modObjectCreator.GetLoaders<IPackageLoader>(mod.PackageFormats, "package");
				var modFileSystem = new FS(mod.Id, Game.Mods, modPackageLoaders);
				modFileSystem.LoadFromManifest(mod);

				var downloadYaml = MiniYaml.Load(modFileSystem, content.Downloads, null);
				modFileSystem.UnmountAll();

				var download = downloadYaml.FirstOrDefault(n => n.Key == content.QuickDownload);
				if (download == null)
					throw new InvalidOperationException("Mod QuickDownload `{0}` definition not found.".F(content.QuickDownload));

				Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", new WidgetArgs
				{
					{ "download", new ModContent.ModDownload(download.Value) },
					{ "onSuccess", continueLoading }
				});
			};

			var quitButton = panel.Get<ButtonWidget>("QUIT_BUTTON");
			quitButton.GetText = () => requiredContentInstalled ? "Continue" : "Quit";
			quitButton.Node.Top = (int)quitButton.Node.LayoutY + headerHeight;
			quitButton.Node.CalculateLayout();
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
