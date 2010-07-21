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
	class Chrome
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
			
			if (Widget.RootWidget == null)
			{
				Widget.RootWidget = WidgetLoader.LoadWidget( widgetYaml.FirstOrDefault() );
				Widget.RootWidget.Initialize();
				Widget.RootWidget.InitDelegates();
			}
		}

		public void Tick(World world)
		{
			Widget.RootWidget.Tick(world);
			
			if (!world.GameHasStarted) return;
			if (world.LocalPlayer == null) return;
			++Widget.ticksSinceLastMove;
		}	

		public void Draw(World world) { Widget.RootWidget.Draw(world); shpRenderer.Flush(); rgbaRenderer.Flush(); lineRenderer.Flush(); }

	}
}
