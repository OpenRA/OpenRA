#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	class ProductionQueueFromSelectionInfo : ITraitInfo
	{
		public string ProductionTabsWidget = null;
		public string ProductionPaletteWidget = null;

		public object Create(ActorInitializer init) { return new ProductionQueueFromSelection(init.World, this); }
	}

	class ProductionQueueFromSelection : INotifySelection
	{
		readonly World world;
		readonly Lazy<ProductionTabsWidget> tabsWidget;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;

		public ProductionQueueFromSelection(World world, ProductionQueueFromSelectionInfo info)
		{
			this.world = world;

			tabsWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionTabsWidget) as ProductionTabsWidget);
			paletteWidget = Exts.Lazy(() => Ui.Root.GetOrNull(info.ProductionPaletteWidget) as ProductionPaletteWidget);
		}

		public void SelectionChanged()
		{
			// Disable for spectators
			if (world.LocalPlayer == null)
				return;

			// Queue-per-actor
			var queue = world.Selection.Actors
				.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
				.SelectMany(a => a.TraitsImplementing<ProductionQueue>())
				.FirstOrDefault(q => q.Enabled);

			// Queue-per-player
			if (queue == null)
			{
				var types = world.Selection.Actors.Where(a => a.IsInWorld && a.World.LocalPlayer == a.Owner)
					.SelectMany(a => a.TraitsImplementing<Production>())
					.SelectMany(t => t.Info.Produces);

				queue = world.LocalPlayer.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => q.Enabled && types.Contains(q.Info.Type));
			}

			if (queue == null)
				return;

			if (tabsWidget.Value != null)
				tabsWidget.Value.CurrentQueue = queue;
			else if (paletteWidget.Value != null)
				paletteWidget.Value.CurrentQueue = queue;
		}
	}
}
