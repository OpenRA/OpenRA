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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc
{
	public class ModChooserLoadScreen : ILoadScreen
	{
		Sprite sprite;
		Rectangle bounds;

		public void Init(Manifest m, Dictionary<string, string> info)
		{
			var sheet = new Sheet("mods/modchooser/chrome.png");
			var res = Game.Renderer.Resolution;
			bounds = new Rectangle(0, 0, res.Width, res.Height);
			sprite = new Sprite(sheet, new Rectangle(0,0,1024,480), TextureChannel.Alpha);
		}

		public void Display()
		{
			var r = Game.Renderer;
			if (r == null)
				return;

			r.BeginFrame(float2.Zero, 1f);
			WidgetUtils.FillRectWithSprite(bounds, sprite);
			r.EndFrame(new NullInputHandler());
		}

		public void StartGame()
		{
			Ui.LoadWidget("MODCHOOSER", Ui.Root, new WidgetArgs());
		}
	}
}