#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc
{
	public class CncLoadScreen : ILoadScreen
	{
		Dictionary<string, string> Info;
		Stopwatch loadTimer = new Stopwatch();
		Sprite[] ss;
		string text;
		int loadTick;
		float2 nodPos, gdiPos, evaPos, textPos;
		Sprite nodLogo, gdiLogo, evaLogo, brightBlock, dimBlock;
		Rectangle Bounds;
		Renderer r;
		NullInputHandler nih = new NullInputHandler();

		public void Init(Dictionary<string, string> info)
		{
			Info = info;
			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;

			var res = Renderer.Resolution;

			var s = new Sheet("mods/cnc/uibits/chrome.png");
			Bounds = new Rectangle(0, 0, res.Width, res.Height);
			ss = new Sprite[]
			{
				new Sprite(s, new Rectangle(161,128,62,33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(161,223,62,33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128,161,33,62), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223,161,33,62), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128,128,33,33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223,128,33,33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128,223,33,33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223,223,33,33), TextureChannel.Alpha)
			};
			nodLogo = new Sprite(s, new Rectangle(0, 256, 256, 256), TextureChannel.Alpha);
			gdiLogo = new Sprite(s, new Rectangle(256, 256, 256, 256), TextureChannel.Alpha);
			evaLogo = new Sprite(s, new Rectangle(256, 64, 128, 64), TextureChannel.Alpha);
			nodPos = new float2(res.Width / 2 - 384, res.Height / 2 - 128);
			gdiPos = new float2(res.Width / 2 + 128, res.Height / 2 - 128);
			evaPos = new float2(res.Width - 43 - 128, 43);

			brightBlock = new Sprite(s, new Rectangle(320, 0, 16, 35), TextureChannel.Alpha);
			dimBlock = new Sprite(s, new Rectangle(336, 0, 16, 35), TextureChannel.Alpha);
		}

		public void Display()
		{
			if (r == null || loadTimer.ElapsedTime() < 0.25)
				return;
			loadTimer.Reset();

			loadTick = ++loadTick % 8;
			r.BeginFrame(float2.Zero, 1f);
			r.RgbaSpriteRenderer.DrawSprite(gdiLogo, gdiPos);
			r.RgbaSpriteRenderer.DrawSprite(nodLogo, nodPos);
			r.RgbaSpriteRenderer.DrawSprite(evaLogo, evaPos);

			var res = Renderer.Resolution;

			WidgetUtils.DrawPanelPartial(ss, Bounds, PanelSides.Edges);

			var barY = res.Height - 78;
			text = "Loading";
			var textSize = r.Fonts["BigBold"].Measure(text);
			textPos = new float2((res.Width - textSize.X) / 2, barY);
			r.Fonts["BigBold"].DrawText(text, textPos, Color.Gray);

			for (var i = 0; i <= 8; i++)
			{
				var block = loadTick == i ? brightBlock : dimBlock;
				r.RgbaSpriteRenderer.DrawSprite(block,
					new float2(res.Width / 2 - 114 - i * 32, barY));
				r.RgbaSpriteRenderer.DrawSprite(block,
					new float2(res.Width / 2 + 114 + i * 32 - 16, barY));
			}

			r.EndFrame(nih);
		}

		public void StartGame()
		{
			TestAndContinue();
		}

		void TestAndContinue()
		{
			Widget.ResetAll();
			if (!FileSystem.Exists(Info["TestFile"]))
			{
				var args = new WidgetArgs()
				{
					{ "continueLoading", () => TestAndContinue() },
					{ "installData", Info }
				};
				Widget.LoadWidget(Info["InstallerBackgroundWidget"], Widget.RootWidget, args);
				Widget.OpenWindow(Info["InstallerMenuWidget"], args);
			}
			else
				Game.LoadShellMap();
		}
	}
}

