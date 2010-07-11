#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA
{
	class Chrome : IHandleInput
	{
		public readonly Renderer renderer;
		public readonly LineRenderer lineRenderer;

		SpriteRenderer rgbaRenderer { get { return renderer.RgbaSpriteRenderer; } }
		SpriteRenderer shpRenderer { get { return renderer.WorldSpriteRenderer; } }

		internal MapStub currentMap;

		public Chrome(Renderer r, Manifest m)
		{
			this.renderer = r;
			lineRenderer = new LineRenderer(renderer);
					
			var widgetYaml = m.ChromeLayout.Select(a => MiniYaml.FromFile(a)).Aggregate(MiniYaml.Merge);
			
			if (rootWidget == null)
			{
				rootWidget = WidgetLoader.LoadWidget( widgetYaml.FirstOrDefault() );
				rootWidget.Initialize();
				rootWidget.InitDelegates();
				Widget.WindowList.Push("MAINMENU_BG");
			}
		}

		public static Widget rootWidget = null;
		public static Widget selectedWidget;
		public static ChatDisplayWidget chatWidget;

		public void Tick(World world)
		{
			rootWidget.Tick(world);
			
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;
			++ticksSinceLastMove;
		}	

		public void Draw(World world) { rootWidget.Draw(world); shpRenderer.Flush(); rgbaRenderer.Flush(); lineRenderer.Flush(); }
		
		public int ticksSinceLastMove = 0;
		public int2 lastMousePos;
		public bool HandleInput(World world, MouseInput mi)
		{
			if (selectedWidget != null && selectedWidget.HandleInput(mi))
				return true;
			
			if (rootWidget.HandleInput(mi))
				return true;

			if (mi.Event == MouseInputEvent.Move)
			{
				lastMousePos = mi.Location;
				ticksSinceLastMove = 0;
			}
			return false;
		}

		
		public bool HandleKeyPress(System.Windows.Forms.KeyPressEventArgs e, Modifiers modifiers)
		{
			if (selectedWidget != null)
				return selectedWidget.HandleKeyPress(e, modifiers);
			
			if (rootWidget.HandleKeyPress(e, modifiers))
				return true;
			return false;
		}
		
		public bool HitTest(int2 mousePos)
		{
			if (selectedWidget != null)
				return true;
			
			return rootWidget.HitTest(mousePos);
		}
	}
}
