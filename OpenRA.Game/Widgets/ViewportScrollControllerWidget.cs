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
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class ViewportScrollControllerWidget : Widget
	{
		public int EdgeScrollThreshold = 15;
		public int EdgeCornerScrollThreshold = 35;

		ScrollDirection Keyboard;
		ScrollDirection Edge;

		float DirectionVectorX = 0;
		float DirectionVectorY = 0;
		float ScrollScale = 0;
		float ScrollScaleMax = 5;
		float2 ScrollAmount = new float2(0, 0);

		public ViewportScrollControllerWidget() : base() { }
		protected ViewportScrollControllerWidget(ViewportScrollControllerWidget widget)
			: base(widget) { }

		public override bool HandleMouseInput(MouseInput mi)
		{
			var scrolltype = Game.Settings.Game.MouseScroll;
			if (scrolltype == MouseScrollType.Disabled)
				return false;

			if (mi.Event == MouseInputEvent.Move &&
			    (mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
			{
				var d = scrolltype == MouseScrollType.Inverted ? -1 : 1;
				Game.viewport.Scroll((Viewport.LastMousePos - mi.Location) * d);
				return true;
			}
			return false;
		}

		static readonly Dictionary<ScrollDirection, string> directions = new Dictionary<ScrollDirection, string>
		{
			{ ScrollDirection.Up | ScrollDirection.Left, "scroll-tl" },
			{ ScrollDirection.Up | ScrollDirection.Right, "scroll-tr" },
			{ ScrollDirection.Down | ScrollDirection.Left, "scroll-bl" },
			{ ScrollDirection.Down | ScrollDirection.Right, "scroll-br" },

			{ ScrollDirection.Up, "scroll-t" },
			{ ScrollDirection.Down, "scroll-b" },
			{ ScrollDirection.Left, "scroll-l" },
			{ ScrollDirection.Right, "scroll-r" },
		};

		public static string GetScrollCursor(Widget w, ScrollDirection edge, int2 pos)
		{
			if (!Game.Settings.Game.ViewportEdgeScroll || Ui.MouseOverWidget != w)
				return null;

			var blockedDirections = Game.viewport.GetBlockedDirections();

			foreach (var dir in directions)
				if (edge.Includes(dir.Key))
					return dir.Value + (blockedDirections.Includes(dir.Key) ? "-blocked" : "");

			return null;
		}

		public override string GetCursor(int2 pos) { return GetScrollCursor(this, Edge, pos); }

		public override bool LoseFocus(MouseInput mi)
		{
			Keyboard = ScrollDirection.None;
			return base.LoseFocus(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			switch (e.KeyName)
			{
				case "up": Keyboard = Keyboard.Set(ScrollDirection.Up, e.Event == KeyInputEvent.Down); return true;
				case "down": Keyboard = Keyboard.Set(ScrollDirection.Down, e.Event == KeyInputEvent.Down); return true;
				case "left": Keyboard = Keyboard.Set(ScrollDirection.Left, e.Event == KeyInputEvent.Down); return true;
				case "right": Keyboard = Keyboard.Set(ScrollDirection.Right, e.Event == KeyInputEvent.Down); return true;
			}
			return false;
		}

		public override void Tick()
		{
			Edge = ScrollDirection.None;
			if (Game.Settings.Game.ViewportEdgeScroll && Game.HasInputFocus)
			{
				Edge = CheckForDirections();
			}

			if (Edge != ScrollDirection.None || Keyboard != ScrollDirection.None ) 
			{
				Scroll();
			} 
			else if (ScrollScale != 0) 
			{
				ScrollScale = 0;
				DirectionVectorY = 0;
				DirectionVectorX = 0;
			}
		}

		void Scroll()
		{
			if (Edge == ScrollDirection.UpLeft || Keyboard == ScrollDirection.UpLeft)
			{	//	up-left
				DirectionVectorY = -1;
				DirectionVectorX = -1;
			}
			else if (Edge == ScrollDirection.UpRight || Keyboard == ScrollDirection.UpRight)
			{	//	up-right
				DirectionVectorY = -1;
				DirectionVectorX = 1;
			} 
			else if (Edge == ScrollDirection.DownLeft || Keyboard == ScrollDirection.DownLeft)
			{	//	down-left
				DirectionVectorY = 1;
				DirectionVectorX = -1;
			} 
			else if (Edge == ScrollDirection.DownRight || Keyboard == ScrollDirection.DownRight)
			{	//	down-right
				DirectionVectorY = 1;
				DirectionVectorX = 1;
			} 
			else if (Keyboard.Includes (ScrollDirection.Up) || Edge.Includes (ScrollDirection.Up)) 
			{	//	up
				DirectionVectorY = -1;
				DirectionVectorX = 0;
			} 
			else if (Keyboard.Includes (ScrollDirection.Right) || Edge.Includes (ScrollDirection.Right))
			{	//	right
				DirectionVectorX = 1;
				DirectionVectorY = 0;
			} 
			else if (Keyboard.Includes (ScrollDirection.Down) || Edge.Includes (ScrollDirection.Down)) 
			{	//	down
				DirectionVectorY = 1;
				DirectionVectorX = 0;
			} 
			else if (Keyboard.Includes (ScrollDirection.Left) || Edge.Includes (ScrollDirection.Left)) 
			{	//	left
				DirectionVectorX = -1;
				DirectionVectorY = 0;
			}

			if(ScrollScale <= ScrollScaleMax)
			{
				ScrollScale += 0.5f;
			}

			ScrollAmount.X = DirectionVectorX * (Game.Settings.Game.ViewportEdgeScrollStep * ScrollScale);
			ScrollAmount.Y = DirectionVectorY * (Game.Settings.Game.ViewportEdgeScrollStep * ScrollScale);

			Game.viewport.Scroll(ScrollAmount);
		}

		ScrollDirection CheckForDirections()
		{
			// First let's check if the mouse is on the corners:
			if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
			    Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold) //Bottom Right
			{
				return ScrollDirection.Right | ScrollDirection.Down;
			}
			else if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold) //Bottom Left
			{
				return ScrollDirection.Down | ScrollDirection.Left;
			}

			else if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y < EdgeCornerScrollThreshold) //Top Right
			{
				return ScrollDirection.Right | ScrollDirection.Up;
			}

			else if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y < EdgeCornerScrollThreshold) //Top Left
			{
				return ScrollDirection.Left | ScrollDirection.Up;
			}

			//Check for corner ends here now let's check the edges:

			// Check for edge-scroll
			if (Viewport.LastMousePos.X < EdgeScrollThreshold)
				return ScrollDirection.Left;
			if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
				return ScrollDirection.Up;
			if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
				return ScrollDirection.Right;
			if (Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
				return ScrollDirection.Down;


			//Check for edge-scroll ends here.If none of above then return none.
			return ScrollDirection.None;
		}

		public override Widget Clone() { return new ViewportScrollControllerWidget(this); }
	}
}
