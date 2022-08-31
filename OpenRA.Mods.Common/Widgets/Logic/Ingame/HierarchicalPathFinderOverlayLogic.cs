﻿#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
				Func<Locomotor, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(
						template,
						() => hpfOverlay.Locomotor == option,
						() => hpfOverlay.Locomotor = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option?.Info.Name ?? "(Selected Units)";
					return item;
				};

				locomotorSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", locomotors.Length * 30, locomotors, setupItem);
			};

			var checks = new[] { BlockedByActor.None, BlockedByActor.Immovable };
			var checkSelector = widget.Get<DropDownButtonWidget>("HPF_OVERLAY_CHECK");
			checkSelector.OnMouseDown = _ =>
			{
				Func<BlockedByActor, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(
						template,
						() => hpfOverlay.Check == option,
						() => hpfOverlay.Check = option);
					item.Get<LabelWidget>("LABEL").GetText = () => option.ToString();
					return item;
				};

				checkSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", checks.Length * 30, checks, setupItem);
			};
		}
	}
}
