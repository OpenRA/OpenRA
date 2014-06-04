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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc
{
	public class CncLoadScreen : ILoadScreen
	{
		Dictionary<string, string> loadInfo;
		Stopwatch loadTimer = Stopwatch.StartNew();
		Sprite[] ss;
		int loadTick;
		float2 nodPos, gdiPos, evaPos;
		Sprite nodLogo, gdiLogo, evaLogo, brightBlock, dimBlock;
		Rectangle bounds;
		Renderer r;
		NullInputHandler nih = new NullInputHandler();

		public void Init(Manifest m, Dictionary<string, string> info)
		{
			loadInfo = info;

			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;

			var s = new Sheet("mods/cnc/uibits/chrome.png");
			var res = r.Resolution;
			bounds = new Rectangle(0, 0, res.Width, res.Height);

			ss = new[]
			{
				new Sprite(s, new Rectangle(161, 128, 62, 33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(161, 223, 62, 33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128, 161, 33, 62), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223, 161, 33, 62), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128, 128, 33, 33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223, 128, 33, 33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(128, 223, 33, 33), TextureChannel.Alpha),
				new Sprite(s, new Rectangle(223, 223, 33, 33), TextureChannel.Alpha)
			};

			nodLogo = new Sprite(s, new Rectangle(0, 256, 256, 256), TextureChannel.Alpha);
			gdiLogo = new Sprite(s, new Rectangle(256, 256, 256, 256), TextureChannel.Alpha);
			evaLogo = new Sprite(s, new Rectangle(256, 64, 128, 64), TextureChannel.Alpha);
			nodPos = new float2(bounds.Width / 2 - 384, bounds.Height / 2 - 128);
			gdiPos = new float2(bounds.Width / 2 + 128, bounds.Height / 2 - 128);
			evaPos = new float2(bounds.Width - 43 - 128, 43);

			brightBlock = new Sprite(s, new Rectangle(320, 0, 16, 35), TextureChannel.Alpha);
			dimBlock = new Sprite(s, new Rectangle(336, 0, 16, 35), TextureChannel.Alpha);

			versionText = m.Mod.Version;
		}

		bool setup;
		SpriteFont loadingFont, versionFont;
		string loadingText, versionText;
		float2 loadingPos, versionPos;

		public void Display()
		{
			if (r == null || loadTimer.Elapsed.TotalSeconds < 0.25)
				return;

			loadTimer.Restart();

			loadTick = ++loadTick % 8;
			r.BeginFrame(int2.Zero, 1f);
			r.RgbaSpriteRenderer.DrawSprite(gdiLogo, gdiPos);
			r.RgbaSpriteRenderer.DrawSprite(nodLogo, nodPos);
			r.RgbaSpriteRenderer.DrawSprite(evaLogo, evaPos);

			WidgetUtils.DrawPanelPartial(ss, bounds, PanelSides.Edges);
			var barY = bounds.Height - 78;

			if (!setup && r.Fonts != null)
			{
				loadingFont = r.Fonts["BigBold"];
				loadingText = "Loading";
				loadingPos = new float2((bounds.Width - loadingFont.Measure(loadingText).X) / 2, barY);

				versionFont = r.Fonts["Regular"];
				var versionSize = versionFont.Measure(versionText);
				versionPos = new float2(bounds.Width - 107 - versionSize.X / 2, 115 - versionSize.Y / 2);

				setup = true;
			}

			if (loadingFont != null)
				loadingFont.DrawText(loadingText, loadingPos, Color.Gray);
			if (versionFont != null)
				versionFont.DrawTextWithContrast(versionText, versionPos, Color.White, Color.Black, 2);

			for (var i = 0; i <= 8; i++)
			{
				var block = loadTick == i ? brightBlock : dimBlock;
				r.RgbaSpriteRenderer.DrawSprite(block,
					new float2(bounds.Width / 2 - 114 - i * 32, barY));
				r.RgbaSpriteRenderer.DrawSprite(block,
					new float2(bounds.Width / 2 + 114 + i * 32 - 16, barY));
			}

			r.EndFrame(nih);
		}

		public void StartGame()
		{
			TestAndContinue();
		}

		void TestAndContinue()
		{
			Ui.ResetAll();
			if (!loadInfo["TestFiles"].Split(',').All(f => GlobalFileSystem.Exists(f.Trim())))
			{
				var args = new WidgetArgs()
				{
					{ "continueLoading", () => TestAndContinue() },
					{ "installData", loadInfo }
				};
				Ui.LoadWidget(loadInfo["InstallerBackgroundWidget"], Ui.Root, args);
				Ui.OpenWindow(loadInfo["InstallerMenuWidget"], args);
			}
			else
				Game.LoadShellMap();
		}
	}
}