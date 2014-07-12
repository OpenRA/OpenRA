#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	class ProductionQueueFromSelectionInfo : ITraitInfo
	{
		public string ProductionTabsWidget = null;

		public object Create(ActorInitializer init) { return new ProductionQueueFromSelection(init.world, this); }
	}

	class ProductionQueueFromSelection : INotifySelection
	{
		readonly World world;
		Lazy<ProductionTabsWidget> tabsWidget;

		public ProductionQueueFromSelection(World world, ProductionQueueFromSelectionInfo info)
		{
			this.world = world;

			tabsWidget = Exts.Lazy(() =>
				Ui.Root.Get<ProductionTabsWidget>(info.ProductionTabsWidget));
		}

		public void SelectionChanged()
		{
			// Find an actor with a queue
			var producer = world.Selection.Actors.FirstOrDefault(a => a.IsInWorld
				&& a.World.LocalPlayer == a.Owner
				&& a.TraitsImplementing<ProductionQueue>().Any(q => q.Enabled));

			if (producer != null)
				tabsWidget.Value.CurrentQueue = producer.TraitsImplementing<ProductionQueue>().First(q => q.Enabled);
		}
	}
}
