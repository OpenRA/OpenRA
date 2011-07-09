#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionQueueFromSelectionInfo : ITraitInfo
	{
		public string ProductionTabsWidget = null;
		public object Create( ActorInitializer init ) { return new ProductionQueueFromSelection(init.world, this); }
	}

	class ProductionQueueFromSelection : INotifySelection
	{
		ProductionQueueFromSelectionInfo info;
		readonly World world;

		public ProductionQueueFromSelection(World world, ProductionQueueFromSelectionInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void SelectionChanged()
		{
			// Find an actor with a queue
			var producer = world.Selection.Actors.FirstOrDefault(a => a.IsInWorld
			                                                     && a.World.LocalPlayer == a.Owner
			                                                     && a.HasTrait<ProductionQueue>());
			if (producer != null)			
				Widget.RootWidget.GetWidget<ProductionTabsWidget>(info.ProductionTabsWidget)
					.SelectQueue(producer.TraitsImplementing<ProductionQueue>().First());
		}
	}
}
