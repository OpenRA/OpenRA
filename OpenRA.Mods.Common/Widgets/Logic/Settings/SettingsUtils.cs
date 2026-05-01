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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class SettingsUtils
	{
		// Starting TabIndex for settings panel elements (menu tabs use 0-9, bottom buttons use 10000+)
		const int PanelElementsBaseTabIndex = 100;

		// Assigns sequential TabIndex values to all focusable widgets in a panel
		// This ensures panel elements are navigated after menu tabs but before bottom buttons
		public static void AssignPanelTabIndexes(Widget panel)
		{
			var focusableWidgets = new List<Widget>();
			CollectFocusableWidgets(panel, focusableWidgets);

			// Use DFS traversal order (= YAML declaration order), which is the correct logical
			// document order. Sorting by screen/document coordinates is unreliable because
			// items inside a ScrollPanel occupy coordinates that overlap with fixed widgets
			// outside the panel (e.g. the remap dialog at the bottom).
			for (var i = 0; i < focusableWidgets.Count; i++)
				focusableWidgets[i].TabIndex = PanelElementsBaseTabIndex + i;
		}

		static void CollectFocusableWidgets(Widget parent, List<Widget> result)
		{
			foreach (var child in parent.Children)
			{
				if (child.IsFocusable)
					result.Add(child);

				CollectFocusableWidgets(child, result);
			}
		}

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
			ss.Value = (int)field.GetValue(group);
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
