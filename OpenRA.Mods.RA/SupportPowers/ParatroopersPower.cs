#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ParatroopersPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public string[] DropItems = { };
		[ActorReference]
		public string UnitType = "badr";
		[ActorReference]
		public string FlareType = "flare";

		[Desc("In game ticks. Default value equates to 2 minutes.")]
		public readonly int FlareTime = 25 * 60 * 2;

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init.self, this); }
	}

	public class ParatroopersPower : SupportPower
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var info = (ParatroopersPowerInfo)Info;
			var items = info.DropItems;
			var startPos = self.World.Map.ChooseRandomEdgeCell(self.World.SharedRandom);

			self.World.AddFrameEndTask(w =>
			{
				var flare = info.FlareType != null ? w.CreateActor(info.FlareType, new TypeDictionary
				{
					new LocationInit(order.TargetLocation),
					new OwnerInit(self.Owner),
				}) : null;

				if (flare != null)
				{
					flare.QueueActivity(new Wait(info.FlareTime));
					flare.QueueActivity(new RemoveSelf());
				}

				var altitude = self.World.Map.Rules.Actors[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude;
				var a = w.CreateActor(info.UnitType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WRange.Zero, WRange.Zero, altitude)),
					new OwnerInit(self.Owner),
					new FacingInit(w.Map.FacingBetween(startPos, order.TargetLocation, 0))
				});

				a.CancelActivity();
				a.QueueActivity(new FlyAttack(Target.FromOrder(self.World, order)));
				a.Trait<ParaDrop>().SetLZ(order.TargetLocation, !info.AllowImpassableCells);

				var cargo = a.Trait<Cargo>();
				foreach (var i in items)
					cargo.Load(a, self.World.CreateActor(false, i.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(a.Owner) }));
			});
		}
	}
}
