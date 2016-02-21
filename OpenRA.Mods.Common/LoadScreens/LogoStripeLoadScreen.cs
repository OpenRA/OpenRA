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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public sealed class LogoStripeLoadScreen : BlankLoadScreen
	{
		Stopwatch lastUpdate = Stopwatch.StartNew();
		Renderer r;

		Rectangle stripeRect;
		float2 logoPos;
		Sheet sheet;
		Sprite stripe, logo;
		string[] messages = { "Loading..." };

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			// Avoid standard loading mechanisms so we
			// can display the loadscreen as early as possible
			r = Game.Renderer;
			if (r == null)
				return;

			if (info.ContainsKey("Text"))
				messages = info["Text"].Split(',');

			if (info.ContainsKey("Image"))
			{
				using (var stream = modData.DefaultFileSystem.Open(info["Image"]))
					sheet = new Sheet(SheetType.BGRA, stream);

				logo = new Sprite(sheet, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
				stripe = new Sprite(sheet, new Rectangle(256, 0, 256, 256), TextureChannel.Alpha);
				stripeRect = new Rectangle(0, r.Resolution.Height / 2 - 128, r.Resolution.Width, 256);
				logoPos = new float2(r.Resolution.Width / 2 - 128, r.Resolution.Height / 2 - 128);
			}
		}

		public override void Display()
		{
			if (r == null)
				return;

			// Update text at most every 0.5 seconds
			if (lastUpdate.Elapsed.TotalSeconds < 0.5)
				return;

			if (r.Fonts == null)
				return;

			lastUpdate.Restart();
			var text = messages.Random(Game.CosmeticRandom);
			var textSize = r.Fonts["Bold"].Measure(text);

			r.BeginFrame(int2.Zero, 1f);

			if (stripe != null)
				WidgetUtils.FillRectWithSprite(stripeRect, stripe);

			if (logo != null)
				r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);

			r.Fonts["Bold"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), Color.White);
			r.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && sheet != null)
				sheet.Dispose();

			base.Dispose(disposing);
		}
	}
}
