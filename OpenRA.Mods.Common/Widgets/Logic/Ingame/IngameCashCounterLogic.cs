#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameCashCounterLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngameCashCounterLogic(Widget widget, World world)
		{
			var resourceDisplay = world.LocalPlayer.PlayerActor.Trait<IResourceDisplay>();
			var cash = widget.Get<LabelWithTooltipWidget>("CASH");
			var label = cash.Text;

			cash.GetText = () => label.F(resourceDisplay.Amount);
			cash.GetTooltipText = () => "Silo Usage: {0}/{1}".F(resourceDisplay.CappedAmount, resourceDisplay.Capacity);
		}
	}
}
