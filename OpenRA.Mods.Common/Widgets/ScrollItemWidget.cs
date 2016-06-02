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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ScrollItemWidget : ButtonWidget
	{
		public string ItemKey;
		public string BaseName = "scrollitem";

		[ObjectCreator.UseCtor]
		public ScrollItemWidget(ModData modData)
			: base(modData)
		{
			IsVisible = () => false;
			VisualHeight = 0;
			IgnoreChildMouseOver = true;
		}

		protected ScrollItemWidget(ScrollItemWidget other)
			: base(other)
		{
			IsVisible = () => false;
			VisualHeight = 0;
			IgnoreChildMouseOver = true;
			Key = other.Key;
			BaseName = other.BaseName;
		}

		public Func<bool> IsSelected = () => false;

		public override void Draw()
		{
			var state = IsSelected() ? BaseName + "-selected" :
				Ui.MouseOverWidget == this ? BaseName + "-hover" :
				null;

			if (state != null)
				WidgetUtils.DrawPanel(state, RenderBounds);
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
