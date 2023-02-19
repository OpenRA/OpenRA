#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public enum WorldTooltipType { None, Unexplored, Actor, FrozenActor, Resource }

	public class ViewportControllerWidget : Widget
	{
		readonly ModData modData;
		readonly IEnumerable<IResourceRenderer> resourceRenderers;

		public readonly HotkeyReference ZoomInKey = new HotkeyReference();
		public readonly HotkeyReference ZoomOutKey = new HotkeyReference();

		public readonly HotkeyReference ScrollUpKey = new HotkeyReference();
		public readonly HotkeyReference ScrollDownKey = new HotkeyReference();
		public readonly HotkeyReference ScrollLeftKey = new HotkeyReference();
		public readonly HotkeyReference ScrollRightKey = new HotkeyReference();

		public readonly HotkeyReference JumpToTopEdgeKey = new HotkeyReference();
		public readonly HotkeyReference JumpToBottomEdgeKey = new HotkeyReference();
		public readonly HotkeyReference JumpToLeftEdgeKey = new HotkeyReference();
		public readonly HotkeyReference JumpToRightEdgeKey = new HotkeyReference();

		// Note: LinterHotkeyNames assumes that these are disabled by default
		public readonly string BookmarkSaveKeyPrefix = null;
		public readonly string BookmarkRestoreKeyPrefix = null;
		public readonly int BookmarkKeyCount = 0;

		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;

		public WorldTooltipType TooltipType { get; private set; }
		public ITooltip ActorTooltip { get; private set; }
		public IProvideTooltipInfo[] ActorTooltipExtra { get; private set; }
		public FrozenActor FrozenActorTooltip { get; private set; }
		public string ResourceTooltip { get; private set; }

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

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		int2? joystickScrollStart, joystickScrollEnd;
		int2? standardScrollStart;
		bool isStandardScrolling;

		ScrollDirection keyboardDirections;
		ScrollDirection edgeDirections;

		HotkeyReference[] saveBookmarkHotkeys;
		HotkeyReference[] restoreBookmarkHotkeys;
		WPos?[] bookmarkPositions;

		[CustomLintableHotkeyNames]
		public static IEnumerable<string> LinterHotkeyNames(MiniYamlNode widgetNode, Action<string> emitError)
		{
			var savePrefix = "";
			var savePrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "BookmarkSaveKeyPrefix");
			if (savePrefixNode != null)
				savePrefix = savePrefixNode.Value.Value;

			var restorePrefix = "";
			var restorePrefixNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "BookmarkRestoreKeyPrefix");
			if (restorePrefixNode != null)
				restorePrefix = restorePrefixNode.Value.Value;

			var count = 0;
			var countNode = widgetNode.Value.Nodes.FirstOrDefault(n => n.Key == "BookmarkKeyCount");
			if (countNode != null)
				count = FieldLoader.GetValue<int>("BookmarkKeyCount", countNode.Value.Value);

			if (count == 0)
				yield break;

			if (string.IsNullOrEmpty(savePrefix))
				emitError($"{widgetNode.Location} must define BookmarkSaveKeyPrefix if BookmarkKeyCount > 0.");

			if (string.IsNullOrEmpty(restorePrefix))
				emitError($"{widgetNode.Location} must define BookmarkRestoreKeyPrefix if BookmarkKeyCount > 0.");

			for (var i = 0; i < count; i++)
			{
				var suffix = (i + 1).ToString("D2");
				yield return savePrefix + suffix;
				yield return restorePrefix + suffix;
			}
		}

		[ObjectCreator.UseCtor]
		public ViewportControllerWidget(ModData modData, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			saveBookmarkHotkeys = Exts.MakeArray(BookmarkKeyCount,
				i => modData.Hotkeys[BookmarkSaveKeyPrefix + (i + 1).ToString("D2")]);

			restoreBookmarkHotkeys = Exts.MakeArray(BookmarkKeyCount,
				i => modData.Hotkeys[BookmarkRestoreKeyPrefix + (i + 1).ToString("D2")]);

			bookmarkPositions = new WPos?[BookmarkKeyCount];
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

		long lastScrollTime = 0;
		public override void Draw()
		{
			if (IsJoystickScrolling)
			{
				// Base the JoystickScrolling speed on the Scroll Speed slider
				var rate = 0.01f * Game.Settings.Game.ViewportEdgeScrollStep;

				var scroll = (joystickScrollEnd.Value - joystickScrollStart.Value).ToFloat2() * rate;
				worldRenderer.Viewport.Scroll(scroll, false);
			}
			else if (!isStandardScrolling)
			{
				edgeDirections = ScrollDirection.None;
				if (Game.Settings.Game.ViewportEdgeScroll && Game.Renderer.WindowHasInputFocus)
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
					scroll *= deltaScale / (25 * length) * Game.Settings.Game.ViewportEdgeScrollStep;

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
			var modifiers = Game.GetModifierKeys();
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!world.Map.Contains(cell))
				return;

			if (world.ShroudObscures(cell))
			{
				TooltipType = WorldTooltipType.Unexplored;
				return;
			}

			var worldPixel = worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos);
			var underCursor = world.ScreenMap.ActorsAtMouse(worldPixel)
				.Where(a => a.Actor.Info.HasTraitInfo<ITooltipInfo>() && !world.FogObscures(a.Actor))
				.WithHighestSelectionPriority(worldPixel, modifiers);

			if (underCursor != null)
			{
				ActorTooltip = underCursor.TraitsImplementing<ITooltip>().FirstEnabledTraitOrDefault();
				if (ActorTooltip != null)
				{
					ActorTooltipExtra = underCursor.TraitsImplementing<IProvideTooltipInfo>().ToArray();
					TooltipType = WorldTooltipType.Actor;
				}

				return;
			}

			var frozen = world.ScreenMap.FrozenActorsAtMouse(world.RenderPlayer, worldPixel)
				.Where(a => a.TooltipInfo != null && a.IsValid && a.Visible && !a.Hidden)
				.WithHighestSelectionPriority(worldPixel, modifiers);

			if (frozen != null)
			{
				FrozenActorTooltip = frozen;

				// HACK: This leaks the tooltip state through the fog
				// This will cause issues for any downstream mods that use IProvideTooltipInfo on enemy actors
				if (frozen.Actor != null)
					ActorTooltipExtra = frozen.Actor.TraitsImplementing<IProvideTooltipInfo>().ToArray();

				TooltipType = WorldTooltipType.FrozenActor;
				return;
			}

			foreach (var resourceRenderer in resourceRenderers)
			{
				var resourceTooltip = resourceRenderer.GetRenderedResourceTooltip(cell);
				if (resourceTooltip != null)
				{
					TooltipType = WorldTooltipType.Resource;
					ResourceTooltip = resourceTooltip;
					break;
				}
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

		bool IsJoystickScrolling =>
			joystickScrollStart.HasValue && joystickScrollEnd.HasValue &&
			(joystickScrollStart.Value - joystickScrollEnd.Value).Length > Game.Settings.Game.MouseScrollDeadzone;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll && mi.Modifiers.HasModifier(Game.Settings.Game.ZoomModifier))
			{
				worldRenderer.Viewport.AdjustZoom(mi.Delta.Y * Game.Settings.Game.ZoomSpeed, mi.Location);
				return true;
			}

			var gs = Game.Settings.Game;
			var scrollButton = gs.UseClassicMouseStyle ^ gs.UseAlternateScrollButton ? MouseButton.Right : MouseButton.Middle;
			var scrollType = mi.Button.HasFlag(scrollButton) ? gs.MouseScroll : MouseScrollType.Disabled;

			if (scrollType == MouseScrollType.Disabled)
				return IsJoystickScrolling || isStandardScrolling;

			if (scrollType == MouseScrollType.Standard || scrollType == MouseScrollType.Inverted)
			{
				if (mi.Event == MouseInputEvent.Down && !isStandardScrolling)
				{
					if (!TakeMouseFocus(mi))
						return false;

					standardScrollStart = mi.Location;
				}
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
					YieldMouseFocus(mi);

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

				if (mi.Event == MouseInputEvent.Move)
				{
					if (!joystickScrollStart.HasValue)
						joystickScrollStart = mi.Location;

					joystickScrollEnd = mi.Location;
				}
			}

			return IsJoystickScrolling || isStandardScrolling;
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			joystickScrollStart = joystickScrollEnd = null;
			return base.YieldMouseFocus(mi);
		}

		public override bool YieldKeyboardFocus()
		{
			keyboardDirections = ScrollDirection.None;
			return base.YieldKeyboardFocus();
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			var key = Hotkey.FromKeyInput(e);

			Func<HotkeyReference, ScrollDirection, bool> handleMapScrollKey = (hotkey, scrollDirection) =>
			{
				var isHotkey = false;
				var keyValue = hotkey.GetValue();
				if (key.Key == keyValue.Key)
				{
					isHotkey = key == keyValue;
					keyboardDirections = keyboardDirections.Set(scrollDirection, e.Event == KeyInputEvent.Down && (isHotkey || keyValue.Modifiers == Modifiers.None));
				}

				return isHotkey;
			};

			if (handleMapScrollKey(ScrollUpKey, ScrollDirection.Up) || handleMapScrollKey(ScrollDownKey, ScrollDirection.Down)
				|| handleMapScrollKey(ScrollLeftKey, ScrollDirection.Left) || handleMapScrollKey(ScrollRightKey, ScrollDirection.Right))
				return true;

			if (e.Event != KeyInputEvent.Down)
				return false;

			if (ZoomInKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.AdjustZoom(0.25f);
				return true;
			}

			if (ZoomOutKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.AdjustZoom(-0.25f);
				return true;
			}

			if (JumpToTopEdgeKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.Viewport.CenterPosition.X, 0, 0));
				return true;
			}

			if (JumpToBottomEdgeKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.Viewport.CenterPosition.X, worldRenderer.World.Map.ProjectedBottomRight.Y, 0));
				return true;
			}

			if (JumpToLeftEdgeKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.Center(new WPos(0, worldRenderer.Viewport.CenterPosition.Y, 0));
				return true;
			}

			if (JumpToRightEdgeKey.IsActivatedBy(e))
			{
				worldRenderer.Viewport.Center(new WPos(worldRenderer.World.Map.ProjectedBottomRight.X, worldRenderer.Viewport.CenterPosition.Y, 0));
				return true;
			}

			for (var i = 0; i < saveBookmarkHotkeys.Length; i++)
			{
				if (saveBookmarkHotkeys[i].IsActivatedBy(e))
				{
					bookmarkPositions[i] = worldRenderer.Viewport.CenterPosition;
					return true;
				}
			}

			for (var i = 0; i < restoreBookmarkHotkeys.Length; i++)
			{
				if (restoreBookmarkHotkeys[i].IsActivatedBy(e))
				{
					var bookmark = bookmarkPositions[i];
					if (bookmark.HasValue)
					{
						worldRenderer.Viewport.Center(bookmark.Value);
						return true;
					}
				}
			}

			return world.OrderGenerator.HandleKeyPress(e);
		}

		ScrollDirection CheckForDirections()
		{
			var margin = Game.Settings.Game.ViewportEdgeScrollMargin;
			var directions = ScrollDirection.None;
			if (Viewport.LastMousePos.X < margin)
				directions |= ScrollDirection.Left;
			if (Viewport.LastMousePos.Y < margin)
				directions |= ScrollDirection.Up;
			if (Viewport.LastMousePos.X >= Game.Renderer.Resolution.Width - margin)
				directions |= ScrollDirection.Right;
			if (Viewport.LastMousePos.Y >= Game.Renderer.Resolution.Height - margin)
				directions |= ScrollDirection.Down;

			return directions;
		}
	}
}
