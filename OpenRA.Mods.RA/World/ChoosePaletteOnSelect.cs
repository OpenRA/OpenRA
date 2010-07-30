#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	class ChoosePaletteOnSelectInfo : TraitInfo<ChoosePaletteOnSelect> { }

	class ChoosePaletteOnSelect : INotifySelection
	{
		public void SelectionChanged()
		{
			var firstItem = Game.world.Selection.Actors.FirstOrDefault(
				a => a.World.LocalPlayer == a.Owner && a.traits.Contains<Production>());

			if (firstItem == null)
				return;

			var produces = firstItem.Info.Traits.Get<ProductionInfo>().Produces.FirstOrDefault();
			if (produces == null)
				return;

			Widget.RootWidget.GetWidget<BuildPaletteWidget>("INGAME_BUILD_PALETTE")
				.SetCurrentTab(produces);
		}
	}
}
