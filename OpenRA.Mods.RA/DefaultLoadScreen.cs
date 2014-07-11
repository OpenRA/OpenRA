#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA
{
	public sealed class DefaultLoadScreen : ILoadScreen
	{
		Stopwatch lastUpdate = Stopwatch.StartNew();
		Renderer r;

		Rectangle stripeRect;
		float2 logoPos;
		Sheet sheet;
		Sprite stripe, logo;
		string[] messages;

		public void Init(Manifest m, Dictionary<string, string> info)
		{
			// Avoid standard loading mechanisms so we
			// can display the loadscreen as early as possible
			r = Game.Renderer;
			if (r == null)
				return;

			messages = info["Text"].Split(',');
			sheet = new Sheet(info["Image"]);
			logo = new Sprite(sheet, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
			stripe = new Sprite(sheet, new Rectangle(256, 0, 256, 256), TextureChannel.Alpha);
			stripeRect = new Rectangle(0, r.Resolution.Height / 2 - 128, r.Resolution.Width, 256);
			logoPos = new float2(r.Resolution.Width / 2 - 128, r.Resolution.Height / 2 - 128);
		}

		public void Display()
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
			WidgetUtils.FillRectWithSprite(stripeRect, stripe);
			r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);
			r.Fonts["Bold"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), Color.White);
			r.EndFrame(new NullInputHandler());
		}

		public void StartGame()
		{
			Game.TestAndContinue();
		}

		public void Dispose()
		{
			if (sheet != null)
				sheet.Dispose();
		}
	}
}
