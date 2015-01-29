#region Copyright & License Information
/*
 * This file is a modification of OpenRA, which is free software. It is
 * made available to you under the very same GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Extension.DS
{
	[Desc("Deliver the unit in production via helicopter.")]
	public class ProductionHelipadInfo : ProductionInfo
	{
		public readonly string ReadyAudio = "Reinforce";
		[Desc("Cargo aircraft used.")]
		[ActorReference] public readonly string ActorType = "tran";
		public readonly int OffsetX = 0;
		public readonly int OffsetY = 0;

		public override object Create(ActorInitializer init) { return new ProductionHelipad(this, init.Self); }
	}

	class ProductionHelipad : Production
	{
		public ProductionHelipad(ProductionHelipadInfo info, Actor self)
			: base(info, self) { }

		public override bool Produce(Actor self, ActorInfo producee, string raceVariant)
		{
			var owner = self.Owner;
			var info = (ProductionHelipadInfo)Info;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var startPos = self.Location + new CVec(owner.World.Map.Bounds.Width, info.OffsetY);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, self.Location.Y + info.OffsetY);

			// Assume a single exit point for simplicity
			var exit = self.Info.Traits.WithInterface<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			var actorType = info.ActorType;

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
					return;

				var altitude = self.World.Map.Rules.Actors[actorType].Traits.Get<HelicopterInfo>().CruiseAltitude;
				var a = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				a.QueueActivity(new HeliFly(a, Target.FromCell(w, self.Location + new CVec(info.OffsetX, info.OffsetY))));
				a.QueueActivity(new Turn(a,0));
				a.QueueActivity(new HeliLand(false));
				a.QueueActivity(new Wait(25));
				a.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
						return;

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, raceVariant));
					Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Country.Race);
				}));

				a.QueueActivity(new HeliFly(a, Target.FromCell(w, endPos)));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
