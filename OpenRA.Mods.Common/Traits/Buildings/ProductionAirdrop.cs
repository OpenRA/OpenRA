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
	public enum EntryType { Fixed, PlayerSpawnClosestEdge, DropSiteClosestEdge }

	[Desc("Deliver the unit in production via skylift.")]
	public class ProductionAirdropInfo : ProductionInfo
	{
		[NotificationReference("Speech")]
		public readonly string ReadyAudio = "Reinforce";

		[FieldLoader.Require]
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used for delivery. Must have the `Aircraft` trait.")]
		public readonly string ActorType = null;

		[Desc("The delivery aircraft spawn and entry behaviour.")]
		public readonly EntryType EntryType = EntryType.Fixed;

		[Desc("Offset relative to the automatically calculated spawn point.")]
		public readonly CVec SpawnOffset = CVec.Zero;

		[Desc("Direction the aircraft should face to land.")]
		public readonly int Facing = 64;

		public override object Create(ActorInitializer init) { return new ProductionAirdrop(init, this); }
	}

	class ProductionAirdrop : Production
	{
		readonly AircraftInfo aircraftInfo;
		readonly ProductionAirdropInfo info;

		public ProductionAirdrop(ActorInitializer init, ProductionAirdropInfo info)
			: base(init, info)
		{
			aircraftInfo = init.Self.World.Map.Rules.Actors[info.ActorType].TraitInfo<AircraftInfo>();
			this.info = info;
		}

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var owner = self.Owner;
			var deliveryPath = GetDeliveryActorPathInfo(self);

			// Assume a single exit point for simplicity
			var exit = self.Info.TraitInfos<ExitInfo>().First();

			foreach (var tower in self.TraitsImplementing<INotifyDelivery>())
				tower.IncomingDelivery(self);

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
					return;

				var actor = w.CreateActor(info.ActorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(deliveryPath.EntryPosition) + new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(deliveryPath.SpawnFacing)
				});

				var exitCell = self.Location + exit.ExitCell;
				actor.QueueActivity(new Land(actor, Target.FromActor(self), WDist.Zero, WVec.Zero, deliveryPath.LandingFacing, clearCells: new CPos[1] { exitCell }));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
						return;

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, productionType, inits));
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				actor.QueueActivity(new FlyOffMap(actor, Target.FromCell(w, deliveryPath.ExitPosition)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}

		DeliveryActorPathInfo GetDeliveryActorPathInfo(Actor self)
		{
			var owner = self.Owner;
			var map = owner.World.Map;
			var bounds = map.Bounds;
			var mpStart = owner.World.WorldActor.TraitOrDefault<MPStartLocations>();

			CPos startPos;
			CPos endPos;
			int spawnFacing;
			WVec spawnDirection;

			switch (info.EntryType)
			{
				case EntryType.Fixed:
					// Start a fixed distance away: the width of the map.
					// This makes the production timing independent of spawnpoint
					var loc = self.Location.ToMPos(map);
					startPos = new MPos(loc.U + map.Bounds.Width, loc.V).ToCPos(map);
					endPos = new MPos(map.Bounds.Left, loc.V).ToCPos(map);
					spawnFacing = info.Facing;

					return new DeliveryActorPathInfo(startPos, endPos, spawnFacing, info.Facing);

				case EntryType.PlayerSpawnClosestEdge:
					if (mpStart == null)
						break;

					var spawn = mpStart.Start[owner];
					var center = new MPos(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToCPos(map);
					var spawnVec = spawn - center;
					startPos = spawn + spawnVec * (Exts.ISqrt((bounds.Height * bounds.Height + bounds.Width * bounds.Width) / (4 * spawnVec.LengthSquared)));
					endPos = startPos;
					spawnDirection = new WVec((self.Location - startPos).X, (self.Location - startPos).Y, 0);
					spawnFacing = spawnDirection.Yaw.Facing;

					return new DeliveryActorPathInfo(startPos, endPos, spawnFacing, info.Facing);

				case EntryType.DropSiteClosestEdge:
					startPos = self.World.Map.ChooseClosestEdgeCell(self.Location);

					spawnDirection = new WVec((self.Location - startPos).X, (self.Location - startPos).Y, 0);
					spawnFacing = spawnDirection.Yaw.Facing;

					bounds = self.World.Map.Bounds;
					if ((info.SpawnOffset.X != 0 && startPos.X != bounds.X && startPos.X != bounds.X + bounds.Width)
						|| (info.SpawnOffset.Y != 0 && startPos.Y != bounds.Y && startPos.Y != bounds.Y + bounds.Height))
						startPos += info.SpawnOffset;

					return new DeliveryActorPathInfo(startPos, startPos, spawnFacing, info.Facing);
			}

			throw new ArgumentOutOfRangeException("info.EntryType", info.EntryType, "Unsupported value for delivery actor entry type!");
		}

		class DeliveryActorPathInfo
		{
			public readonly CPos EntryPosition;
			public readonly CPos ExitPosition;
			public readonly int SpawnFacing;
			public readonly int LandingFacing;

			public DeliveryActorPathInfo(CPos entryPosition, CPos exitPosition, int spawnFacing, int landingFacing)
			{
				EntryPosition = entryPosition;
				ExitPosition = exitPosition;
				SpawnFacing = spawnFacing;
				LandingFacing = landingFacing;
			}
		}
	}
}
