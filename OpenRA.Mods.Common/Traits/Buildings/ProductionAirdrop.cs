#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		[NotificationReference("Speech")]
		public readonly string ReadyAudio = "Reinforce";

		[FieldLoader.Require]
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used for delivery. Must have the `Aircraft` trait.")]
		public readonly string ActorType = null;

		[Desc("The cargo aircraft will spawn at the player baseline (map edge directly behind the player spawn)")]
		public readonly bool BaselineSpawn = false;

		[Desc("Direction the aircraft should face to land.")]
		public readonly WAngle Facing = new WAngle(256);

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(init, this); }
	}

	class ProductionAirdrop : Production
	{
		public ProductionAirdrop(ActorInitializer init, ProductionAirdropInfo info)
			: base(init, info) { }

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var info = (ProductionAirdropInfo)Info;
			var owner = self.Owner;
			var map = owner.World.Map;
			var aircraftInfo = self.World.Map.Rules.Actors[info.ActorType].TraitInfo<AircraftInfo>();

			WPos startPos;
			WPos endPos;
			WAngle spawnFacing;

			var bounds = map.Bounds;
			var diagonalVec = map.ProjectedBottomRight - map.ProjectedTopLeft;

			if (info.BaselineSpawn)
			{
				var spawnPos = map.CenterOfCell(owner.HomeLocation);
				var spawnVec = spawnPos - (map.ProjectedTopLeft + diagonalVec / 2);
				startPos = spawnPos + map.DistanceToEdge(spawnPos, spawnVec).Length * spawnVec / spawnVec.Length;
				endPos = startPos;
				spawnFacing = (self.CenterPosition - startPos).Yaw;
			}
			else
			{
				// Start a fixed distance away: the longest straight line across the
				// map at a given angle. This makes the production timing independent
				// of spawnpoint.
				spawnFacing = info.Facing;
				var longestApproach = Math.Min(diagonalVec.Y * 1024 / spawnFacing.Cos(), diagonalVec.X * 1024 / spawnFacing.Sin());
				var spawnVec = new WVec(0, -longestApproach, 0).Rotate(WRot.FromYaw(spawnFacing));
				startPos = self.CenterPosition + spawnVec;
				endPos = self.CenterPosition - spawnVec;
			}

			// Assume a single exit point for simplicity
			var exit = self.Info.TraitInfos<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
				{
					owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
					return;
				}

				var actor = w.CreateActor(info.ActorType, new TypeDictionary
				{
					new CenterPositionInit(startPos + new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(spawnFacing)
				});

				var exitCell = self.Location + exit.ExitCell;
				actor.QueueActivity(new Land(actor, Target.FromActor(self), WDist.Zero, WVec.Zero, info.Facing, clearCells: new CPos[1] { exitCell }));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
					{
						owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
						return;
					}

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, productionType, inits));
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				actor.QueueActivity(new FlyOffMap(actor, Target.FromPos(endPos)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
