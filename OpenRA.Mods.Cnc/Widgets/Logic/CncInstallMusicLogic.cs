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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncInstallMusicLogic
	{
		[ObjectCreator.UseCtor]
		public CncInstallMusicLogic(Widget widget, Ruleset modRules, Action onExit)
		{
			var installButton = widget.GetOrNull<ButtonWidget>("INSTALL_BUTTON");
			if (installButton != null)
			{
				Action afterInstall = () =>
				{
					try
					{
						var path = new string[] { Platform.SupportDir, "Content", Game.modData.Manifest.Mod.Id }.Aggregate(Path.Combine);
						GlobalFileSystem.Mount(Path.Combine(path, "scores.mix"));
						GlobalFileSystem.Mount(Path.Combine(path, "transit.mix"));

						modRules.Music.Do(m => m.Value.Reload());

						var musicPlayerLogic = (MusicPlayerLogic)installButton.Parent.LogicObject;
						musicPlayerLogic.BuildMusicTable();
					}
					catch (Exception e)
					{
						Log.Write("debug", "Mounting the new mixfile and rebuild of scores list failed:\n{0}", e);
					}
				};

				installButton.OnClick = () =>
					Ui.OpenWindow("INSTALL_MUSIC_PANEL", new WidgetArgs() {
						{ "afterInstall", afterInstall },
						{ "filesToCopy", new[] { "SCORES.MIX" } },
						{ "filesToExtract", new[] { "transit.mix" } },
					});
				installButton.IsVisible = () => modRules.InstalledMusic.ToArray().Length < 3; // HACK around music being split between transit.mix and scores.mix
			}
		}
	}
}
