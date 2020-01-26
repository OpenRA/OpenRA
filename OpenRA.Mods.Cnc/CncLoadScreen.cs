#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc
{
	public sealed class CncLoadScreen : SheetLoadScreen
	{
		int loadTick;

		Sprite nodLogo, gdiLogo, evaLogo, brightBlock, dimBlock;
		Sprite[] border;
		float2 nodPos, gdiPos, evaPos;
		Rectangle bounds;

		SpriteFont loadingFont, versionFont;
		string loadingText, versionText;
		float2 loadingPos, versionPos;

		Sheet lastSheet;
		Size lastResolution;
		IReadOnlyDictionary<string, SpriteFont> lastFonts;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			versionText = modData.Manifest.Metadata.Version;
		}

		public override void DisplayInner(Renderer r, Sheet s)
		{
			if (s != lastSheet)
			{
				lastSheet = s;

				border = new[]
				{
					new Sprite(s, new Rectangle(129, 129, 32, 32), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(161, 129, 62, 32), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(223, 129, 32, 32), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(129, 161, 32, 62), TextureChannel.RGBA),
					null,
					new Sprite(s, new Rectangle(223, 161, 32, 62), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(129, 223, 32, 32), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(161, 223, 62, 32), TextureChannel.RGBA),
					new Sprite(s, new Rectangle(223, 223, 32, 32), TextureChannel.RGBA)
				};

				nodLogo = new Sprite(s, new Rectangle(0, 256, 256, 256), TextureChannel.RGBA);
				gdiLogo = new Sprite(s, new Rectangle(256, 256, 256, 256), TextureChannel.RGBA);
				evaLogo = new Sprite(s, new Rectangle(769, 320, 128, 64), TextureChannel.RGBA);

				brightBlock = new Sprite(s, new Rectangle(777, 385, 16, 35), TextureChannel.RGBA);
				dimBlock = new Sprite(s, new Rectangle(794, 385, 16, 35), TextureChannel.RGBA);
			}

			if (r.Resolution != lastResolution)
			{
				lastResolution = r.Resolution;

				bounds = new Rectangle(0, 0, lastResolution.Width, lastResolution.Height);
				nodPos = new float2(bounds.Width / 2 - 384, bounds.Height / 2 - 128);
				gdiPos = new float2(bounds.Width / 2 + 128, bounds.Height / 2 - 128);
				evaPos = new float2(bounds.Width - 43 - 128, 43);
			}

			var barY = bounds.Height - 78;

			// The fonts dictionary may change when switching between the mod and content installer
			if (r.Fonts != lastFonts)
			{
				lastFonts = r.Fonts;

				loadingFont = lastFonts["BigBold"];
				loadingText = Info["Text"];
				loadingPos = new float2((bounds.Width - loadingFont.Measure(loadingText).X) / 2, barY);

				versionFont = lastFonts["Regular"];
				var versionSize = versionFont.Measure(versionText);
				versionPos = new float2(bounds.Width - 107 - versionSize.X / 2, 115 - versionSize.Y / 2);
			}

			loadTick = ++loadTick % 8;

			r.RgbaSpriteRenderer.DrawSprite(gdiLogo, gdiPos);
			r.RgbaSpriteRenderer.DrawSprite(nodLogo, nodPos);
			r.RgbaSpriteRenderer.DrawSprite(evaLogo, evaPos);

			WidgetUtils.DrawPanel(bounds, border);

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
		}
	}
}
