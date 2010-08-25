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

namespace OpenRA.Mods.RA
{
	public class RALoadScreen : ILoadScreen
	{	
		static string[] loadComments = new[] {	"Filling Crates...", "Charging Capacitors...", "Reticulating Splines...",
												"Planting Trees...", "Building Bridges...", "Aging Empires...",
												"Compiling EVA...", "Constructing Pylons...", "Activating Skynet...",
												"Splitting Atoms..."
		};
		
		static Stopwatch lastLoadScreen = new Stopwatch();
		public void Display()
		{
			if (Game.Renderer == null)
				return;
			
			// Update text at most every 0.5 seconds
			if (lastLoadScreen.ElapsedTime() < 0.5)
				return;
			
			lastLoadScreen.Reset();
			
			var r = Game.Renderer;
			var font = r.BoldFont;
			r.BeginFrame(float2.Zero);
			
			WidgetUtils.FillRectWithSprite(new Rectangle(0, Renderer.Resolution.Height/2 - 64, Renderer.Resolution.Width, 128), ChromeProvider.GetImage("loadscreen", "stripe"));
			
			var logo = ChromeProvider.GetImage("loadscreen","logo");
			var logoPos =  new float2((Renderer.Resolution.Width - logo.size.X)/2,(Renderer.Resolution.Height - logo.size.Y)/2);
			r.RgbaSpriteRenderer.DrawSprite(logo, logoPos);
			
			var text = loadComments.Random(Game.CosmeticRandom);
			var textSize = font.Measure(text);
			
			font.DrawText(text, new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20), Color.White);
			
			r.RgbaSpriteRenderer.Flush();
			r.EndFrame();
		}
	}
}

