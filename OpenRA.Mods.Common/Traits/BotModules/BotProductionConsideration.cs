#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls AI production.")]
	public class BotProductionConsiderationInfo : ConditionalTraitInfo
	{
		[FieldLoader.LoadUsing("LoadActors", true)]
		[Desc("Dictionary of [actor id]: [desired percentage of army/base composition].",
			"Composition percentages are calculated for each queue counting only actors with considerations in that queue.",
			"Limit and Delay nodes can be added to limit or delay when actors should be produced.")]
		public readonly Dictionary<string, ActorConsideration> Actors = new Dictionary<string, ActorConsideration>();

		[FieldLoader.Require]
		[Desc("Production queues that the consideration applies to.")]
		public readonly HashSet<string> Queues = new HashSet<string>();

		[Desc("Considerations with larger priorities will override definitions from lower priorities.")]
		public readonly int Priority = 1;

		public override object Create(ActorInitializer init) { return new BotProductionConsideration(this); }

		static object LoadActors(MiniYaml yaml)
		{
			var ret = new Dictionary<string, ActorConsideration>();
			var actors = yaml.Nodes.FirstOrDefault(n => n.Key == "Actors");
			if (actors != null)
				foreach (var a in actors.Value.Nodes)
					ret[a.Key] = new ActorConsideration(a.Value);

			return ret;
		}

		public class ActorConsideration
		{
			public readonly int Percentage = -1;
			public readonly int Limit = -1;
			public readonly int Delay = -1;

			public ActorConsideration(MiniYaml yaml)
			{
				if (!string.IsNullOrEmpty(yaml.Value))
					Percentage = FieldLoader.GetValue<int>("Percentage", yaml.Value);

				var limitsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Limit");
				if (limitsNode != null)
					Limit = FieldLoader.GetValue<int>("Limit", limitsNode.Value.Value);

				var delayNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Delay");
				if (delayNode != null)
					Delay = FieldLoader.GetValue<int>("Delay", delayNode.Value.Value);
			}
		}
	}

	public class BotProductionConsideration : ConditionalTrait<BotProductionConsiderationInfo>
	{
		IBotNotifyProductionConsiderationsUpdated[] production;

		public BotProductionConsideration(BotProductionConsiderationInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);
			production = self.TraitsImplementing<IBotNotifyProductionConsiderationsUpdated>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var p in production.Where(Exts.IsTraitEnabled))
				p.ProductionConsiderationsUpdated(Info.Queues);
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var p in production.Where(Exts.IsTraitEnabled))
				p.ProductionConsiderationsUpdated(Info.Queues);
		}
	}
}
