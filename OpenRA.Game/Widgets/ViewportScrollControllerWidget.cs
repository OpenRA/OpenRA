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
using OpenRA.Graphics;

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
		public int EdgeScrollThreshold = 15;

		ScrollDirection Keyboard;
		ScrollDirection Edge;

		public ViewportScrollControllerWidget() : base() { }
		protected ViewportScrollControllerWidget(ViewportScrollControllerWidget widget) : base(widget) {}
		public override void DrawInner( World world ) {}
		
		public override bool HandleInputInner(MouseInput mi)
		{									
			if (mi.Event == MouseInputEvent.Move &&
				(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
			{
                int InverseScroll = Game.Settings.Game.InverseDragScroll ? -1 : 1;
                Game.viewport.Scroll((Viewport.LastMousePos - mi.Location) * InverseScroll);
				return true;
			}
			return false;
		}
		
		public override string GetCursor(int2 pos)
		{
			if (!Game.Settings.Game.ViewportEdgeScroll)
				return null;

			if (Edge.Includes(ScrollDirection.Up) && Edge.Includes(ScrollDirection.Left)){
				ScrollDirection BlockedDirections = Game.viewport.GetBlockedDirections();
				if(BlockedDirections.Includes(ScrollDirection.Up) && BlockedDirections.Includes(ScrollDirection.Left))
					return "scroll-tl-blocked";
				else
					return "scroll-tl";
			}
			if (Edge.Includes(ScrollDirection.Up) && Edge.Includes(ScrollDirection.Right)){
				ScrollDirection BlockedDirections = Game.viewport.GetBlockedDirections();
				if (BlockedDirections.Includes(ScrollDirection.Up) && BlockedDirections.Includes(ScrollDirection.Right))
					return "scroll-tr-blocked";
				else
					return "scroll-tr";
			}
			if (Edge.Includes(ScrollDirection.Down) && Edge.Includes(ScrollDirection.Left)){
				ScrollDirection BlockedDirections = Game.viewport.GetBlockedDirections();
				if (BlockedDirections.Includes(ScrollDirection.Down) && BlockedDirections.Includes(ScrollDirection.Left))
					return "scroll-bl-blocked";
				else
					return "scroll-bl";
			}
			if (Edge.Includes(ScrollDirection.Down) && Edge.Includes(ScrollDirection.Right)){
				ScrollDirection BlockedDirections = Game.viewport.GetBlockedDirections();
				if (BlockedDirections.Includes(ScrollDirection.Down) && BlockedDirections.Includes(ScrollDirection.Right))
					return "scroll-br-blocked";
				else
					return "scroll-br";
			}
			
			if (Edge.Includes(ScrollDirection.Up))
				if (Game.viewport.GetBlockedDirections().Includes(ScrollDirection.Up))
					return "scroll-t-blocked";
				else
					return "scroll-t";
			if (Edge.Includes(ScrollDirection.Down))
				if (Game.viewport.GetBlockedDirections().Includes(ScrollDirection.Down))
					return "scroll-b-blocked";
				else
					return "scroll-b";
			if (Edge.Includes(ScrollDirection.Left))
				if (Game.viewport.GetBlockedDirections().Includes(ScrollDirection.Left))
					return "scroll-l-blocked";
				else
					return "scroll-l";
			if (Edge.Includes(ScrollDirection.Right))
				if (Game.viewport.GetBlockedDirections().Includes(ScrollDirection.Right))
					return "scroll-r-blocked";
				else
					return "scroll-r";
			
			return null;
		}

		public override bool LoseFocus (MouseInput mi)
		{
			Keyboard = ScrollDirection.None;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleKeyPressInner(KeyInput e)
		{			
			switch (e.KeyName)
			{
				case "up": Keyboard = Keyboard.Set(ScrollDirection.Up, (e.Event == KeyInputEvent.Down)); return true;
				case "down": Keyboard = Keyboard.Set(ScrollDirection.Down, (e.Event == KeyInputEvent.Down)); return true;
				case "left": Keyboard = Keyboard.Set(ScrollDirection.Left, (e.Event == KeyInputEvent.Down)); return true;
				case "right": Keyboard = Keyboard.Set(ScrollDirection.Right, (e.Event == KeyInputEvent.Down)); return true;
			}
			return false;
		}
		
		public override void Tick(World world)
		{
			Edge = ScrollDirection.None;
			if (Game.Settings.Game.ViewportEdgeScroll && Game.HasInputFocus)
			{
				// Check for edge-scroll
				if (Viewport.LastMousePos.X < EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Left, true);
				if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Up, true);
				if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Right, true);
				if (Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
					Edge = Edge.Set(ScrollDirection.Down, true);
			}
			
			if(Keyboard != ScrollDirection.None || Edge != ScrollDirection.None)
			{
                var scroll = new float2(0, 0);

                // Modified to use the ViewportEdgeScrollStep setting - Gecko
                if (Keyboard.Includes(ScrollDirection.Up) || Edge.Includes(ScrollDirection.Up))
                    scroll += new float2(0, -(Game.Settings.Game.ViewportEdgeScrollStep * 100));
                if (Keyboard.Includes(ScrollDirection.Right) || Edge.Includes(ScrollDirection.Right))
                    scroll += new float2((Game.Settings.Game.ViewportEdgeScrollStep * 100), 0);
                if (Keyboard.Includes(ScrollDirection.Down) || Edge.Includes(ScrollDirection.Down))
                    scroll += new float2(0, (Game.Settings.Game.ViewportEdgeScrollStep * 100));
                if (Keyboard.Includes(ScrollDirection.Left) || Edge.Includes(ScrollDirection.Left))
                    scroll += new float2(-(Game.Settings.Game.ViewportEdgeScrollStep * 100), 0);
			
				Game.viewport.Scroll(scroll);
			}
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
