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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionTabsWidget : Widget
	{
		public string QueueType = null;
		string cachedQueueType = null;
		public string PaletteWidget = null;
		
		List<Pair<Rectangle, ProductionQueue>> buttons = new List<Pair<Rectangle,ProductionQueue>>();
		List<ProductionQueue> VisibleQueues = new List<ProductionQueue>();

		readonly World world;
		
		[ObjectCreator.UseCtor]
		public ProductionTabsWidget( [ObjectCreator.Param] World world )
		{
			this.world = world;
		}
		
		public override void Tick()
		{
			VisibleQueues.Clear();
			
			VisibleQueues = world.ActorsWithTrait<ProductionQueue>()
				.Where(p => p.Actor.Owner == world.LocalPlayer && p.Trait.Info.Type == QueueType)
				.Select(p => p.Trait).ToList();
			
			var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
			if (VisibleQueues.Count() == 0)
				palette.CurrentQueue = null;
			else if (palette.CurrentQueue == null || cachedQueueType != QueueType)
			{
				palette.CurrentQueue = VisibleQueues.First();
				cachedQueueType = QueueType;
			}
			base.Tick();
		}
		
		public override bool HandleMouseInput(MouseInput mi)
		{			
			if (mi.Event != MouseInputEvent.Down)
				return false;
			
			var queue = buttons.Where(a => a.First.Contains(mi.Location))
					.Select(a => a.Second).FirstOrDefault();
			if (queue == null)
				return true;
			
			var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);

			palette.CurrentQueue = queue;
			return true;
		}
		
		public override void DrawInner()
		{	
			if (!IsVisible()) return;
			buttons.Clear();
			int x = 0;
			var palette = Widget.RootWidget.GetWidget<ProductionPaletteWidget>(PaletteWidget);
			
			// Giant hack
			var width = 30;

			foreach (var queue in VisibleQueues)
			{
				var foo = queue;
				var rect = new Rectangle(RenderBounds.X + x,RenderBounds.Y,width, RenderBounds.Height);
				var state = palette.CurrentQueue == queue ? 2 : 
						rect.Contains(Viewport.LastMousePos) ? 1 : 0;
				x += width;
				
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("button", "background"), new int2(rect.Location));
				buttons.Add(Pair.New(rect, foo));
			}
		}
	}
}
