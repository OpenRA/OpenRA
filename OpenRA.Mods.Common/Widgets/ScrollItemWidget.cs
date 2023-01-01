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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ScrollItemWidget : ButtonWidget
	{
		public new readonly string Background = "scrollitem";
		public readonly bool EnableChildMouseOver = false;
		public string ItemKey;

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

		public override void Draw()
		{
			if (string.IsNullOrEmpty(Background))
				return;

			// PERF: Only check for ourself or our direct children
			var hover = Ui.MouseOverWidget == this;
			if (!IgnoreChildMouseOver && !hover)
				hover = Children.Contains(Ui.MouseOverWidget);

			WidgetUtils.DrawPanel(RenderBounds, getPanelCache.Update((IsDisabled(), Depressed, hover, false, IsSelected() || IsHighlighted())));
		}

		public override Widget Clone() { return new ScrollItemWidget(this); }

		public static ScrollItemWidget Setup(ScrollItemWidget template, Func<bool> isSelected, Action onClick)
		{
			var w = template.Clone() as ScrollItemWidget;
			w.IsVisible = () => true;
			w.IsSelected = isSelected;
			w.OnClick = onClick;
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
	}
}
