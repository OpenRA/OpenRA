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
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		public readonly string ReadyAudio = "Reinforce";
		[Desc("Cargo aircraft used for delivery. Must have the `Aircraft` trait.")]
		[ActorReference(typeof(AircraftInfo))] public readonly string ActorType = "c17";

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(init, this); }
	}

	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ActorInitializer init, ProductionAirdropInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, ActorInfo producee, string factionVariant)
		{
			var owner = self.Owner;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var startPos = self.Location + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, self.Location.Y);

			// Assume a single exit point for simplicity
			var exit = self.Info.TraitInfos<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			var info = (ProductionAirdropInfo)Info;
			var actorType = info.ActorType;

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
					return;

				var altitude = self.World.Map.Rules.Actors[actorType].TraitInfo<AircraftInfo>().CruiseAltitude;
				var actor = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WDist.Zero, WDist.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, self.Location + new CVec(12, 0))));
				actor.QueueActivity(new Land(actor, Target.FromActor(self)));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
						return;

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, factionVariant));
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, endPos)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
