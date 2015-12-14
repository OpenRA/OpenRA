#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class ModChooserLoadScreen : ILoadScreen
	{
		Sprite sprite;
		Rectangle bounds;

		public void Init(Manifest m, Dictionary<string, string> info)
		{
			var res = Game.Renderer.Resolution;
			bounds = new Rectangle(0, 0, res.Width, res.Height);

			using (var stream = File.OpenRead(info["Image"]))
			{
				var sheet = new Sheet(SheetType.BGRA, stream);
				sprite = new Sprite(sheet, new Rectangle(0, 0, 1024, 480), TextureChannel.Alpha);
			}
		}

		public void Display()
		{
			var r = Game.Renderer;
			if (r == null)
				return;

			r.BeginFrame(int2.Zero, 1f);
			WidgetUtils.FillRectWithSprite(bounds, sprite);
			r.EndFrame(new NullInputHandler());
		}

		public void StartGame(Arguments args)
		{
			var widgetArgs = new WidgetArgs();

			Ui.LoadWidget("MODCHOOSER_BACKGROUND", Ui.Root, widgetArgs);

			if (args != null && args.Contains("installMusic"))
			{
				widgetArgs.Add("modId", args.GetValue("installMusic", ""));
				Ui.OpenWindow("INSTALL_MUSIC_PANEL", widgetArgs);
			}
			else
				Ui.OpenWindow("MODCHOOSER_DIALOG", widgetArgs);
		}

		public void Dispose()
		{
			if (sprite != null)
				sprite.Sheet.Dispose();
		}
	}
}