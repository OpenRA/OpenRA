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
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class RootWidget : ContainerWidget
	{
		public RootWidget()
		{
			IgnoreMouseOver = true;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var hk = Hotkey.FromKeyInput(e);

				if (hk == Game.Settings.Keys.DevReloadChromeKey)
				{
					ChromeProvider.Initialize();
					return true;
				}
			}

			return base.HandleKeyPress(e);
		}
	}
}
