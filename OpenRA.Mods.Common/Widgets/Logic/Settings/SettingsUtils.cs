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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class SettingsUtils
	{
		public static void BindCheckboxPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException($"{group.GetType().Name} does not contain a preference type {pref}");

			var cb = parent.Get<CheckboxWidget>(id);
			cb.IsChecked = () => (bool)field.GetValue(group);
			cb.OnClick = () => field.SetValue(group, cb.IsChecked() ^ true);
		}

		public static void BindSliderPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException($"{group.GetType().Name} does not contain a preference type {pref}");

			var ss = parent.Get<SliderWidget>(id);
			ss.Value = (float)field.GetValue(group);
			ss.OnChange += x => field.SetValue(group, x);
		}

		public static void BindIntSliderPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException($"{group.GetType().Name} does not contain a preference type {pref}");

			var ss = parent.Get<SliderWidget>(id);
			ss.Value = (float)(int)field.GetValue(group);
			ss.OnChange += x => field.SetValue(group, (int)x);
		}

		public static void AdjustSettingsScrollPanelLayout(ScrollPanelWidget scrollPanel)
		{
			foreach (var row in scrollPanel.Children)
			{
				if (row.Children.Count == 0)
					continue;

				var hasVisibleChildren = false;

				foreach (var container in row.Children)
				{
					if (container.IsVisible())
					{
						hasVisibleChildren = true;
						break;
					}
				}

				if (!hasVisibleChildren)
					row.Visible = false;
			}

			scrollPanel.Layout.AdjustChildren();
		}
	}
}
