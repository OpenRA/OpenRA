#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Deliver the unit in production via paradrop.")]
	public class ProductionParadropInfo : ProductionInfo, Requires<ExitInfo>
	{
		[Desc("Cargo aircraft used. Must have Aircraft trait.")]
		[ActorReference(typeof(AircraftInfo))] public readonly string ActorType = "badr";

		[Desc("Sound to play when dropping the unit.")]
		public readonly string ChuteSound = null;

		[Desc("Notification to play when dropping the unit.")]
		public readonly string ReadyAudio = null;

		public override object Create(ActorInitializer init) { return new ProductionParadrop(init, this); }
	}

	class ProductionParadrop : Production
	{
		readonly Lazy<RallyPoint> rp;

		public ProductionParadrop(ActorInitializer init, ProductionParadropInfo info)
			: base(init, info)
		{
			rp = Exts.Lazy(() => init.Self.IsDead ? null : init.Self.TraitOrDefault<RallyPoint>());
		}

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var owner = self.Owner;

			var exit = SelectExit(self, producee, productionType);

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var dropPos = exit != null ? self.Location + exit.ExitCell : self.Location;
			var startPos = dropPos + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, dropPos.Y);

			foreach (var notify in self.TraitsImplementing<INotifyDelivery>())
				notify.IncomingDelivery(self);

			var info = (ProductionParadropInfo)Info;
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

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, dropPos)));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
						return;

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit, productionType, inits));
					Game.Sound.Play(SoundType.World, info.ChuteSound, self.CenterPosition);
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				}));

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, endPos)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}

		public override void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string productionType, TypeDictionary inits)
		{
			var exit = CPos.Zero;
			var exitLocation = CPos.Zero;
			var target = Target.Invalid;

			var info = (ProductionParadropInfo)Info;
			var actorType = info.ActorType;

			var altitude = self.World.Map.Rules.Actors[actorType].TraitInfo<AircraftInfo>().CruiseAltitude;

			// Clone the initializer dictionary for the new actor
			var td = new TypeDictionary();
			foreach (var init in inits)
				td.Add(init);

			if (self.OccupiesSpace != null)
			{
				exit = self.Location + exitinfo.ExitCell;
				var spawn = self.World.Map.CenterOfCell(exit) + new WVec(WDist.Zero, WDist.Zero, altitude);
				var to = self.World.Map.CenterOfCell(exit);

				var initialFacing = exitinfo.Facing < 0 ? (to - spawn).Yaw.Facing : exitinfo.Facing;

				exitLocation = rp.Value != null ? rp.Value.Location : exit;
				target = Target.FromCell(self.World, exitLocation);

				td.Add(new LocationInit(exit));
				td.Add(new CenterPositionInit(spawn));
				td.Add(new FacingInit(initialFacing));
			}

			self.World.AddFrameEndTask(w =>
			{
				var newUnit = self.World.CreateActor(producee.Name, td);

				newUnit.QueueActivity(new Parachute(newUnit, newUnit.CenterPosition, self));
				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
				{
					if (exitinfo.MoveIntoWorld)
					{
						if (exitinfo.ExitDelay > 0)
							newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

						newUnit.QueueActivity(move.MoveIntoWorld(newUnit, exit));
						newUnit.QueueActivity(new AttackMoveActivity(newUnit, move.MoveTo(exitLocation, 1)));
					}
				}

				newUnit.SetTargetLine(target, rp.Value != null ? Color.Red : Color.Green, false);

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, exit);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType);

				foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
					t.BuildingComplete(newUnit);
			});
		}
	}
}