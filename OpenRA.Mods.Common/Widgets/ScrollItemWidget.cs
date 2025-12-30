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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ScrollItemWidget : ButtonWidget
	{
		public new readonly string Background = "scrollitem";
		public readonly bool EnableChildMouseOver = false;
		public string ItemKey;

		/// <summary>
		/// When true, this item is culled from rendering due to being outside the visible scroll area.
		/// Unlike IsVisible, culled items should still be navigable via keyboard.
		/// </summary>
		public Func<bool> IsCulledForRendering = () => false;

		readonly CachedTransform<(bool, bool, bool, bool, bool), Sprite[]> getPanelCache;

		[ObjectCreator.UseCtor]
		public ScrollItemWidget(ModData modData)
			: base(modData)
		{
			IsVisible = () => false;
			VisualHeight = 0;
			getPanelCache = WidgetUtils.GetCachedStatefulPanelImages(Background);
		}

		protected ScrollItemWidget(ScrollItemWidget other)
			: base(other)
		{
			IsVisible = () => false;
			VisualHeight = 0;
			Key = other.Key;
			Background = other.Background;
			EnableChildMouseOver = other.EnableChildMouseOver;
			IsCulledForRendering = other.IsCulledForRendering;
			getPanelCache = WidgetUtils.GetCachedStatefulPanelImages(Background);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			// HACK: We want to default IgnoreChildMouseOver to true in this widget
			// but still allow it to be disabled
			IgnoreChildMouseOver = !EnableChildMouseOver;
		}

		public Func<bool> IsSelected = () => false;
		public Func<bool> IsKeyboardFocused = () => false;
		public Action OnKeyboardSelect;
		public bool IsSelectable = true;

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left && Parent is ScrollPanelWidget scrollPanel)
			{
				scrollPanel.ClearKeyboardFocusedItem();
				scrollPanel.TakeKeyboardFocus();
			}

			return base.HandleMouseInput(mi);
		}

		public override void DrawOuter()
		{
			// Skip rendering if culled (outside visible scroll area) but keep IsVisible for logical filtering
			if (IsCulledForRendering())
				return;

			base.DrawOuter();
		}

		public override void Draw()
		{
			if (string.IsNullOrEmpty(Background))
				return;

			// PERF: Only check for ourself or our direct children
			var hover = Ui.MouseOverWidget == this;
			if (!IgnoreChildMouseOver && !hover)
				hover = Children.Contains(Ui.MouseOverWidget);

			var parentHasKeyboardFocus = Parent is ScrollPanelWidget sp && sp.HasKeyboardFocusedItem;
			var highlighted = IsKeyboardFocused() || (!parentHasKeyboardFocus && (IsSelected() || IsHighlighted()));
			WidgetUtils.DrawPanel(RenderBounds, getPanelCache.Update((IsDisabled(), Depressed, hover, false, highlighted)));
		}

		public override ScrollItemWidget Clone() { return new ScrollItemWidget(this); }

		public static ScrollItemWidget Setup(ScrollItemWidget template, Func<bool> isSelected, Action onClick)
		{
			var w = template.Clone();
			w.IsVisible = () => true;
			w.IsSelected = isSelected;
			w.OnClick = onClick;
			w.OnKeyboardSelect = onClick;
			return w;
		}

		public static ScrollItemWidget Setup(ScrollItemWidget template, Func<bool> isSelected, Action onClick, Action onDoubleClick)
		{
			var w = Setup(template, isSelected, onClick);
			w.OnDoubleClick = onDoubleClick;
			return w;
		}

		public static ScrollItemWidget Setup(string key, ScrollItemWidget template, Func<bool> isSelected, Action onClick, Action onDoubleClick)
		{
			var w = Setup(template, isSelected, onClick);
			w.OnDoubleClick = onDoubleClick;
			w.ItemKey = key;
			return w;
		}

		public static ScrollItemWidget SetupHeader(ScrollItemWidget template)
		{
			var w = template.Clone();
			w.IsVisible = () => true;
			w.IsSelected = () => false;
			w.IsSelectable = false;
			return w;
		}
	}
}
