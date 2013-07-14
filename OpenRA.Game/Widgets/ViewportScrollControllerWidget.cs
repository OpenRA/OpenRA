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
		static readonly Dictionary<ScrollDirection, string> Directions = new Dictionary<ScrollDirection, string>
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

		public int EdgeScrollThreshold = 15;
		public int EdgeCornerScrollThreshold = 35;

		ScrollDirection keyboard;
		ScrollDirection edge;
		float currentScrollScale = 0;
		float scrollScaleLimit = 5;
		float2 scrollAmount = new float2(0, 0);
		float2 directionVector = new float2(0, 0);

		public static string GetScrollCursor(Widget w, ScrollDirection edge, int2 pos)
		{
			if (!Game.Settings.Game.ViewportEdgeScroll || Ui.MouseOverWidget != w)
				return null;

			var blockedDirections = Game.viewport.GetBlockedDirections();

			foreach (var dir in Directions)
				if (edge.Includes(dir.Key))
					return dir.Value + (blockedDirections.Includes(dir.Key) ? "-blocked" : "");

			return null;
		}

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

		public override string GetCursor(int2 pos) { return GetScrollCursor(this, edge, pos); }

		public override bool LoseFocus(MouseInput mi)
		{
			keyboard = ScrollDirection.None;
			return base.LoseFocus(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			switch (e.KeyName)
			{
				case "up": keyboard = keyboard.Set(ScrollDirection.Up, e.Event == KeyInputEvent.Down); return true;
				case "down": keyboard = keyboard.Set(ScrollDirection.Down, e.Event == KeyInputEvent.Down); return true;
				case "left": keyboard = keyboard.Set(ScrollDirection.Left, e.Event == KeyInputEvent.Down); return true;
				case "right": keyboard = keyboard.Set(ScrollDirection.Right, e.Event == KeyInputEvent.Down); return true;
			}

			return false;
		}

		public override void Tick()
		{
			edge = ScrollDirection.None;
			if (Game.Settings.Game.ViewportEdgeScroll && Game.HasInputFocus)
			{
				edge = CheckForDirections();
			}

			if (edge != ScrollDirection.None || keyboard != ScrollDirection.None) 
			{
				Scroll();
			} 
			else if (currentScrollScale != 0) 
			{
				currentScrollScale = 0;
				directionVector.Y = 0;
				directionVector.X = 0;
			}
		}

		void Scroll()
		{
			if (edge == ScrollDirection.UpLeft || keyboard == ScrollDirection.UpLeft)
			{
				directionVector.Y = -1;
				directionVector.X = -1;
			}
			else if (edge == ScrollDirection.UpRight || keyboard == ScrollDirection.UpRight)
			{
				directionVector.Y = -1;
				directionVector.X = 1;
			} 
			else if (edge == ScrollDirection.DownLeft || keyboard == ScrollDirection.DownLeft)
			{
				directionVector.Y = 1;
				directionVector.X = -1;
			} 
			else if (edge == ScrollDirection.DownRight || keyboard == ScrollDirection.DownRight)
			{
				directionVector.Y = 1;
				directionVector.X = 1;
			} 
			else if (keyboard.Includes(ScrollDirection.Up) || edge.Includes(ScrollDirection.Up)) 
			{
				directionVector.Y = -1;
				directionVector.X = 0;
			} 
			else if (keyboard.Includes(ScrollDirection.Down) || edge.Includes(ScrollDirection.Down)) 
			{
				directionVector.Y = 1;
				directionVector.X = 0;
			} 
			else if (keyboard.Includes(ScrollDirection.Left) || edge.Includes(ScrollDirection.Left)) 
			{
				directionVector.X = -1;
				directionVector.Y = 0;
			}
			else if (keyboard.Includes(ScrollDirection.Right) || edge.Includes(ScrollDirection.Right))
			{
				directionVector.X = 1;
				directionVector.Y = 0;
			} 

			if (currentScrollScale <= scrollScaleLimit)
			{
				currentScrollScale += 0.1f;
			}

			scrollAmount.X = directionVector.X * (Game.Settings.Game.ViewportEdgeScrollStep * currentScrollScale);
			scrollAmount.Y = directionVector.Y * (Game.Settings.Game.ViewportEdgeScrollStep * currentScrollScale);

			Game.viewport.Scroll(scrollAmount);
		}

		ScrollDirection CheckForDirections()
		{
			//// First let's check if the mouse is on the corners:
			if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
			    Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold)
			{
				return ScrollDirection.Right | ScrollDirection.Down;
			}
			else if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold)
			{
				return ScrollDirection.Down | ScrollDirection.Left;
			}
			else if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y < EdgeCornerScrollThreshold)
			{
				return ScrollDirection.Right | ScrollDirection.Up;
			}
			else if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
			         Viewport.LastMousePos.Y < EdgeCornerScrollThreshold)
			{
				return ScrollDirection.Left | ScrollDirection.Up;
			}

			// Check for corner ends here now let's check the edges:
			// Check for edge-scroll
			if (Viewport.LastMousePos.X < EdgeScrollThreshold)
				return ScrollDirection.Left;
			if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
				return ScrollDirection.Up;
			if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
				return ScrollDirection.Right;
			if (Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
				return ScrollDirection.Down;

			// Check for edge-scroll ends here.If none of above then return none.
			return ScrollDirection.None;
		}

		public override Widget Clone() { return new ViewportScrollControllerWidget(this); }
	}
}
