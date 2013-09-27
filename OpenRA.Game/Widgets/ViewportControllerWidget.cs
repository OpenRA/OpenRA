#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public enum WorldTooltipType { None, Unexplored, Actor, FrozenActor }

	public class ViewportControllerWidget : Widget
	{
		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public WorldTooltipType TooltipType { get; private set; }
		public IToolTip ActorTooltip { get; private set; }
		public FrozenActor FrozenActorTooltip { get; private set; }

		public int EdgeScrollThreshold = 15;
		public int EdgeCornerScrollThreshold = 35;

		static readonly Dictionary<ScrollDirection, string> ScrollCursors = new Dictionary<ScrollDirection, string>
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

		static readonly Dictionary<ScrollDirection, float2> ScrollOffsets = new Dictionary<ScrollDirection, float2>
		{
			{ ScrollDirection.Up, new float2(0, -1) },
			{ ScrollDirection.Down, new float2(0, 1) },
			{ ScrollDirection.Left, new float2(-1, 0) },
			{ ScrollDirection.Right, new float2(1, 0) },
		};

		ScrollDirection keyboardDirections;
		ScrollDirection edgeDirections;
		World world;

		[ObjectCreator.UseCtor]
		public ViewportControllerWidget(World world, WorldRenderer worldRenderer)
			: base()
		{
			this.world = world;
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() {{ "world", world }, { "viewport", this }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			UpdateMouseover();
			base.Draw();
		}

		public void UpdateMouseover()
		{
			TooltipType = WorldTooltipType.None;
			var cell = Game.viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.IsInMap(cell))
				return;

			if (world.ShroudObscures(cell))
			{
				TooltipType = WorldTooltipType.Unexplored;
				return;
			}

			var underCursor = world.ScreenMap.ActorsAt(Game.viewport.ViewToWorldPx(Viewport.LastMousePos))
				.Where(a => !world.FogObscures(a) && a.HasTrait<IToolTip>())
				.OrderByDescending(a => a.Info.SelectionPriority())
				.FirstOrDefault();

			if (underCursor != null)
			{
				ActorTooltip = underCursor.TraitsImplementing<IToolTip>().First();
				TooltipType = WorldTooltipType.Actor;
				return;
			}

			var frozen = world.FindFrozenActorsAtMouse(Viewport.LastMousePos)
				.Where(a => a.TooltipName != null)
				.OrderByDescending(a => a.Info.SelectionPriority())
				.FirstOrDefault();

			if (frozen != null)
			{
				FrozenActorTooltip = frozen;
				TooltipType = WorldTooltipType.FrozenActor;
			}
		}

		public static string GetScrollCursor(Widget w, ScrollDirection edge, int2 pos)
		{
			if (!Game.Settings.Game.ViewportEdgeScroll || Ui.MouseOverWidget != w)
				return null;

			var blockedDirections = Game.viewport.GetBlockedDirections();
			foreach (var dir in ScrollCursors)
				if (edge.Includes(dir.Key))
					return dir.Value + (blockedDirections.Includes(dir.Key) ? "-blocked" : "");

			return null;
		}

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

		public override string GetCursor(int2 pos) { return GetScrollCursor(this, edgeDirections, pos); }

		public override bool YieldKeyboardFocus()
		{
			keyboardDirections = ScrollDirection.None;
			return base.YieldKeyboardFocus();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			switch (e.KeyName)
			{
				case "up": keyboardDirections = keyboardDirections.Set(ScrollDirection.Up, e.Event == KeyInputEvent.Down); return true;
				case "down": keyboardDirections = keyboardDirections.Set(ScrollDirection.Down, e.Event == KeyInputEvent.Down); return true;
				case "left": keyboardDirections = keyboardDirections.Set(ScrollDirection.Left, e.Event == KeyInputEvent.Down); return true;
				case "right": keyboardDirections = keyboardDirections.Set(ScrollDirection.Right, e.Event == KeyInputEvent.Down); return true;
			}

			return false;
		}

		public override void Tick()
		{
			edgeDirections = ScrollDirection.None;
			if (Game.Settings.Game.ViewportEdgeScroll && Game.HasInputFocus)
				edgeDirections = CheckForDirections();

			if (keyboardDirections != ScrollDirection.None || edgeDirections != ScrollDirection.None)
			{
				var scroll = float2.Zero;

				foreach (var kv in ScrollOffsets)
					if (keyboardDirections.Includes(kv.Key) || edgeDirections.Includes(kv.Key))
						scroll += kv.Value;

				var length = Math.Max(1, scroll.Length);
				scroll *= (1f / length) * Game.Settings.Game.ViewportEdgeScrollStep;

				Game.viewport.Scroll(scroll);
			}
		}

		ScrollDirection CheckForDirections()
		{
			var directions = ScrollDirection.None;
			if (Viewport.LastMousePos.X < EdgeScrollThreshold)
				directions |= ScrollDirection.Left;
			if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
				directions |= ScrollDirection.Up;
			if (Viewport.LastMousePos.X >= Game.viewport.Width - EdgeScrollThreshold)
				directions |= ScrollDirection.Right;
			if (Viewport.LastMousePos.Y >= Game.viewport.Height - EdgeScrollThreshold)
				directions |= ScrollDirection.Down;

			return directions;
		}
	}
}
