#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum WorldTooltipType { None, Unexplored, Actor, FrozenActor }

	public class ViewportControllerWidget : Widget
	{
		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public WorldTooltipType TooltipType { get; private set; }
		public ITooltip ActorTooltip { get; private set; }
		public IProvideTooltipInfo[] ActorTooltipExtra { get; private set; }
		public FrozenActor FrozenActorTooltip { get; private set; }

		public int EdgeScrollThreshold = 15;
		public int EdgeCornerScrollThreshold = 35;

		int2? joystickScrollStart, joystickScrollEnd;
		int2? standardScrollStart;
		bool isStandardScrolling;

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
		WPos?[] viewPortBookmarkSlots = new WPos?[4];

		void SaveBookmark(int index, WPos position)
		{
			viewPortBookmarkSlots[index] = position;
		}

		void SaveCurrentPositionToBookmark(int index)
		{
			SaveBookmark(index, worldRenderer.Viewport.CenterPosition);
		}

		WPos? JumpToBookmark(int index)
		{
			return viewPortBookmarkSlots[index];
		}

		void JumpToSavedBookmark(int index)
		{
			var bookmark = JumpToBookmark(index);
			if (bookmark != null)
				worldRenderer.Viewport.Center((WPos)bookmark);
		}

		[ObjectCreator.UseCtor]
		public ViewportControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(TooltipTemplate,
				new WidgetArgs() { { "world", world }, { "viewport", this } });
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.RemoveTooltip();
		}

		int lastScrollTime = 0;
		public override void Draw()
		{
			if (IsJoystickScrolling)
			{
				// Base the JoystickScrolling speed on the Scroll Speed slider
				var rate = 0.01f * Game.Settings.Game.ViewportEdgeScrollStep;

				var scroll = (joystickScrollEnd.Value - joystickScrollStart.Value).ToFloat2() * rate;
				worldRenderer.Viewport.Scroll(scroll, false);
			}
			else
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

					// Scroll rate is defined for a 40ms interval
					var deltaScale = Math.Min(Game.RunTime - lastScrollTime, 25f);

					var length = Math.Max(1, scroll.Length);
					scroll *= (deltaScale / (25 * length)) * Game.Settings.Game.ViewportEdgeScrollStep;

					worldRenderer.Viewport.Scroll(scroll, false);
					lastScrollTime = Game.RunTime;
				}
			}

			UpdateMouseover();
			base.Draw();
		}

		public void UpdateMouseover()
		{
			TooltipType = WorldTooltipType.None;
			ActorTooltipExtra = null;
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.Contains(cell))
				return;

			if (world.ShroudObscures(cell))
			{
				TooltipType = WorldTooltipType.Unexplored;
				return;
			}

			var worldPixel = worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos);
			var underCursor = world.ScreenMap.ActorsAt(worldPixel)
				.Where(a => !world.FogObscures(a) && a.Info.HasTraitInfo<ITooltipInfo>())
				.WithHighestSelectionPriority(worldPixel);

			if (underCursor != null)
			{
				ActorTooltip = underCursor.TraitsImplementing<ITooltip>().First();
				ActorTooltipExtra = underCursor.TraitsImplementing<IProvideTooltipInfo>().ToArray();
				TooltipType = WorldTooltipType.Actor;
				return;
			}

			var frozen = world.ScreenMap.FrozenActorsAt(world.RenderPlayer, worldPixel)
				.Where(a => a.TooltipInfo != null && a.IsValid)
				.WithHighestSelectionPriority(worldPixel);

			if (frozen != null)
			{
				var actor = frozen.Actor;
				if (actor != null && actor.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(actor, world.RenderPlayer)))
					return;

				FrozenActorTooltip = frozen;
				if (frozen.Actor != null)
					ActorTooltipExtra = frozen.Actor.TraitsImplementing<IProvideTooltipInfo>().ToArray();
				TooltipType = WorldTooltipType.FrozenActor;
			}
		}

		public override string GetCursor(int2 pos)
		{
			if (!(IsJoystickScrolling || isStandardScrolling) &&
				(!Game.Settings.Game.ViewportEdgeScroll || Ui.MouseOverWidget != this))
				return null;

			var blockedDirections = worldRenderer.Viewport.GetBlockedDirections();

			if (IsJoystickScrolling || isStandardScrolling)
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
					(joystickScrollStart.Value - joystickScrollEnd.Value).Length > Game.Settings.Game.MouseScrollDeadzone;
			}
		}

		bool IsZoomAllowed(float zoom)
		{
			return world.IsGameOver || zoom >= 1.0f || world.IsReplay || world.LocalPlayer == null || world.LocalPlayer.Spectating;
		}

		void Zoom(int direction)
		{
			var zoomSteps = worldRenderer.Viewport.AvailableZoomSteps;
			var currentZoom = worldRenderer.Viewport.Zoom;
			var nextIndex = zoomSteps.IndexOf(currentZoom);

			if (direction < 0)
				nextIndex++;
			else
				nextIndex--;

			if (nextIndex < 0 || nextIndex >= zoomSteps.Count())
				return;

			var zoom = zoomSteps.ElementAt(nextIndex);
			if (!IsZoomAllowed(zoom))
				return;

			worldRenderer.Viewport.Zoom = zoom;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll &&
				Game.Settings.Game.AllowZoom && mi.Modifiers.HasModifier(Game.Settings.Game.ZoomModifier))
			{
				Zoom(mi.ScrollDelta);
				return true;
			}

			var scrollType = MouseScrollType.Disabled;

			if (mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right))
				scrollType = Game.Settings.Game.MiddleMouseScroll;
			else if (mi.Button == MouseButton.Right)
				scrollType = Game.Settings.Game.RightMouseScroll;

			if (scrollType == MouseScrollType.Disabled)
				return false;

			if (scrollType == MouseScrollType.Standard || scrollType == MouseScrollType.Inverted)
			{
				if (mi.Event == MouseInputEvent.Down && !isStandardScrolling)
					standardScrollStart = mi.Location;
				else if (mi.Event == MouseInputEvent.Move && (isStandardScrolling ||
					(standardScrollStart.HasValue && ((standardScrollStart.Value - mi.Location).Length > Game.Settings.Game.MouseScrollDeadzone))))
				{
					isStandardScrolling = true;
					var d = scrollType == MouseScrollType.Inverted ? -1 : 1;
					worldRenderer.Viewport.Scroll((Viewport.LastMousePos - mi.Location) * d, false);
					return true;
				}
				else if (mi.Event == MouseInputEvent.Up)
				{
					var wasStandardScrolling = isStandardScrolling;
					isStandardScrolling = false;
					standardScrollStart = null;

					if (wasStandardScrolling)
						return true;
				}
			}

			// Tiberian Sun style click-and-drag scrolling
			if (scrollType == MouseScrollType.Joystick)
			{
				if (mi.Event == MouseInputEvent.Down)
				{
					if (!TakeMouseFocus(mi))
						return false;
					joystickScrollStart = mi.Location;
				}

				if (mi.Event == MouseInputEvent.Up)
				{
					var wasJoystickScrolling = IsJoystickScrolling;

					joystickScrollStart = joystickScrollEnd = null;
					YieldMouseFocus(mi);

					if (wasJoystickScrolling)
						return true;
				}

				if (mi.Event == MouseInputEvent.Move && joystickScrollStart.HasValue)
					joystickScrollEnd = mi.Location;
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

			if (key == ks.MapPushTop)
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.Viewport.CenterPosition.X, 0, 0));
				return false;
			}

			if (key == ks.MapPushBottom)
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.Viewport.CenterPosition.X, worldRenderer.World.Map.ProjectedBottomRight.Y, 0));
				return false;
			}

			if (key == ks.MapPushLeftEdge)
			{
				worldRenderer.Viewport.Center(new WPos(0, worldRenderer.Viewport.CenterPosition.Y, 0));
				return false;
			}

			if (key == ks.MapPushRightEdge)
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.World.Map.ProjectedBottomRight.X, worldRenderer.Viewport.CenterPosition.Y, 0));
			}

			if (key == ks.ViewPortBookmarkSaveSlot1)
			{
				SaveCurrentPositionToBookmark(0);
				return false;
			}

			if (key == ks.ViewPortBookmarkSaveSlot2)
			{
				SaveCurrentPositionToBookmark(1);
				return false;
			}

			if (key == ks.ViewPortBookmarkSaveSlot3)
			{
				SaveCurrentPositionToBookmark(2);
				return false;
			}

			if (key == ks.ViewPortBookmarkSaveSlot4)
			{
				SaveCurrentPositionToBookmark(3);
				return false;
			}

			if (key == ks.ViewPortBookmarkUseSlot1)
			{
				JumpToSavedBookmark(0);
				return false;
			}

			if (key == ks.ViewPortBookmarkUseSlot2)
			{
				JumpToSavedBookmark(1);
				return false;
			}

			if (key == ks.ViewPortBookmarkUseSlot3)
			{
				JumpToSavedBookmark(2);
				return false;
			}

			if (key == ks.ViewPortBookmarkUseSlot4)
			{
				JumpToSavedBookmark(3);
				return false;
			}

			return false;
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
