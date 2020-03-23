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

	public enum ExitType { OppositeToEntry, StraightAhead, SameAsEntry, Closest }

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

		[Desc("The delivery aircraft map exit behaviour.")]
		public readonly ExitType ExitType = ExitType.OppositeToEntry;

		[Desc("Offset relative to the automatically calculated spawn point.")]
		public readonly CVec SpawnOffset = CVec.Zero;

		[Desc("Offset relative to the automatically calculated exit point.")]
		public readonly CVec ExitOffset = CVec.Zero;

		[Desc("Direction the aircraft should face to land. A value of -1 would mean to ignore facing and just land.")]
		public readonly int LandingFacing = -1;

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

				actor.QueueActivity(new CallFunc(() =>
				{
					var exitPosition = deliveryPath.CalculateExitPosition(actor);
					actor.QueueActivity(new FlyOffMap(actor, Target.FromCell(w, exitPosition)));
					actor.QueueActivity(new RemoveSelf());
				}));
			});

			return true;
		}

		DeliveryActorPathInfo GetDeliveryActorPathInfo(Actor self)
		{
			var owner = self.Owner;
			var map = owner.World.Map;
			var bounds = map.Bounds;
			var mpStart = owner.World.WorldActor.TraitOrDefault<MPStartLocations>();

			var startPos = CPos.Zero;
			var spawnFacing = 0;

			switch (info.EntryType)
			{
				case EntryType.Fixed:
					// Start a fixed distance away: the width of the map.
					// This makes the production timing independent of spawnpoint
					var loc = self.Location.ToMPos(map);
					startPos = new MPos(loc.U + map.Bounds.Width, loc.V).ToCPos(map);

					spawnFacing = GetSpawnFacing(self.Location, startPos);
					break;

				case EntryType.PlayerSpawnClosestEdge:
					if (mpStart == null)
						break;

					var spawn = mpStart.Start[owner];
					var center = new MPos(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToCPos(map);
					var spawnVec = spawn - center;
					startPos = spawn + spawnVec * (Exts.ISqrt((bounds.Height * bounds.Height + bounds.Width * bounds.Width) / (4 * spawnVec.LengthSquared)));

					spawnFacing = GetSpawnFacing(self.Location, startPos);
					break;

				case EntryType.DropSiteClosestEdge:
					startPos = self.World.Map.ChooseClosestEdgeCell(self.Location);

					spawnFacing = GetSpawnFacing(self.Location, startPos);

					bounds = self.World.Map.Bounds;
					if (info.SpawnOffset.X != 0 && startPos.X != bounds.X && startPos.X != bounds.X + bounds.Width)
						startPos += new CVec(info.SpawnOffset.X, 0);

					if (info.SpawnOffset.Y != 0 && startPos.Y != bounds.Y && startPos.Y != bounds.Y + bounds.Height)
						startPos += new CVec(0, info.SpawnOffset.Y);
					break;
			}

			var exitPositionFunc = GetExitPositionFunc(self, startPos, spawnFacing);

			return new DeliveryActorPathInfo(startPos, spawnFacing, info.LandingFacing, exitPositionFunc);
		}

		static int GetSpawnFacing(CPos targetLocation, CPos spawnLocation)
		{
			var direction = targetLocation - spawnLocation;
			var spawnDirection = new WVec(direction.X, direction.Y, 0);
			return spawnDirection.Yaw.Facing;
		}

		Func<Actor, CPos> GetExitPositionFunc(Actor self, CPos startPos, int spawnFacing)
		{
			Func<Actor, CPos> exitPositionFunc = actor => CPos.Zero;
			switch (info.ExitType)
			{
				case ExitType.OppositeToEntry:
					exitPositionFunc = actor =>
					{
						var rotation = WRot.FromFacing(spawnFacing);
						var delta = new WVec(0, -1024, 0).Rotate(rotation);
						var finishEdge = actor.CenterPosition + self.World.Map.DistanceToEdge(actor.CenterPosition, delta).Length * delta / 1024;
						return new CPos(finishEdge.X / 1024, finishEdge.Y / 1024, 0);
					};

					break;

				case ExitType.StraightAhead:
					exitPositionFunc = actor =>
					{
						var rotation = WRot.FromFacing(actor.Orientation.Yaw.Facing);
						var delta = new WVec(0, -1024, 0).Rotate(rotation);
						var finishEdge = actor.CenterPosition + self.World.Map.DistanceToEdge(actor.CenterPosition, delta).Length * delta / 1024;
						return new CPos(finishEdge.X / 1024, finishEdge.Y / 1024, 0);
					};

					break;

				case ExitType.SameAsEntry:
					exitPositionFunc = actor => startPos;
					break;

				case ExitType.Closest:
					exitPositionFunc = actor =>
					{
						// By the time the func gets executed the map bounds could have changed, so fetching them fresh.
						var mapBounds = self.World.Map.Bounds;
						var exitCell = self.World.Map.ChooseClosestEdgeCell(actor.Location);
						if (info.ExitOffset.X != 0 && exitCell.X != mapBounds.X && exitCell.X != mapBounds.X + mapBounds.Width)
							exitCell += new CVec(info.ExitOffset.X, 0);

						if (info.ExitOffset.Y != 0 && exitCell.Y != mapBounds.Y && exitCell.Y != mapBounds.Y + mapBounds.Height)
							exitCell += new CVec(0, info.ExitOffset.Y);

						return exitCell;
					};

					break;
			}

			return exitPositionFunc;
		}

		class DeliveryActorPathInfo
		{
			public readonly CPos EntryPosition;
			public readonly int SpawnFacing;
			public readonly int LandingFacing;
			public readonly Func<Actor, CPos> CalculateExitPosition;

			public DeliveryActorPathInfo(CPos entryPosition, int spawnFacing, int landingFacing, Func<Actor, CPos> calculateExitPosition)
			{
				EntryPosition = entryPosition;
				SpawnFacing = spawnFacing;
				LandingFacing = landingFacing;
				CalculateExitPosition = calculateExitPosition;
			}
		}
	}
}
