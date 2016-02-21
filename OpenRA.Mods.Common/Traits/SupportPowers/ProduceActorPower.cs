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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class ProduceActorPowerInfo : SupportPowerInfo
	{
		[ActorReference, FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		public override object Create(ActorInitializer init) { return new ProduceActorPower(init, this); }
	}

	public class ProduceActorPower : SupportPower
	{
		readonly string faction;

		public ProduceActorPower(ActorInitializer init, ProduceActorPowerInfo info)
			: base(init.Self, info)
		{
			faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : init.Self.Owner.Faction.InternalName;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.IssueOrder(new Order(order, manager.Self, false));
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var info = Info as ProduceActorPowerInfo;
			var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => p.Info.Produces.Contains(info.Type));

			// TODO: The power should not reset if the production fails.
			// Fixing this will require a larger rework of the support power code
			var activated = false;

			if (sp != null)
				foreach (var name in info.Actors)
					activated |= sp.Produce(self, self.World.Map.Rules.Actors[name], faction);

			if (activated)
				Game.Sound.PlayNotification(self.World.Map.Rules, manager.Self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
			else
				Game.Sound.PlayNotification(self.World.Map.Rules, manager.Self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);
		}
	}
}
