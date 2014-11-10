#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		public readonly string ReadyAudio = "Reinforce";
		[Desc("Cargo aircraft used.")]
		[ActorReference] public readonly string ActorType = "c17";

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(this, init.self); }
	}

	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ProductionAirdropInfo info, Actor self)
			: base(info, self) { }

		public override bool Produce(Actor self, ActorInfo producee, string raceVariant)
		{
			var owner = self.Owner;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var startPos = self.Location + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, self.Location.Y);

			// Assume a single exit point for simplicity
			var exit = self.Info.Traits.WithInterface<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			var info = (ProductionAirdropInfo)Info;
			var actorType = info.ActorType;

			owner.World.AddFrameEndTask(w =>
			{
				var altitude = self.World.Map.Rules.Actors[actorType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var a = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				a.QueueActivity(new Fly(a, Target.FromCell(w, self.Location + new CVec(9, 0))));
				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new CallFunc(() =>
				{
					if (!self.Flagged(ActorFlag.InWorld) || self.Flagged(ActorFlag.Dead))
						return;

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, raceVariant));
					Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Country.Race);
				}));

				a.QueueActivity(new Fly(a, Target.FromCell(w, endPos)));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
