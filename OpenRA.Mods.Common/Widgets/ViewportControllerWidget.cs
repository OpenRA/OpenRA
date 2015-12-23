#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ViewportControllerWidget : Widget, IWorldTooltipInfo
	{
		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;
		readonly Dictionary<int, ResourceType> resources;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public string Label { get; private set; }
		public string Extra { get; private set; }
		public IPlayerSummary Owner { get; private set; }
		public bool ShowOwner { get; private set; }

		public int EdgeScrollThreshold = 15;
		public int EdgeCornerScrollThreshold = 35;

		int2? joystickScrollStart, joystickScrollEnd;

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

		static readonly Dictionary<ScrollDirection, string> JoystickCursors = new Dictionary<ScrollDirection, string>
		{
			{ ScrollDirection.Up | ScrollDirection.Left, "joystick-tl-blocked" },
			{ ScrollDirection.Up | ScrollDirection.Right, "joystick-tr-blocked" },
			{ ScrollDirection.Down | ScrollDirection.Left, "joystick-bl-blocked" },
			{ ScrollDirection.Down | ScrollDirection.Right, "joystick-br-blocked" },
			{ ScrollDirection.Up, "joystick-t-blocked" },
			{ ScrollDirection.Down, "joystick-b-blocked" },
			{ ScrollDirection.Left, "joystick-l-blocked" },
			{ ScrollDirection.Right, "joystick-r-blocked" },
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
		WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public ViewportControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			resources = world.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() { { "info", this as IWorldTooltipInfo } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		public override void Draw()
		{
			if (IsJoystickScrolling)
			{
				// Base the JoystickScrolling speed on the Scroll Speed slider
				var rate = 0.01f * Game.Settings.Game.ViewportEdgeScrollStep;

				var scroll = (joystickScrollEnd.Value - joystickScrollStart.Value).ToFloat2() * rate;
				worldRenderer.Viewport.Scroll(scroll, false);
			}

			UpdateMouseover();
			base.Draw();
		}

		public void UpdateMouseover()
		{
			Label = null;
			Extra = null;
			Owner = null;
			ShowOwner = false;
			IEnumerable<IProvideTooltipInfo> extras = null;

			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.Contains(cell))
				return;

			if (world.ShroudObscures(cell))
			{
				// Case unexplored terrain
				Label = "Unexplored Terrain";
				return;
			}

			ITooltipInfo tooltipInfo = null;
			Player owner = null;
			var underCursor = world.ScreenMap.ActorsAt(worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos))
				.Where(a => !world.FogObscures(a) && a.Info.HasTraitInfo<ITooltipInfo>())
				.WithHighestSelectionPriority();

			if (underCursor != null)
			{
				// Case actor
				extras = underCursor.TraitsImplementing<IProvideTooltipInfo>();
				var tooltip = underCursor.TraitsImplementing<ITooltip>().FirstOrDefault();
				if (tooltip != null)
				{
					tooltipInfo = tooltip.TooltipInfo;
					owner = tooltip.Owner;
				}
			}
			else
			{
				var frozen = world.ScreenMap.FrozenActorsAt(world.RenderPlayer, worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos))
					.Where(a => a.TooltipInfo != null && a.IsValid)
					.WithHighestSelectionPriority();

				if (frozen != null)
				{
					// Case frozen actor
					var actor = frozen.Actor;
					if (actor != null && actor.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(actor, world.RenderPlayer)))
						return;

					var tooltip = actor.TraitsImplementing<ITooltip>().FirstOrDefault();
					if (tooltip != null)
						tooltipInfo = tooltip.TooltipInfo;
					owner = frozen.Owner;
					if (actor != null)
						extras = actor.TraitsImplementing<IProvideTooltipInfo>();
				}
				else
				{
					// Case resource type or nothing
					ResourceType type;
					var mapResources = world.Map.MapResources.Value;
					if (mapResources.Contains(cell) && resources.TryGetValue(mapResources[cell].Type, out type))
						Label = type.Info.Name;

					return;
				}
			}

			var stance = owner == null || world.RenderPlayer == null ? Stance.None : owner.Stances[world.RenderPlayer];
			Label = tooltipInfo.TooltipForPlayerStance(stance);

			Owner = owner;
			ShowOwner = owner != null && !owner.NonCombatant && tooltipInfo.IsOwnerRowVisible;

			if (extras != null && extras.Any())
				Extra = string.Join("\n", extras.Where(i => i.IsTooltipVisible(world.LocalPlayer)));
		}

		public override string GetCursor(int2 pos)
		{
			if (!IsJoystickScrolling &&
			    (!Game.Settings.Game.ViewportEdgeScroll || Ui.MouseOverWidget != this))
				return null;

			var blockedDirections = worldRenderer.Viewport.GetBlockedDirections();

			if (IsJoystickScrolling)
			{
				foreach (var dir in JoystickCursors)
					if (blockedDirections.Includes(dir.Key))
						return dir.Value;
				return "joystick-all";
			}

			foreach (var dir in ScrollCursors)
				if (edgeDirections.Includes(dir.Key))
					return dir.Value + (blockedDirections.Includes(dir.Key) ? "-blocked" : "");

			return null;
		}

		bool IsJoystickScrolling
		{
			get
			{
				return joystickScrollStart.HasValue && joystickScrollEnd.HasValue &&
					(joystickScrollStart.Value - joystickScrollEnd.Value).Length > Game.Settings.Game.JoystickScrollDeadzone;
			}
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var scrolltype = Game.Settings.Game.MouseScroll;
			if (scrolltype == MouseScrollType.Disabled)
				return false;

			if (scrolltype == MouseScrollType.Standard || scrolltype == MouseScrollType.Inverted)
			{
				if (mi.Event == MouseInputEvent.Move &&
					(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
				{
					var d = scrolltype == MouseScrollType.Inverted ? -1 : 1;
					worldRenderer.Viewport.Scroll((Viewport.LastMousePos - mi.Location) * d, false);
					return true;
				}
			}

			// Tiberian Sun style right-click-and-drag scrolling
			if (scrolltype == MouseScrollType.Joystick)
			{
				if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
				{
					if (!TakeMouseFocus(mi))
						return false;
					joystickScrollStart = mi.Location;
				}

				if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
				{
					var wasJoystickScrolling = IsJoystickScrolling;

					joystickScrollStart = joystickScrollEnd = null;
					YieldMouseFocus(mi);

					if (wasJoystickScrolling)
						return true;
				}

				if (mi.Event == MouseInputEvent.Move && mi.Button == MouseButton.Right && joystickScrollStart.HasValue)
				{
					joystickScrollEnd = mi.Location;
				}
			}

			return false;
		}

		public override bool YieldKeyboardFocus()
		{
			keyboardDirections = ScrollDirection.None;
			return base.YieldKeyboardFocus();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			var key = Hotkey.FromKeyInput(e);
			var ks = Game.Settings.Keys;

			if (key == ks.MapScrollUp)
			{
				keyboardDirections = keyboardDirections.Set(ScrollDirection.Up, e.Event == KeyInputEvent.Down);
				return true;
			}

			if (key == ks.MapScrollDown)
			{
				keyboardDirections = keyboardDirections.Set(ScrollDirection.Down, e.Event == KeyInputEvent.Down);
				return true;
			}

			if (key == ks.MapScrollLeft)
			{
				keyboardDirections = keyboardDirections.Set(ScrollDirection.Left, e.Event == KeyInputEvent.Down);
				return true;
			}

			if (key == ks.MapScrollRight)
			{
				keyboardDirections = keyboardDirections.Set(ScrollDirection.Right, e.Event == KeyInputEvent.Down);
				return true;
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

				worldRenderer.Viewport.Scroll(scroll, false);
			}
		}

		ScrollDirection CheckForDirections()
		{
			var directions = ScrollDirection.None;
			if (Viewport.LastMousePos.X < EdgeScrollThreshold)
				directions |= ScrollDirection.Left;
			if (Viewport.LastMousePos.Y < EdgeScrollThreshold)
				directions |= ScrollDirection.Up;
			if (Viewport.LastMousePos.X >= Game.Renderer.Resolution.Width - EdgeScrollThreshold)
				directions |= ScrollDirection.Right;
			if (Viewport.LastMousePos.Y >= Game.Renderer.Resolution.Height - EdgeScrollThreshold)
				directions |= ScrollDirection.Down;

			return directions;
		}
	}
}
