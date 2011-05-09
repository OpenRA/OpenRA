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
using System.Linq;
using OpenRA.Support;
using OpenRA.Graphics;
using OpenRA.Widgets;
using OpenRA.FileFormats;
using System;

namespace OpenRA.Mods.Cnc
{
	public class CncLoadScreen : ILoadScreen
	{	
		static string[] Comments = new[] {	"Filling Crates...", "Charging Capacitors...", "Reticulating Splines...",
												"Planting Trees...", "Building Bridges...", "Aging Empires...",
												"Compiling EVA...", "Constructing Pylons...", "Activating Skynet...",
												"Splitting Atoms..."
		};
		
		Dictionary<string,string> Info;
		Stopwatch lastLoadScreen = new Stopwatch();
		Rectangle StripeRect, BgRect;
		Sprite Stripe, Logo, Bg;
		float2 LogoPos;
		
		Renderer r;
		SpriteFont Font;
		public void Init(Dictionary<string, string> info)
		{
			Info = info;
			// Avoid standard loading mechanisms so we
			// can display loadscreen as early as possible
			r = Game.Renderer;
			if (r == null) return;
			Font = r.BoldFont;
			
			var s = new Sheet("mods/cnc/uibits/loadscreen.png");
			Logo = new Sprite(s, new Rectangle(0,0,256,256), TextureChannel.Alpha);
			Bg = new Sprite(s, new Rectangle(0,256,512,256), TextureChannel.Alpha);
			BgRect = new Rectangle(0, 0, Renderer.Resolution.Width, Renderer.Resolution.Height);
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
			WidgetUtils.FillRectWithSprite(BgRect, Bg);
			WidgetUtils.FillRectWithSprite(StripeRect, Stripe);			
			r.RgbaSpriteRenderer.DrawSprite(Logo, LogoPos);
			Font.DrawText(text, new float2(Renderer.Resolution.Width - textSize.X - 20, Renderer.Resolution.Height - textSize.Y - 20), Color.Black);
			r.EndFrame( new NullInputHandler() );
		}
		
		public void StartGame()
		{
			TestAndContinue();
		}

		void TestAndContinue()
		{
			Widget.RootWidget.RemoveChildren();
			if (!FileSystem.Exists(Info["TestFile"]))
			{
				var args = new Dictionary<string, object>()
				{
					{ "continueLoading", (Action)(() => TestAndContinue()) },
					{ "installData", Info }
				};
				Widget.LoadWidget(Info["InstallerBackgroundWidget"], args);
				Widget.OpenWindow(Info["InstallerMenuWidget"], args);
			}
			else
				Game.LoadShellMap();
		}
	}
}

