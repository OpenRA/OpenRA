#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA
{
	public class SidebarButtonWidget : ButtonWidget
	{
		public string Image = "";
		
		readonly World world;
		[ObjectCreator.UseCtor]
		public SidebarButtonWidget( [ObjectCreator.Param] World world )
			: base()
		{
			this.world = world;
		}
		
		protected SidebarButtonWidget(SidebarButtonWidget widget)
			: base(widget)
		{
			this.world = widget.world;
		}
		
		public override void Draw()
		{
			var state = Depressed ? "pressed" : 
						RenderBounds.Contains(Viewport.LastMousePos) ? "hover" : "normal";
			
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage(Image + "-" + world.LocalPlayer.Country.Race, state), RenderOrigin);
		}
	}
}

