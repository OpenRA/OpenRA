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
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining build time of actor produced here.")]
	class ProductionBarInfo : ITraitInfo, Requires<ProductionInfo>
	{
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;

		public readonly Color Color = Color.SkyBlue;

		public object Create(ActorInitializer init) { return new ProductionBar(init.Self, this); }
	}

	class ProductionBar : ISelectionBar, ITick, INotifyCreated, INotifyOwnerChanged
	{
		readonly ProductionBarInfo info;
		readonly Actor self;
		ProductionQueue queue;
		float value;

		public ProductionBar(Actor self, ProductionBarInfo info)
		{
			this.self = self;
			this.info = info;
		}

		void FindQueue()
		{
			var type = info.ProductionType ?? self.Info.TraitInfo<ProductionInfo>().Produces.First();

			// Per-actor queue
			// Note: this includes disabled queues, as each bar must bind to exactly one queue.
			queue = self.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => type == null || type == q.Info.Type);

			if (queue == null)
			{
				// No queues available - check for classic production queues
				queue = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => type == null || type == q.Info.Type);
			}

			if (queue == null)
				throw new InvalidOperationException("No queues available for production type '{0}'".F(type));
		}

		public void Created(Actor self)
		{
			FindQueue();
		}

		public void Tick(Actor self)
		{
			var current = queue.CurrentItem();
			value = current != null ? 1 - (float)current.RemainingCost / current.TotalCost : 0;
		}

		float ISelectionBar.GetValue()
		{
			// only people we like should see our production status.
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return info.Color; }

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			FindQueue();
		}
	}
}
