#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Widgets
{
	[Flags]
	public enum ScrollDirection
	{
		None = 0,
		Up = 1,
		Left = 2,
		Down = 4,
		Right = 8
	}
		
	class ViewportScrollControllerWidget : Widget
	{
		public ViewportScrollControllerWidget() : base()	{}
		protected ViewportScrollControllerWidget(ViewportScrollControllerWidget widget) : base(widget) {}
		
		public override void DrawInner( World world ) {}
		

		ScrollDirection Scroll;
		public override bool HandleInputInner(MouseInput mi)
		{						
			if (mi.Event == MouseInputEvent.Move &&
				(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
			{
				Game.viewport.Scroll(Widget.LastMousePos - mi.Location);
				return true;
			}
			return false;
		}
		
		public override string GetCursor(int2 pos)
		{
			return null;
		}

		public override bool LoseFocus (MouseInput mi)
		{
			Scroll = ScrollDirection.None;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleKeyPressInner(KeyInput e)
		{
			
			switch (e.KeyName)
			{
				case "up": Scroll = Scroll.Set(ScrollDirection.Up, (e.Event == KeyInputEvent.Down)); return true;
				case "down": Scroll = Scroll.Set(ScrollDirection.Down, (e.Event == KeyInputEvent.Down)); return true;
				case "left": Scroll = Scroll.Set(ScrollDirection.Left, (e.Event == KeyInputEvent.Down)); return true;
				case "right": Scroll = Scroll.Set(ScrollDirection.Right, (e.Event == KeyInputEvent.Down)); return true;
			}
			return false;
		}
		
		public override void Tick(World world)
		{
			var scroll = new float2(0,0);
			if (Scroll.Includes(ScrollDirection.Up))
				scroll += new float2(0, -10);
			if (Scroll.Includes(ScrollDirection.Right))
				scroll += new float2(10, 0);
			if (Scroll.Includes(ScrollDirection.Down))
				scroll += new float2(0, 10);
			if (Scroll.Includes(ScrollDirection.Left))
				scroll += new float2(-10, 0);
			
			Game.viewport.Scroll(scroll);
		}
		
		public override Widget Clone() { return new ViewportScrollControllerWidget(this); }
	}
	
	public static class ViewportExts
	{	
		public static bool Includes(this ScrollDirection d, ScrollDirection s)
		{
			return (d & s) == s;
		}
		
		public static ScrollDirection Set(this ScrollDirection d, ScrollDirection s, bool val)
		{
			return (d.Includes(s) != val) ? d ^ s : d;
		}
	}
}