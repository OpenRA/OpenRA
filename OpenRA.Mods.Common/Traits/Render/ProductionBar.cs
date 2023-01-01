#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Visualizes the remaining build time of actor produced here.")]
	class ProductionBarInfo : ConditionalTraitInfo, Requires<ProductionInfo>, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Production queue type, for actors with multiple queues.")]
		public readonly string ProductionType = null;

		public readonly Color Color = Color.SkyBlue;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			// Per-actor queue
			var queue = ai.TraitInfos<ProductionQueueInfo>().FirstOrDefault(q => ProductionType == q.Type);

			// No queues available - check for classic production queues
			if (queue == null)
				queue = rules.Actors[SystemActors.Player].TraitInfos<ProductionQueueInfo>().FirstOrDefault(q => ProductionType == q.Type);

			if (queue == null)
				throw new YamlException($"Can't find a queue with ProductionType '{ProductionType}'");

			base.RulesetLoaded(rules, ai);
		}

		public override object Create(ActorInitializer init) { return new ProductionBar(init.Self, this); }
	}

	class ProductionBar : ConditionalTrait<ProductionBarInfo>, ISelectionBar, ITick, INotifyOwnerChanged
	{
		readonly Actor self;
		ProductionQueue queue;
		float value;

		public ProductionBar(Actor self, ProductionBarInfo info)
			: base(info)
		{
			this.self = self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			FindQueue();
		}

		void FindQueue()
		{
			// Per-actor queue
			// Note: this includes disabled queues, as each bar must bind to exactly one queue.
			queue = self.TraitsImplementing<ProductionQueue>()
				.FirstOrDefault(q => Info.ProductionType == q.Info.Type);

			if (queue == null)
			{
				// No queues available - check for classic production queues
				queue = self.Owner.PlayerActor.TraitsImplementing<ProductionQueue>()
					.FirstOrDefault(q => Info.ProductionType == q.Info.Type);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var current = queue.AllQueued().Where(i => i.Started).MinByOrDefault(i => i.RemainingTime);
			value = current != null ? 1 - (float)current.RemainingCost / current.TotalCost : 0;
		}

		float ISelectionBar.GetValue()
		{
			// Only people we like should see our production status.
			if (IsTraitDisabled || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				return 0;

			return value;
		}

		Color ISelectionBar.GetColor() { return Info.Color; }
		bool ISelectionBar.DisplayWhenEmpty => false;

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			FindQueue();
		}
	}
}
