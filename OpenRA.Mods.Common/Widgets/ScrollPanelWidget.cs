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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public interface ILayout
	{
		void AdjustChild(Widget w);
		void AdjustChildren();
	}

	public enum ScrollPanelAlign
	{
		Bottom,
		Top
	}

	public enum ScrollBar
	{
		Left,
		Right,
		Hidden
	}

	public class ScrollPanelWidget : Widget, IKeyboardScrollable
	{
		readonly Ruleset modRules;
		public int ScrollbarWidth = 24;

		ScrollItemWidget keyboardFocusedItem;
		public bool HasKeyboardFocusedItem => keyboardFocusedItem != null;

		// Called when Enter or Space is pressed. Return true if the key was handled.
		// If null or returns false, the focused item's OnClick will be invoked.
		public Func<bool> OnEnterKey;

		// Called when Escape is pressed. Return true if the key was handled.
		public Func<bool> OnEscapeKey;

		// Called when Delete is pressed. Return true if the key was handled.
		public Func<bool> OnDeleteKey;

		// Called when the keyboard-focused item changes (e.g., via UP/DOWN arrow keys).
		// The parameter is the newly focused item, or null if focus is cleared.
		public Action<ScrollItemWidget> OnKeyboardFocusChanged;
		public int BorderWidth = 1;
		public int TopBottomSpacing = 2;
		public int ItemSpacing = 0;
		public int ButtonDepth = ChromeMetrics.Get<int>("ButtonDepth");
		public string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");
		public string Background = "scrollpanel-bg";
		public string ScrollBarBackground = "scrollpanel-bg";
		public string Button = "scrollpanel-button";
		public string Decorations = "scrollpanel-decorations";
		public readonly string DecorationScrollUp = "up";
		public readonly string DecorationScrollDown = "down";
		readonly CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getUpArrowImage;
		readonly CachedTransform<(bool Disabled, bool Pressed, bool Hover, bool Focused, bool Highlighted), Sprite> getDownArrowImage;
		public int ContentHeight;
		public ILayout Layout;
		public int MinimumThumbSize = 10;
		public ScrollPanelAlign Align = ScrollPanelAlign.Top;
		public ScrollBar ScrollBar = ScrollBar.Right;
		public bool CollapseHiddenChildren;

		// Fraction of the remaining scroll-delta to move in 40ms
		public float SmoothScrollSpeed = 0.333f;

		protected bool upPressed;
		protected bool downPressed;
		protected bool upDisabled;
		protected bool downDisabled;
		protected bool thumbPressed;
		protected Rectangle upButtonRect;
		protected Rectangle downButtonRect;
		protected Rectangle backgroundRect;
		protected Rectangle scrollbarRect;
		protected Rectangle thumbRect;

		// The target value is the list offset we're trying to reach
		float targetListOffset;

		// The current value is the actual list offset at the moment
		float currentListOffset;

		// The Game.Runtime value when UpdateSmoothScrolling was last called
		// Used for calculating the per-frame smooth-scrolling delta
		long lastSmoothScrollTime = 0;

		// Setting "smooth" to true will only update the target list offset.
		// Setting "smooth" to false will also set the current list offset,
		// i.e. it will scroll immediately.
		//
		// For example, scrolling with the mouse wheel will use smooth
		// scrolling to give a nice visual effect that makes it easier
		// for the user to follow. Dragging the scrollbar's thumb, however,
		// will scroll to the desired position immediately.
		protected void SetListOffset(float value, bool smooth)
		{
			targetListOffset = value;
			if (!smooth)
			{
				var oldListOffset = currentListOffset;
				currentListOffset = value;

				// Update mouseover
				if (oldListOffset != currentListOffset)
					Ui.ResetTooltips();
			}
		}

		[ObjectCreator.UseCtor]
		public ScrollPanelWidget(ModData modData)
		{
			modRules = modData.DefaultRules;

			Layout = new ListLayout(this);

			getUpArrowImage = WidgetUtils.GetCachedStatefulImage(Decorations, DecorationScrollUp);
			getDownArrowImage = WidgetUtils.GetCachedStatefulImage(Decorations, DecorationScrollDown);

			// ScrollPanelWidget can be focusable by TAB when it contains selectable items.
			// When it receives TAB focus, it takes KeyboardFocus and allows arrow-key navigation of items.
			IsFocusable = true;
		}

		// Override to check if this panel has content before allowing TAB focus
		public override bool IsTabNavigable()
		{
			if (!base.IsTabNavigable())
				return false;

			// Allow TAB focus if there are selectable items in the list
			if (GetSelectableItems().Count > 0)
				return true;

			// Also allow TAB focus if there's scrollable content (like Credits)
			// This enables keyboard scrolling with PAGE UP/DOWN, HOME, END
			if (ContentHeight > Bounds.Height)
				return true;

			// Also allow TAB focus if there are focusable widgets (like checkboxes in Options)
			if (GetFocusableWidgets().Count > 0)
				return true;

			return false;
		}

		public override bool TakeKeyboardFocus()
		{
			if (!base.TakeKeyboardFocus())
				return false;

			// Initialize keyboard focus to the currently selected item (if any)
			// so that pressing ENTER/SPACE immediately confirms the current selection.
			InitializeKeyboardFocusToSelectedItem();
			return true;
		}

		// Called when this panel gains TAB focus
		public override void OnTabFocusGained()
		{
			base.OnTabFocusGained();

			// Take KeyboardFocus to enable arrow-key navigation
			TakeKeyboardFocus();

			// Initialize focus to the first selectable item if none is selected
			if (keyboardFocusedItem == null)
			{
				var selectableItems = GetSelectableItems();
				if (selectableItems.Count > 0)
					SetKeyboardFocus(selectableItems[0]);
			}
		}

		// Called when this panel loses TAB focus
		public override void OnTabFocusLost()
		{
			base.OnTabFocusLost();

			// Clear keyboard focus when losing TAB focus
			ClearKeyboardFocusedItem();
		}

		// Handle ENTER/SPACE activation when this panel has TAB focus
		public override bool OnTabFocusActivate(KeyInput e)
		{
			// Activate the currently keyboard-focused item
			return ActivateFocusedItem();
		}

		// Handle arrow keys when this panel has TAB focus
		public override bool OnTabFocusKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			// Check what kind of content we have
			var selectableItems = GetSelectableItems();
			var hasScrollItems = selectableItems.Count > 0;

			// If we have ScrollItemWidgets, use item-based navigation
			if (hasScrollItems)
			{
				switch (e.Key)
				{
					case Keycode.UP:
						return FocusAdjacentItem(forward: false);

					case Keycode.DOWN:
						return FocusAdjacentItem(forward: true);

					case Keycode.PAGEUP:
						return FocusItemByPage(forward: false);

					case Keycode.PAGEDOWN:
						return FocusItemByPage(forward: true);

					case Keycode.HOME:
						return FocusFirstItem();

					case Keycode.END:
						return FocusLastItem();
				}

				return false;
			}

			// Check for focusable widgets (like checkboxes in Options)
			var focusableWidgets = GetFocusableWidgets();
			if (focusableWidgets.Count > 0)
				return HandleFocusableWidgetKeyPress(e);

			// No items and no focusable widgets - use pure scroll navigation (like Credits)
			return HandlePureScrollKeyPress(e);
		}

		void InitializeKeyboardFocusToSelectedItem()
		{
			var selectableItems = GetSelectableItems();
			var selectedItem = selectableItems.Find(item => item.IsSelected());
			if (selectedItem != null)
				SetKeyboardFocus(selectedItem);
		}

		public override void RemoveChildren()
		{
			ContentHeight = 0;
			base.RemoveChildren();
			Scroll(0);
		}

		public override void AddChild(Widget child)
		{
			// Initial setup of margins/height
			Layout.AdjustChild(child);
			base.AddChild(child);
		}

		public override void RemoveChild(Widget child)
		{
			base.RemoveChild(child);
			Layout.AdjustChildren();
			Scroll(0);
		}

		public void ReplaceChild(Widget oldChild, Widget newChild)
		{
			oldChild.Removed();
			newChild.Parent = this;
			Children[Children.IndexOf(oldChild)] = newChild;
			Layout.AdjustChildren();
			Scroll(0);
		}

		public override void DrawOuter()
		{
			if (!IsVisible())
				return;

			UpdateSmoothScrolling();

			var rb = RenderBounds;

			var scrollbarHeight = rb.Height - 2 * ScrollbarWidth;

			// Scroll thumb is only visible if the content does not fit within the panel bounds
			var thumbHeight = 0;
			var thumbOrigin = rb.Y + ScrollbarWidth;
			if (ContentHeight > rb.Height)
			{
				thumbHeight = Math.Max(MinimumThumbSize, scrollbarHeight * rb.Height / ContentHeight);
				thumbOrigin += (int)((scrollbarHeight - thumbHeight) * currentListOffset / (rb.Height - ContentHeight));
			}

			switch (ScrollBar)
			{
				case ScrollBar.Left:
					backgroundRect = new Rectangle(rb.X + ScrollbarWidth, rb.Y, rb.Width + 1, rb.Height);
					upButtonRect = new Rectangle(rb.X, rb.Y, ScrollbarWidth, ScrollbarWidth);
					downButtonRect = new Rectangle(rb.X, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
					scrollbarRect = new Rectangle(rb.X, rb.Y + ScrollbarWidth - 1, ScrollbarWidth, scrollbarHeight + 2);
					thumbRect = new Rectangle(rb.X, thumbOrigin, ScrollbarWidth, thumbHeight);
					break;
				case ScrollBar.Right:
					backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width - ScrollbarWidth + 1, rb.Height);
					upButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y, ScrollbarWidth, ScrollbarWidth);
					downButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
					scrollbarRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y + ScrollbarWidth - 1, ScrollbarWidth, scrollbarHeight + 2);
					thumbRect = new Rectangle(rb.Right - ScrollbarWidth, thumbOrigin, ScrollbarWidth, thumbHeight);
					break;
				case ScrollBar.Hidden:
					backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width + 1, rb.Height);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			WidgetUtils.DrawPanel(Background, backgroundRect);

			if (ScrollBar != ScrollBar.Hidden)
			{
				var upHover = Ui.MouseOverWidget == this && upButtonRect.Contains(Viewport.LastMousePos);
				upDisabled = thumbHeight == 0 || currentListOffset >= 0;

				var downHover = Ui.MouseOverWidget == this && downButtonRect.Contains(Viewport.LastMousePos);
				downDisabled = thumbHeight == 0 || currentListOffset <= Bounds.Height - ContentHeight;

				var thumbHover = Ui.MouseOverWidget == this && thumbRect.Contains(Viewport.LastMousePos);
				WidgetUtils.DrawPanel(ScrollBarBackground, scrollbarRect);
				ButtonWidget.DrawBackground(Button, upButtonRect, upDisabled, upPressed, upHover, false);
				ButtonWidget.DrawBackground(Button, downButtonRect, downDisabled, downPressed, downHover, false);

				if (thumbHeight > 0)
					ButtonWidget.DrawBackground(Button, thumbRect, false, HasMouseFocus && thumbHover, thumbHover, false);

				var upOffset = !upPressed || upDisabled ? 4 : 4 + ButtonDepth;
				var downOffset = !downPressed || downDisabled ? 4 : 4 + ButtonDepth;

				var upArrowImage = getUpArrowImage.Update((upDisabled, upPressed, upHover, false, false));
				WidgetUtils.DrawSprite(upArrowImage,
					new float2(upButtonRect.Left + upOffset, upButtonRect.Top + upOffset));

				var downArrowImage = getDownArrowImage.Update((downDisabled, downPressed, downHover, false, false));
				WidgetUtils.DrawSprite(downArrowImage,
					new float2(downButtonRect.Left + downOffset, downButtonRect.Top + downOffset));
			}

			var drawBounds = backgroundRect.InflateBy(-BorderWidth, -BorderWidth, -BorderWidth, -BorderWidth);
			Game.Renderer.EnableScissor(drawBounds);

			// ChildOrigin enumerates the widget tree, so only evaluate it once
			var co = ChildOrigin;
			drawBounds = new Rectangle(drawBounds.X - co.X, drawBounds.Y - co.Y, drawBounds.Width, drawBounds.Height);

			foreach (var child in Children)
				if (child.Bounds.ToRectangle().IntersectsWith(drawBounds))
					child.DrawOuter();

			Game.Renderer.DisableScissor();
		}

		public override int2 ChildOrigin => RenderOrigin + new int2(ScrollBar == ScrollBar.Left ? ScrollbarWidth : 0, (int)currentListOffset);

		public override bool EventBoundsContains(int2 location)
		{
			return EventBounds.Contains(location);
		}

		void Scroll(int amount, bool smooth = false)
		{
			var newTarget = targetListOffset + amount * Game.Settings.Game.UIScrollSpeed;
			newTarget = Math.Min(0, Math.Max(Bounds.Height - ContentHeight, newTarget));

			SetListOffset(newTarget, smooth);
		}

		public void ScrollToBottom(bool smooth = false)
		{
			var value = Align == ScrollPanelAlign.Top ?
				Math.Min(0, Bounds.Height - ContentHeight) :
				Bounds.Height - ContentHeight;

			SetListOffset(value, smooth);
		}

		public void ScrollToTop(bool smooth = false)
		{
			var value = Align == ScrollPanelAlign.Top ? 0 :
				Math.Max(0, Bounds.Height - ContentHeight);

			SetListOffset(value, smooth);
		}

		public bool HandleScrollKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			switch (e.Key)
			{
				case Keycode.PAGEUP:
					Scroll(1);
					return true;
				case Keycode.PAGEDOWN:
					Scroll(-1);
					return true;
				case Keycode.HOME:
					ScrollToTop(true);
					return true;
				case Keycode.END:
					ScrollToBottom(true);
					return true;
			}

			return false;
		}

		public bool ScrolledToBottom => targetListOffset == Math.Min(0, Bounds.Height - ContentHeight) || ContentHeight <= Bounds.Height;

		void ScrollToItem(Widget item, bool smooth = false)
		{
			// Scroll the item to be visible
			float? newOffset = null;
			if (item.Bounds.Top + currentListOffset < 0)
				newOffset = ItemSpacing - item.Bounds.Top;

			if (item.Bounds.Bottom + currentListOffset > RenderBounds.Height)
				newOffset = RenderBounds.Height - item.Bounds.Bottom - ItemSpacing;

			if (newOffset.HasValue)
				SetListOffset(newOffset.Value, smooth);
		}

		public void ScrollToItem(string itemKey, bool smooth = false)
		{
			var item = Children.FirstOrDefault(c => c is ScrollItemWidget si && si.ItemKey == itemKey);

			if (item != null)
				ScrollToItem(item, smooth);
		}

		public void ScrollToSelectedItem()
		{
			var item = Children.FirstOrDefault(c => c is ScrollItemWidget si && si.IsSelected());

			if (item != null)
				ScrollToItem(item);
		}

		public void ScrollIntoView(Widget widget)
		{
			// Use screen-space RenderBounds to determine whether the widget is outside the visible area of this panel.
			var widgetTop = widget.RenderBounds.Top - RenderBounds.Top;
			var widgetBottom = widget.RenderBounds.Bottom - RenderBounds.Top;

			float? newOffset = null;
			if (widgetTop < 0)
				newOffset = currentListOffset - widgetTop + ItemSpacing;
			else if (widgetBottom > RenderBounds.Height)
				newOffset = currentListOffset - widgetBottom + RenderBounds.Height - ItemSpacing;

			if (newOffset.HasValue)
				SetListOffset(newOffset.Value, false);
		}

		void UpdateSmoothScrolling()
		{
			if (lastSmoothScrollTime == 0)
			{
				lastSmoothScrollTime = Game.RunTime;
				return;
			}

			var offsetDiff = targetListOffset - currentListOffset;
			var absOffsetDiff = Math.Abs(offsetDiff);
			if (absOffsetDiff > 1f)
			{
				var dt = Game.RunTime - lastSmoothScrollTime;
				currentListOffset += offsetDiff * SmoothScrollSpeed.Clamp(0.1f, 1.0f) * dt / 40;

				Ui.ResetTooltips();
			}
			else
				SetListOffset(targetListOffset, false);

			lastSmoothScrollTime = Game.RunTime;
		}

		public override void Tick()
		{
			if (upPressed)
				Scroll(1);

			if (downPressed)
				Scroll(-1);
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			upPressed = downPressed = thumbPressed = false;
			return base.YieldMouseFocus(mi);
		}

		int2 lastMouseLocation;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll)
			{
				Scroll(mi.Delta.Y, true);
				return true;
			}

			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;

			if (!HasMouseFocus)
				return false;

			if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
				return YieldMouseFocus(mi);

			if (thumbPressed && mi.Event == MouseInputEvent.Move)
			{
				var rb = RenderBounds;
				var scrollbarHeight = rb.Height - 2 * ScrollbarWidth;
				var thumbHeight = ContentHeight == 0 ? 0 : Math.Max(MinimumThumbSize, (int)(scrollbarHeight * Math.Min(rb.Height * 1f / ContentHeight, 1f)));
				var oldOffset = currentListOffset;

				var newOffset = currentListOffset + (int)((lastMouseLocation.Y - mi.Location.Y) * (ContentHeight - rb.Height) * 1f / (scrollbarHeight - thumbHeight));
				newOffset = Math.Min(0, Math.Max(rb.Height - ContentHeight, newOffset));
				SetListOffset(newOffset, false);

				if (oldOffset != newOffset)
					lastMouseLocation = mi.Location;
			}
			else
			{
				upPressed = upButtonRect.Contains(mi.Location);
				downPressed = downButtonRect.Contains(mi.Location);
				thumbPressed = thumbRect.Contains(mi.Location);
				if (thumbPressed)
					lastMouseLocation = mi.Location;

				if (mi.Event == MouseInputEvent.Down)
				{
					if (thumbPressed || (upPressed && !upDisabled) || (downPressed && !downDisabled))
						Game.Sound.PlayNotification(modRules, null, "Sounds", ClickSound, null);
					else if ((upPressed && upDisabled) || (downPressed && downDisabled))
						Game.Sound.PlayNotification(modRules, null, "Sounds", ClickDisabledSound, null);
				}
			}

			return upPressed || downPressed || thumbPressed;
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event != KeyInputEvent.Down)
				return false;

			// Check if we have ScrollItemWidget children for standard navigation
			var selectableItems = GetSelectableItems();
			var hasScrollItems = selectableItems.Count > 0;

			// If no ScrollItemWidget children, use focusable widget navigation instead
			if (!hasScrollItems)
				return HandleFocusableWidgetKeyPress(e);

			switch (e.Key)
			{
				case Keycode.UP:
					if (e.Modifiers.HasFlag(Modifiers.Meta) && Platform.CurrentPlatform == PlatformType.OSX)
						return FocusFirstItem();
					else
						return FocusAdjacentItem(forward: false);

				case Keycode.DOWN:
					if (e.Modifiers.HasFlag(Modifiers.Meta) && Platform.CurrentPlatform == PlatformType.OSX)
						return FocusLastItem();
					else
						return FocusAdjacentItem(forward: true);

				case Keycode.PAGEUP:
					return FocusItemByPage(forward: false);

				case Keycode.PAGEDOWN:
					return FocusItemByPage(forward: true);

				case Keycode.HOME:
					return FocusFirstItem();

				case Keycode.END:
					return FocusLastItem();

				case Keycode.RETURN:
				case Keycode.KP_ENTER:
				case Keycode.SPACE:
					if (OnEnterKey != null && OnEnterKey())
						return true;
					return ActivateFocusedItem();

				case Keycode.ESCAPE:
					if (OnEscapeKey != null)
						return OnEscapeKey();
					return false;

				case Keycode.DELETE:
					if (OnDeleteKey != null)
						return OnDeleteKey();
					return false;

				default:
					return false;
			}
		}

		// Handle keyboard navigation for panels with focusable widgets (like checkboxes) instead of ScrollItemWidgets
		bool HandleFocusableWidgetKeyPress(KeyInput e)
		{
			var focusableWidgets = GetFocusableWidgets();

			// If no focusable widgets, use pure scroll navigation (for panels like Credits)
			if (focusableWidgets.Count == 0)
				return HandlePureScrollKeyPress(e);

			switch (e.Key)
			{
				case Keycode.UP:
					return FocusAdjacentFocusableWidget(forward: false);

				case Keycode.DOWN:
					return FocusAdjacentFocusableWidget(forward: true);

				case Keycode.PAGEUP:
					return FocusFocusableWidgetByPage(forward: false);

				case Keycode.PAGEDOWN:
					return FocusFocusableWidgetByPage(forward: true);

				case Keycode.HOME:
					return FocusFirstFocusableWidget();

				case Keycode.END:
					return FocusLastFocusableWidget();

				case Keycode.RETURN:
				case Keycode.KP_ENTER:
				case Keycode.SPACE:
					return ActivateFocusedFocusableWidget();

				case Keycode.ESCAPE:
					if (OnEscapeKey != null)
						return OnEscapeKey();
					return false;

				case Keycode.DELETE:
					if (OnDeleteKey != null)
						return OnDeleteKey();
					return false;

				default:
					return false;
			}
		}

		// Handle keyboard scrolling for panels with no selectable items (like Credits)
		bool HandlePureScrollKeyPress(KeyInput e)
		{
			switch (e.Key)
			{
				case Keycode.UP:
					Scroll(1, true);
					return true;

				case Keycode.DOWN:
					Scroll(-1, true);
					return true;

				case Keycode.PAGEUP:
					ScrollByPixels(Bounds.Height);
					return true;

				case Keycode.PAGEDOWN:
					ScrollByPixels(-Bounds.Height);
					return true;

				case Keycode.HOME:
					ScrollToTop(true);
					return true;

				case Keycode.END:
					ScrollToBottom(true);
					return true;

				case Keycode.ESCAPE:
					if (OnEscapeKey != null)
						return OnEscapeKey();
					return false;

				case Keycode.DELETE:
					if (OnDeleteKey != null)
						return OnDeleteKey();
					return false;

				default:
					return false;
			}
		}

		void ScrollByPixels(int pixels)
		{
			var newTarget = targetListOffset + pixels;
			newTarget = Math.Min(0, Math.Max(Bounds.Height - ContentHeight, newTarget));
			SetListOffset(newTarget, true);
		}

		// Get all focusable widgets in this panel (for panels without ScrollItemWidgets)
		List<Widget> GetFocusableWidgets()
		{
			var widgets = new List<Widget>();
			CollectFocusableWidgets(this, widgets);
			return widgets;
		}

		static void CollectFocusableWidgets(Widget parent, List<Widget> result)
		{
			foreach (var child in parent.Children)
			{
				if (child.IsVisible() && child.IsFocusable)
					result.Add(child);

				CollectFocusableWidgets(child, result);
			}
		}

		bool FocusAdjacentFocusableWidget(bool forward)
		{
			var focusableWidgets = GetFocusableWidgets();
			if (focusableWidgets.Count == 0)
				return false;

			// Find the currently focused widget
			var currentIndex = Ui.TabFocusWidget != null
				? focusableWidgets.IndexOf(Ui.TabFocusWidget)
				: -1;

			int newIndex;
			if (currentIndex == -1)
				newIndex = forward ? 0 : focusableWidgets.Count - 1;
			else
			{
				newIndex = forward ? currentIndex + 1 : currentIndex - 1;

				// Wrap around
				if (newIndex < 0)
					newIndex = focusableWidgets.Count - 1;
				else if (newIndex >= focusableWidgets.Count)
					newIndex = 0;
			}

			var newFocusWidget = focusableWidgets[newIndex];

			// Transfer TAB focus
			var oldFocusWidget = Ui.TabFocusWidget;
			Ui.TabFocusWidget = newFocusWidget;

			oldFocusWidget?.OnTabFocusLost();

			newFocusWidget.OnTabFocusGained();

			return true;
		}

		bool ActivateFocusedFocusableWidget()
		{
			if (Ui.TabFocusWidget == null)
				return false;

			// Check if the focused widget is within this panel
			var focusableWidgets = GetFocusableWidgets();
			if (!focusableWidgets.Contains(Ui.TabFocusWidget))
				return false;

			// Activate the widget using its OnTabFocusActivate
			var dummyInput = new KeyInput { Key = Keycode.RETURN, Event = KeyInputEvent.Down };
			return Ui.TabFocusWidget.OnTabFocusActivate(dummyInput);
		}

		bool ActivateFocusedItem()
		{
			if (keyboardFocusedItem == null)
				return false;

			// Prefer OnKeyboardSelect if defined, otherwise fall back to OnClick.
			if (keyboardFocusedItem.OnKeyboardSelect != null)
				keyboardFocusedItem.OnKeyboardSelect();
			else
				keyboardFocusedItem.OnClick?.Invoke();

			return true;
		}

		bool FocusAdjacentItem(bool forward)
		{
			var selectableItems = GetSelectableItems();
			if (selectableItems.Count == 0)
				return false;

			var currentIndex = keyboardFocusedItem != null
				? selectableItems.IndexOf(keyboardFocusedItem)
				: selectableItems.FindIndex(item => item.IsSelected());

			int newIndex;
			if (currentIndex == -1)
				newIndex = forward ? 0 : selectableItems.Count - 1;
			else
			{
				newIndex = forward ? currentIndex + 1 : currentIndex - 1;

				if (newIndex < 0 || newIndex >= selectableItems.Count)
					return true;
			}

			SetKeyboardFocus(selectableItems[newIndex]);
			ScrollToItem(selectableItems[newIndex]);

			return true;
		}

		bool FocusFirstItem()
		{
			var selectableItems = GetSelectableItems();
			if (selectableItems.Count == 0)
				return false;

			SetKeyboardFocus(selectableItems[0]);
			ScrollToItem(selectableItems[0]);

			return true;
		}

		bool FocusLastItem()
		{
			var selectableItems = GetSelectableItems();
			if (selectableItems.Count == 0)
				return false;

			SetKeyboardFocus(selectableItems[^1]);
			ScrollToItem(selectableItems[^1]);

			return true;
		}

		bool FocusItemByPage(bool forward)
		{
			var selectableItems = GetSelectableItems();
			if (selectableItems.Count == 0)
				return false;

			var currentIndex = keyboardFocusedItem != null
				? selectableItems.IndexOf(keyboardFocusedItem)
				: selectableItems.FindIndex(item => item.IsSelected());

			if (currentIndex == -1)
				currentIndex = forward ? 0 : selectableItems.Count - 1;

			// Calculate how many items fit in the visible area
			var itemHeight = selectableItems[0].Bounds.Height + ItemSpacing;
			var itemsPerPage = itemHeight > 0 ? Math.Max(1, Bounds.Height / itemHeight) : 1;

			var newIndex = forward
				? Math.Min(currentIndex + itemsPerPage, selectableItems.Count - 1)
				: Math.Max(currentIndex - itemsPerPage, 0);

			if (newIndex == currentIndex)
				return true;

			SetKeyboardFocus(selectableItems[newIndex]);
			ScrollToItem(selectableItems[newIndex]);

			return true;
		}

		bool FocusFocusableWidgetByPage(bool forward)
		{
			var focusableWidgets = GetFocusableWidgets();
			if (focusableWidgets.Count == 0)
				return false;

			var currentIndex = Ui.TabFocusWidget != null
				? focusableWidgets.IndexOf(Ui.TabFocusWidget)
				: -1;

			if (currentIndex == -1)
				currentIndex = forward ? 0 : focusableWidgets.Count - 1;

			// Estimate items per page based on first widget height
			var itemHeight = focusableWidgets[0].Bounds.Height + ItemSpacing;
			var itemsPerPage = itemHeight > 0 ? Math.Max(1, Bounds.Height / itemHeight) : 1;

			var newIndex = forward
				? Math.Min(currentIndex + itemsPerPage, focusableWidgets.Count - 1)
				: Math.Max(currentIndex - itemsPerPage, 0);

			if (newIndex == currentIndex)
				return true;

			var newFocusWidget = focusableWidgets[newIndex];

			var oldFocusWidget = Ui.TabFocusWidget;
			Ui.TabFocusWidget = newFocusWidget;

			oldFocusWidget?.OnTabFocusLost();

			newFocusWidget.OnTabFocusGained();

			return true;
		}

		bool FocusFirstFocusableWidget()
		{
			var focusableWidgets = GetFocusableWidgets();
			if (focusableWidgets.Count == 0)
				return false;

			var newFocusWidget = focusableWidgets[0];

			var oldFocusWidget = Ui.TabFocusWidget;
			Ui.TabFocusWidget = newFocusWidget;

			oldFocusWidget?.OnTabFocusLost();

			newFocusWidget.OnTabFocusGained();

			return true;
		}

		bool FocusLastFocusableWidget()
		{
			var focusableWidgets = GetFocusableWidgets();
			if (focusableWidgets.Count == 0)
				return false;

			var newFocusWidget = focusableWidgets[^1];

			var oldFocusWidget = Ui.TabFocusWidget;
			Ui.TabFocusWidget = newFocusWidget;

			oldFocusWidget?.OnTabFocusLost();

			newFocusWidget.OnTabFocusGained();

			return true;
		}

		void SetKeyboardFocus(ScrollItemWidget item)
		{
			if (keyboardFocusedItem == item)
				return;

			if (keyboardFocusedItem != null)
				keyboardFocusedItem.IsKeyboardFocused = () => false;

			keyboardFocusedItem = item;

			if (keyboardFocusedItem != null)
				keyboardFocusedItem.IsKeyboardFocused = () => true;

			// Notify listeners that the keyboard focus has changed
			OnKeyboardFocusChanged?.Invoke(item);
		}

		// Called when an item is clicked with the mouse to clear keyboard navigation state.
		// This allows the visual highlight to fall back to the selected item.
		public void ClearKeyboardFocusedItem()
		{
			SetKeyboardFocus(null);
		}

		List<ScrollItemWidget> GetSelectableItems()
		{
			var items = new List<ScrollItemWidget>();

			foreach (var child in Children)
			{
				// Filter by IsVisible() for logical visibility (e.g. filtered items in AssetBrowser).
				// Note: Items using IsCulledForRendering for geometric culling are still included
				// because IsVisible() remains true for them - they are just not rendered when outside
				// the visible scroll area but should still be navigable via keyboard.
				if (child is ScrollItemWidget scrollItem && scrollItem.IsVisible() && scrollItem.IsSelectable)
					items.Add(scrollItem);
			}

			return items;
		}

		IObservableCollection collection;
		Func<object, Widget> makeWidget;
		Func<Widget, object, bool> widgetItemEquals;
		bool autoScroll;

		public void Unbind()
		{
			Bind(null, null, null, false);
		}

		public void Bind(IObservableCollection c, Func<object, Widget> makeWidget, Func<Widget, object, bool> widgetItemEquals, bool autoScroll)
		{
			this.autoScroll = autoScroll;

			Game.RunAfterTick(() =>
			{
				if (collection != null)
				{
					collection.OnAdd -= BindingAdd;
					collection.OnRemove -= BindingRemove;
					collection.OnRemoveAt -= BindingRemoveAt;
					collection.OnSet -= BindingSet;
					collection.OnRefresh -= BindingRefresh;
				}

				this.makeWidget = makeWidget;
				this.widgetItemEquals = widgetItemEquals;

				RemoveChildren();
				collection = c;

				if (c != null)
				{
					foreach (var item in c.ObservedItems)
						BindingAddImpl(item);

					c.OnAdd += BindingAdd;
					c.OnRemove += BindingRemove;
					c.OnRemoveAt += BindingRemoveAt;
					c.OnSet += BindingSet;
					c.OnRefresh += BindingRefresh;
				}
			});
		}

		void BindingAdd(IObservableCollection col, object item)
		{
			Game.RunAfterTick(() =>
			{
				if (collection != col)
					return;

				BindingAddImpl(item);
			});
		}

		void BindingAddImpl(object item)
		{
			if (makeWidget == null)
				return;

			var widget = makeWidget(item);
			var scrollToBottom = autoScroll && ScrolledToBottom;

			AddChild(widget);

			if (scrollToBottom)
				ScrollToBottom();
		}

		void BindingRemove(IObservableCollection col, object item)
		{
			Game.RunAfterTick(() =>
			{
				if (collection != col)
					return;

				var widget = Children.FirstOrDefault(w => widgetItemEquals(w, item));
				if (widget != null)
					RemoveChild(widget);
			});
		}

		void BindingRemoveAt(IObservableCollection col, int index)
		{
			Game.RunAfterTick(() =>
			{
				if (collection != col)
					return;

				if (index < 0 || index >= Children.Count)
					return;

				RemoveChild(Children[index]);
			});
		}

		void BindingSet(IObservableCollection col, object oldItem, object newItem)
		{
			Game.RunAfterTick(() =>
			{
				if (collection != col)
					return;

				var newWidget = makeWidget(newItem);
				newWidget.Parent = this;

				var i = Children.FindIndex(w => widgetItemEquals(w, oldItem));
				if (i >= 0)
				{
					var oldWidget = Children[i];
					oldWidget.Removed();
					Children[i] = newWidget;
					Layout.AdjustChildren();
				}
				else
					AddChild(newWidget);
			});
		}

		void BindingRefresh(IObservableCollection col)
		{
			Game.RunAfterTick(() =>
			{
				if (collection != col)
					return;

				RemoveChildren();
				foreach (var item in collection.ObservedItems)
					BindingAddImpl(item);
			});
		}
	}
}
