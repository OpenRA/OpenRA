#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class ModChooserLoadScreen : ILoadScreen
	{
		Sprite sprite;
		Rectangle bounds;

		public void Init(ModData modData, Dictionary<string, string> info)
		{
			var res = Game.Renderer.Resolution;
			bounds = new Rectangle(0, 0, res.Width, res.Height);

			using (var stream = modData.DefaultFileSystem.Open(info["Image"]))
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
			Ui.OpenWindow("MODCHOOSER_DIALOG", widgetArgs);
		}

		public void Dispose()
		{
			if (sprite != null)
				sprite.Sheet.Dispose();
		}

		public bool RequiredContentIsInstalled()
		{
			return true;
		}
	}
}