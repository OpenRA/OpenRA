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
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncInstallMusicLogic
	{
		[ObjectCreator.UseCtor]
		public CncInstallMusicLogic(Widget widget, Action onExit)
		{
			var installButton = widget.GetOrNull<ButtonWidget>("INSTALL_BUTTON");
			if (installButton != null)
			{
				Action afterInstall = () =>
				{
					try
					{
						var path = new string[] { Platform.SupportDir, "Content", WidgetUtils.ActiveModId() }.Aggregate(Path.Combine);
						FileSystem.Mount(Path.Combine(path, "scores.mix"));
						FileSystem.Mount(Path.Combine(path, "transit.mix"));

						Rules.Music.Do(m => m.Value.Reload());

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
				installButton.IsVisible = () => Rules.InstalledMusic.ToArray().Length < 3; // HACK around music being split between transit.mix and scores.mix
			}
		}
	}
}
