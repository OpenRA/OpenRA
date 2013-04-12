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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public enum WorldTooltipType { None, Unexplored, Actor }

	public class CncWorldInteractionControllerWidget : WorldInteractionControllerWidget
	{
		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public WorldTooltipType TooltipType { get; private set; }
		public IToolTip ActorTooltip { get; private set; }

		public int EdgeScrollThreshold = 15;
		ScrollDirection Keyboard;
		ScrollDirection Edge;

		[ObjectCreator.UseCtor]
		public CncWorldInteractionControllerWidget(World world, WorldRenderer worldRenderer)
			: base(world, worldRenderer)
		{
			tooltipContainer = Lazy.New(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() {{ "world", world }, { "wic", this }});
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
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

			var actor = world.FindUnitsAtMouse(Viewport.LastMousePos).FirstOrDefault();
			if (actor == null)
				return;

			ActorTooltip = actor.TraitsImplementing<IToolTip>().FirstOrDefault();
			if (ActorTooltip != null)
				TooltipType = WorldTooltipType.Actor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var scrolltype = Game.Settings.Game.MouseScroll;
			if (scrolltype != OpenRA.GameRules.MouseScrollType.Disabled && mi.Event == MouseInputEvent.Move &&
					(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
			{
				var d = scrolltype == OpenRA.GameRules.MouseScrollType.Inverted ? -1 : 1;
				Game.viewport.Scroll((Viewport.LastMousePos - mi.Location) * d);
			}

			return base.HandleMouseInput(mi);
		}

		public override string GetCursor(int2 pos)
		{
			return ViewportScrollControllerWidget.GetScrollCursor(this, Edge, pos)
				?? base.GetCursor(pos);
		}

		public override bool LoseFocus(MouseInput mi)
		{
			Keyboard = ScrollDirection.None;
			return base.LoseFocus(mi);
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			switch (e.KeyName)
			{
				case "up": Keyboard = Keyboard.Set(ScrollDirection.Up, (e.Event == KeyInputEvent.Down)); return true;
				case "down": Keyboard = Keyboard.Set(ScrollDirection.Down, (e.Event == KeyInputEvent.Down)); return true;
				case "left": Keyboard = Keyboard.Set(ScrollDirection.Left, (e.Event == KeyInputEvent.Down)); return true;
				case "right": Keyboard = Keyboard.Set(ScrollDirection.Right, (e.Event == KeyInputEvent.Down)); return true;
			}
			return base.HandleKeyPress(e);
		}

		public override void Tick()
		{
			Edge = ScrollDirection.None;
			if (Game.Settings.Game.ViewportEdgeScroll && Game.HasInputFocus && Ui.MouseOverWidget == this)
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

			base.Tick();
		}
	}
}