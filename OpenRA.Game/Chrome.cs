#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

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
			}
		}

		public static Widget rootWidget = null;

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
			if (Widget.SelectedWidget != null && Widget.SelectedWidget.HandleMouseInputOuter(mi))
				return true;
			
			if (rootWidget.HandleMouseInputOuter(mi))
				return true;

			if (mi.Event == MouseInputEvent.Move)
			{
				lastMousePos = mi.Location;
				ticksSinceLastMove = 0;
			}
			return false;
		}

		
		public bool HandleKeyPress(KeyInput e)
		{
			if (Widget.SelectedWidget != null)
				return Widget.SelectedWidget.HandleKeyPressOuter(e);
			
			if (rootWidget.HandleKeyPressOuter(e))
				return true;
			return false;
		}
		
		public bool HitTest(int2 mousePos)
		{
			if (Widget.SelectedWidget != null)
				return true;
			
			return rootWidget.HitTest(mousePos);
		}
	}
}
