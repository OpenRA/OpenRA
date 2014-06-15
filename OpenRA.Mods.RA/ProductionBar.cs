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
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Visualizes the remaining build time of actor produced here.")]
	class ProductionBarInfo : ITraitInfo
	{
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;

		public object Create(ActorInitializer init) { return new ProductionBar(init.self, this); }
	}

	class ProductionBar : ISelectionBar, ITick
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

		public void Tick(Actor self)
		{
			if (queue == null)
			{
				var type = info.ProductionType ?? self.Trait<Production>().Info.Produces.First();

				// Per-actor queue
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

			var current = queue.CurrentItem();
			value = current != null ? 1 - (float)current.RemainingCost / current.TotalCost : 0;
		}

		public float GetValue()
		{
			// only people we like should see our production status.
			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		public Color GetColor() { return Color.SkyBlue; }
	}
}
