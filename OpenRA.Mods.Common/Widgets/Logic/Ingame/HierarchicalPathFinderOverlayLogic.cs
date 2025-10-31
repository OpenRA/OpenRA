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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class HierarchicalPathFinderOverlayLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public HierarchicalPathFinderOverlayLogic(Widget widget, World world)
		{
			var hpfOverlay = world.WorldActor.Trait<HierarchicalPathFinderOverlay>();
			widget.IsVisible = () => hpfOverlay.Enabled;

			var locomotors = new Locomotor[] { null }.Concat(
				world.WorldActor.TraitsImplementing<Locomotor>().OrderBy(l => l.Info.Name))
				.ToArray();
			var locomotorSelector = widget.Get<DropDownButtonWidget>("HPF_OVERLAY_LOCOMOTOR");
			locomotorSelector.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(Locomotor option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(
						template,
						() => hpfOverlay.Locomotor == option,
						() => hpfOverlay.Locomotor = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option?.Info.Name ?? "(Selected Units)";
					return item;
				}

				locomotorSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", locomotors.Length * 30, locomotors, SetupItem);
			};

			var checks = new[] { BlockedByActor.None, BlockedByActor.Immovable };
			var checkSelector = widget.Get<DropDownButtonWidget>("HPF_OVERLAY_CHECK");
			checkSelector.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(BlockedByActor option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(
						template,
						() => hpfOverlay.Check == option,
						() => hpfOverlay.Check = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option.ToString();
					return item;
				}

				checkSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", checks.Length * 30, checks, SetupItem);
			};
		}
	}
}
