using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Traits
{
	class ChoosePaletteOnSelectInfo : StatelessTraitInfo<ChoosePaletteOnSelect> { }

	class ChoosePaletteOnSelect : INotifySelection
	{
		public void SelectionChanged()
		{
			var firstItem = Game.controller.selection.Actors.FirstOrDefault(
				a => a.World.LocalPlayer == a.Owner && a.traits.Contains<Production>());

			if (firstItem == null)
				return;

			var produces = firstItem.Info.Traits.Get<ProductionInfo>().Produces.FirstOrDefault();
			if (produces == null)
				return;

			Game.chrome.SetCurrentTab(produces);
		}
	}
}
