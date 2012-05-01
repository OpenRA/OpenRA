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
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionQueueFromSelectionInfo : ITraitInfo
	{
		public string ProductionTabsWidget = null;

		public object Create( ActorInitializer init ) { return new ProductionQueueFromSelection(init.world, this); }
	}

	class ProductionQueueFromSelection : INotifySelection
	{
		Lazy<ProductionTabsWidget> tabsWidget;
		readonly World world;

		public ProductionQueueFromSelection(World world, ProductionQueueFromSelectionInfo info)
		{
			this.world = world;

			tabsWidget = Lazy.New(() =>
				Ui.Root.Get<ProductionTabsWidget>(info.ProductionTabsWidget));
		}

		public void SelectionChanged()
		{
			// Find an actor with a queue
			var producer = world.Selection.Actors.FirstOrDefault(a => a.IsInWorld
				&& a.World.LocalPlayer == a.Owner
				&& a.HasTrait<ProductionQueue>());

			if (producer != null)
				tabsWidget.Value.CurrentQueue = producer.TraitsImplementing<ProductionQueue>().First();
		}
	}
}
