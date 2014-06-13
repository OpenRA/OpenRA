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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Widgets
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

	public class ScrollPanelWidget : Widget
	{
		public int ScrollbarWidth = 24;
		public int ItemSpacing = 2;
		public int ButtonDepth = ChromeMetrics.Get<int>("ButtonDepth");
		public string Background = "scrollpanel-bg";
		public int ContentHeight;
		public ILayout Layout;
		public int MinimumThumbSize = 10;
		public ScrollPanelAlign Align = ScrollPanelAlign.Top;
		public bool CollapseHiddenChildren;
		public float SmoothScrollSpeed = 0.333f;

		protected bool UpPressed;
		protected bool DownPressed;
		protected bool ThumbPressed;
		protected Rectangle upButtonRect;
		protected Rectangle downButtonRect;
		protected Rectangle backgroundRect;
		protected Rectangle scrollbarRect;
		protected Rectangle thumbRect;

		// The target value is the list offset we're trying to reach
		float targetListOffset;

		// The current value is the actual list offset at the moment
		float currentListOffset;

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
				currentListOffset = value;
		}

		public ScrollPanelWidget()
		{
			Layout = new ListLayout(this);
		}

		public override void RemoveChildren()
		{
			ContentHeight = 0;
			base.RemoveChildren();
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

			var rb = RenderBounds;

			var ScrollbarHeight = rb.Height - 2 * ScrollbarWidth;

			var thumbHeight = ContentHeight == 0 ? 0 : Math.Max(MinimumThumbSize, (int)(ScrollbarHeight * Math.Min(rb.Height * 1f / ContentHeight, 1f)));
			var thumbOrigin = rb.Y + ScrollbarWidth + (int)((ScrollbarHeight - thumbHeight) * (-1f * currentListOffset / (ContentHeight - rb.Height)));
			if (thumbHeight == ScrollbarHeight)
				thumbHeight = 0;

			backgroundRect = new Rectangle(rb.X, rb.Y, rb.Width - ScrollbarWidth + 1, rb.Height);
			upButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y, ScrollbarWidth, ScrollbarWidth);
			downButtonRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Bottom - ScrollbarWidth, ScrollbarWidth, ScrollbarWidth);
			scrollbarRect = new Rectangle(rb.Right - ScrollbarWidth, rb.Y + ScrollbarWidth - 1, ScrollbarWidth, ScrollbarHeight + 2);
			thumbRect = new Rectangle(rb.Right - ScrollbarWidth, thumbOrigin, ScrollbarWidth, thumbHeight);

			var upHover = Ui.MouseOverWidget == this && upButtonRect.Contains(Viewport.LastMousePos);
			var upDisabled = thumbHeight == 0 || currentListOffset >= 0;

			var downHover = Ui.MouseOverWidget == this && downButtonRect.Contains(Viewport.LastMousePos);
			var downDisabled = thumbHeight == 0 || currentListOffset <= Bounds.Height - ContentHeight;

			var thumbHover = Ui.MouseOverWidget == this && thumbRect.Contains(Viewport.LastMousePos);
			WidgetUtils.DrawPanel(Background, backgroundRect);
			WidgetUtils.DrawPanel("scrollpanel-bg", scrollbarRect);
			ButtonWidget.DrawBackground("button", upButtonRect, upDisabled, UpPressed, upHover, false);
			ButtonWidget.DrawBackground("button", downButtonRect, downDisabled, DownPressed, downHover, false);

			if (thumbHeight > 0)
				ButtonWidget.DrawBackground("scrollthumb", thumbRect, false, HasMouseFocus && thumbHover, thumbHover, false);

			var upOffset = !UpPressed || upDisabled ? 4 : 4 + ButtonDepth;
			var downOffset = !DownPressed || downDisabled ? 4 : 4 + ButtonDepth;

			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", UpPressed || upDisabled ? "up_pressed" : "up_arrow"),
				new float2(upButtonRect.Left + upOffset, upButtonRect.Top + upOffset));
			WidgetUtils.DrawRGBA(ChromeProvider.GetImage("scrollbar", DownPressed || downDisabled ? "down_pressed" : "down_arrow"),
				new float2(downButtonRect.Left + downOffset, downButtonRect.Top + downOffset));

			Game.Renderer.EnableScissor(backgroundRect.InflateBy(-1, -1, -1, -1));

			foreach (var child in Children)
				child.DrawOuter();

			Game.Renderer.DisableScissor();
		}

		public override int2 ChildOrigin { get { return RenderOrigin + new int2(0, (int)currentListOffset); } }

		public override Rectangle GetEventBounds()
		{
			return EventBounds;
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

		public bool ScrolledToBottom
		{
			get { return targetListOffset == Math.Min(0, Bounds.Height - ContentHeight) || ContentHeight <= Bounds.Height; }
		}

		public void ScrollToItem(string itemKey, bool smooth = false)
		{
			var item = Children.FirstOrDefault(c =>
			{
				var si = c as ScrollItemWidget;
				return si != null && si.ItemKey == itemKey;
			});

			if (item == null)
				return;

			// Scroll the item to be visible
			float? newOffset = null;
			if (item.Bounds.Top + currentListOffset < 0)
				newOffset = ItemSpacing - item.Bounds.Top;

			if (item.Bounds.Bottom + currentListOffset > RenderBounds.Height)
				newOffset = RenderBounds.Height - item.Bounds.Bottom - ItemSpacing;

			if (newOffset.HasValue)
				SetListOffset(newOffset.Value, smooth);
		}

		public override void Tick()
		{
			if (UpPressed)
				Scroll(1);

			if (DownPressed)
				Scroll(-1);

			var offsetDiff = targetListOffset - currentListOffset;
			var absOffsetDiff = Math.Abs(offsetDiff);
			if (absOffsetDiff > 1f)
				currentListOffset += offsetDiff * SmoothScrollSpeed.Clamp(0.1f, 1.0f);
			else
				SetListOffset(targetListOffset, false);
		}

		public override bool YieldMouseFocus(MouseInput mi)
		{
			UpPressed = DownPressed = ThumbPressed = false;
			return base.YieldMouseFocus(mi);
		}

		int2 lastMouseLocation;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll)
			{
				Scroll(mi.ScrollDelta, true);
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

			if (ThumbPressed && mi.Event == MouseInputEvent.Move)
			{
				var rb = RenderBounds;
				var ScrollbarHeight = rb.Height - 2 * ScrollbarWidth;
				var thumbHeight = ContentHeight == 0 ? 0 : Math.Max(MinimumThumbSize, (int)(ScrollbarHeight * Math.Min(rb.Height * 1f / ContentHeight, 1f)));
				var oldOffset = currentListOffset;

				var newOffset = currentListOffset + ((int)((lastMouseLocation.Y - mi.Location.Y) * (ContentHeight - rb.Height) * 1f / (ScrollbarHeight - thumbHeight)));
				newOffset = Math.Min(0, Math.Max(rb.Height - ContentHeight, newOffset));
				SetListOffset(newOffset, false);

				if (oldOffset != newOffset)
					lastMouseLocation = mi.Location;
			}
			else
			{
				UpPressed = upButtonRect.Contains(mi.Location);
				DownPressed = downButtonRect.Contains(mi.Location);
				ThumbPressed = thumbRect.Contains(mi.Location);
				if (ThumbPressed)
					lastMouseLocation = mi.Location;
			}

			return UpPressed || DownPressed || ThumbPressed;
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

		void BindingAdd(object item)
		{
			Game.RunAfterTick(() => BindingAddImpl(item));
		}

		void BindingAddImpl(object item)
		{
			var widget = makeWidget(item);
			var scrollToBottom = autoScroll && ScrolledToBottom;

			AddChild(widget);

			if (scrollToBottom)
				ScrollToBottom();
		}

		void BindingRemove(object item)
		{
			Game.RunAfterTick(() =>
			{
				var widget = Children.FirstOrDefault(w => widgetItemEquals(w, item));
				if (widget != null)
					RemoveChild(widget);
			});
		}

		void BindingRemoveAt(int index)
		{
			Game.RunAfterTick(() =>
			{
				if (index < 0 || index >= Children.Count)
					return;
				RemoveChild(Children[index]);
			});
		}

		void BindingSet(object oldItem, object newItem)
		{
			Game.RunAfterTick(() =>
			{
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

		void BindingRefresh()
		{
			Game.RunAfterTick(() =>
			{
				RemoveChildren();
				foreach (var item in collection.ObservedItems)
					BindingAddImpl(item);
			});
		}
	}
}
