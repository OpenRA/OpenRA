#region Copyright & License Information
/*
 * Copyright 2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k
{
	public class D2kLoadScreen : ILoadScreen
	{
		public static string[] Comments = new[] { "Filling Crates...", "Breeding Sandworms..." };
		public Dictionary<string, string> Info;

		Stopwatch lastLoadScreen = new Stopwatch();
		Rectangle stripeRect;
		Sprite stripe, Logo;
		float2 logoPos;

		Renderer r;
		public void Init(Dictionary<string, string> info)
		{
			Info = info;

			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;
			var s = new Sheet("mods/d2k/uibits/loadscreen.png");
			Logo = new Sprite(s, new Rectangle(0, 0, 256, 256), TextureChannel.Alpha);
			stripe = new Sprite(s, new Rectangle(256, 0, 256, 256), TextureChannel.Alpha);
			stripeRect = new Rectangle(0, r.Resolution.Height / 2 - 128, r.Resolution.Width, 256);
			logoPos = new float2(r.Resolution.Width / 2 - 128, r.Resolution.Height / 2 - 128);
		}

		public void Display()
		{
			if (r == null)
				return;

			// Update text at most every 0.5 seconds
			if (lastLoadScreen.ElapsedTime() < 0.5)
				return;

			if (r.Fonts == null)
				return;

			lastLoadScreen.Reset();
			var text = Comments.Random(Game.CosmeticRandom);
			var textSize = r.Fonts["Bold"].Measure(text);

			r.BeginFrame(float2.Zero, 1f);
			WidgetUtils.FillRectWithSprite(stripeRect, stripe);
			r.RgbaSpriteRenderer.DrawSprite(Logo, logoPos);
			r.Fonts["Bold"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), Color.White);
			r.EndFrame(new NullInputHandler());
		}

		public void StartGame()
		{
			TestAndContinue();
		}

		void TestAndContinue()
		{
			Ui.ResetAll();
			if (!FileSystem.Exists(Info["TestFile"]))
			{
				var args = new WidgetArgs()
				{
					{ "continueLoading", () => TestAndContinue() },
					{ "installData", Info }
				};
				Ui.OpenWindow(Info["InstallerMenuWidget"], args);
			}
			else
			{
				Ui.ResetAll();
				Game.LoadShellMap();
			}
		}
	}
}