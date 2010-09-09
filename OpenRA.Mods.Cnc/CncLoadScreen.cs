#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Support;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc
{
	public class CncLoadScreen : ILoadScreen
	{	
		static string[] Comments = new[] {	"Filling Crates...", "Charging Capacitors...", "Reticulating Splines...",
												"Planting Trees...", "Building Bridges...", "Aging Empires...",
												"Compiling EVA...", "Constructing Pylons...", "Activating Skynet...",
												"Splitting Atoms..."
		};
		
		Stopwatch lastLoadScreen = new Stopwatch();
		Rectangle StripeRect;
		Sprite Stripe, Logo;
		float2 LogoPos;
		
		Renderer r;
		SpriteFont Font;
		public void Init()
		{
			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;
			Font = r.BoldFont;
			
			var s = new Sheet("mods/cnc/uibits/loadscreen.png");
			Logo = new Sprite(s, new Rectangle(0,0,256,256), TextureChannel.Alpha);
			Stripe = new Sprite(s, new Rectangle(256,0,256,256), TextureChannel.Alpha);
			StripeRect = new Rectangle(0, Renderer.Resolution.Height/2 - 128, Renderer.Resolution.Width, 256);
			LogoPos =  new float2(Renderer.Resolution.Width/2 - 128, Renderer.Resolution.Height/2 - 128);
		}

		
		public void Display()
		{
			if (r == null)
				return;
			
			// Update text at most every 0.5 seconds
			if (lastLoadScreen.ElapsedTime() < 0.5)
				return;
			
			lastLoadScreen.Reset();
			var text = Comments.Random(Game.CosmeticRandom);
			var textSize = Font.Measure(text);
			
			r.BeginFrame(float2.Zero);
			WidgetUtils.FillRectWithSprite(StripeRect, Stripe);			
			r.RgbaSpriteRenderer.DrawSprite(Logo, LogoPos);
			Font.DrawText(text, new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20), Color.White);
			r.RgbaSpriteRenderer.Flush();
			r.EndFrame();
		}
	}
}

