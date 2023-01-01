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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class HistoryLogLogic : ChromeLogic
	{
		readonly ScrollPanelWidget panel;
		readonly EditorActionManager editorActionManager;
		readonly ScrollItemWidget template;

		readonly Dictionary<EditorActionContainer, ScrollItemWidget> states = new Dictionary<EditorActionContainer, ScrollItemWidget>();

		[ObjectCreator.UseCtor]
		public HistoryLogLogic(Widget widget, World world)
		{
			panel = widget.Get<ScrollPanelWidget>("HISTORY_LIST");
			template = panel.Get<ScrollItemWidget>("HISTORY_TEMPLATE");
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editorActionManager.ItemAdded += ItemAdded;
			editorActionManager.ItemRemoved += ItemRemoved;
		}

		void ItemAdded(EditorActionContainer editorAction)
		{
			var item = ScrollItemWidget.Setup(template, () => false, () =>
			{
				if (editorAction.Status == EditorActionStatus.History)
					editorActionManager.Rewind(editorAction.Id);
				else if (editorAction.Status == EditorActionStatus.Future)
					editorActionManager.Forward(editorAction.Id);
			});

			var titleLabel = item.Get<LabelWidget>("TITLE");
			var textColor = template.TextColor;
			var futureTextColor = template.TextColorDisabled;

			titleLabel.GetText = () => editorAction.Action.Text;
			titleLabel.GetColor = () => editorAction.Status == EditorActionStatus.Future ? futureTextColor : textColor;

			item.IsSelected = () => editorAction.Status == EditorActionStatus.Active;
			panel.AddChild(item);

			states[editorAction] = item;
		}

		void ItemRemoved(EditorActionContainer editorAction)
		{
			var widget = states[editorAction];

			panel.RemoveChild(widget);

			states.Remove(editorAction);
		}
	}
}
