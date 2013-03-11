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
                // First let's check if the mouse is on the corners:
                if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
                    Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold) //Bottom Right
                {
                    Edge = Edge.Set(ScrollDirection.Right, true);
                    Scroll();
                    Edge = Edge.Set(ScrollDirection.Down, true);
                    Scroll();
                    return;
                }
                if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
                   Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeCornerScrollThreshold) //Bottom Left
                {
                    Edge = Edge.Set(ScrollDirection.Left, true);
                    Scroll();
                    Edge = Edge.Set(ScrollDirection.Down, true);
                    Scroll();
                    return;
                }

                if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeCornerScrollThreshold &&
                   Viewport.LastMousePos.Y < EdgeCornerScrollThreshold) //Top Right
                {
                    Edge = Edge.Set(ScrollDirection.Right, true);
                    Scroll();
                    Edge = Edge.Set(ScrollDirection.Up, true);
                    Scroll();
                    return;
                }

                if (Viewport.LastMousePos.X < EdgeCornerScrollThreshold &&
                    Viewport.LastMousePos.Y < EdgeCornerScrollThreshold) //Top Left
                {
                    Edge = Edge.Set(ScrollDirection.Left, true);
                    Scroll();
                    Edge = Edge.Set(ScrollDirection.Up, true);
                    Scroll();
                    return;
                }


                //Check for corner ends here now let's check the edges:

                // Check for edge-scroll
                if (Viewport.LastMousePos.X < EdgeScrollThreshold)
                    Edge = Edge.Set(ScrollDirection.Left, true);
                if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
                    Edge = Edge.Set(ScrollDirection.Up, true);
                if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
                    Edge = Edge.Set(ScrollDirection.Right, true);
                if (Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
                    Edge = Edge.Set(ScrollDirection.Down, true);

                Scroll();
                //Check for edge-scroll ends here.
            }


        }

        public void Scroll()
        {
            if (Keyboard != ScrollDirection.None || Edge != ScrollDirection.None)
            {
                var scroll = new float2(0, 0);

                if (Keyboard.Includes(ScrollDirection.Up) || Edge.Includes(ScrollDirection.Up))
                    scroll += new float2(0, -1);
                if (Keyboard.Includes(ScrollDirection.Right) || Edge.Includes(ScrollDirection.Right))
                    scroll += new float2(1, 0);
                if (Keyboard.Includes(ScrollDirection.Down) || Edge.Includes(ScrollDirection.Down))
                    scroll += new float2(0, 1);
                if (Keyboard.Includes(ScrollDirection.Left) || Edge.Includes(ScrollDirection.Left))
                    scroll += new float2(-1, 0);

                float length = Math.Max(1, scroll.Length);
                scroll.X = (scroll.X / length) * Game.Settings.Game.ViewportEdgeScrollStep;
                scroll.Y = (scroll.Y / length) * Game.Settings.Game.ViewportEdgeScrollStep;

                Game.viewport.Scroll(scroll);
            }
        }

        public override Widget Clone() { return new ViewportScrollControllerWidget(this); }
    }
}
