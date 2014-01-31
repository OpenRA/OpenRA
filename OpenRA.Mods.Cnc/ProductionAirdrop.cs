#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		public readonly string ReadyAudio = "Reinforce";
		[Desc("Cargo aircraft used.")]
		[ActorReference] public readonly string ActorType = "c17";

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(this); }
	}

	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ProductionAirdropInfo info)
			: base(info) { }

		public override bool Produce(Actor self, ActorInfo producee)
		{
			var owner = self.Owner;

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var startPos = self.Location + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, self.Location.Y);

			// Assume a single exit point for simplicity
			var exit = self.Info.Traits.WithInterface<ExitInfo>().First();

			var rb = self.Trait<RenderBuilding>();
			rb.PlayCustomAnimRepeating(self, "active");

			var actorType = (Info as ProductionAirdropInfo).ActorType;

			owner.World.AddFrameEndTask(w =>
			{
				var altitude = Rules.Info[actorType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var a = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(startPos.CenterPosition + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(64)
				});

				a.QueueActivity(Fly.ToCell(self.Location + new CVec(9, 0)));
				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead())
						return;

					rb.PlayCustomAnimRepeating(self, "idle");
					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit));
					Sound.PlayNotification(self.Owner, "Speech", (Info as ProductionAirdropInfo).ReadyAudio, self.Owner.Country.Race);
				}));
				a.QueueActivity(Fly.ToCell(endPos));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
