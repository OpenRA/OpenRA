#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Deliver the unit in production via paradrop.")]
	public class ProductionParadropInfo : ProductionInfo, Requires<ExitInfo>
	{
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used. Must have Aircraft trait.")]
		public readonly string ActorType = "badr";

		[Desc("Sound to play when dropping the unit.")]
		public readonly string ChuteSound = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when dropping the unit.")]
		public readonly string ReadyAudio = null;

		[Desc("Text notification to display when dropping the unit.")]
		public readonly string ReadyTextNotification = null;

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

		public override bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			var owner = self.Owner;

			var exit = SelectExit(self, producee, productionType);

			// Start a fixed distance away: the width of the map.
			// This makes the production timing independent of spawnpoint
			var dropPos = exit != null ? self.Location + exit.Info.ExitCell : self.Location;
			var startPos = dropPos + new CVec(owner.World.Map.Bounds.Width, 0);
			var endPos = new CPos(owner.World.Map.Bounds.Left - 5, dropPos.Y);

			foreach (var notify in self.TraitsImplementing<INotifyDelivery>())
				notify.IncomingDelivery(self);

			var info = (ProductionParadropInfo)Info;
			var actorType = info.ActorType;

			owner.World.AddFrameEndTask(w =>
			{
				if (!self.IsInWorld || self.IsDead)
				{
					owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
					return;
				}

				var altitude = self.World.Map.Rules.Actors[actorType].TraitInfo<AircraftInfo>().CruiseAltitude;
				var actor = w.CreateActor(actorType, new TypeDictionary
				{
					new CenterPositionInit(w.Map.CenterOfCell(startPos) + new WVec(WDist.Zero, WDist.Zero, altitude)),
					new OwnerInit(owner),
					new FacingInit(new WAngle(256)),
				});

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, dropPos)));
				actor.QueueActivity(new CallFunc(() =>
				{
					if (!self.IsInWorld || self.IsDead)
					{
						owner.PlayerActor.Trait<PlayerResources>().GiveCash(refundableValue);
						return;
					}

					foreach (var cargo in self.TraitsImplementing<INotifyDelivery>())
						cargo.Delivered(self);

					self.World.AddFrameEndTask(ww => DoProduction(self, producee, exit?.Info, productionType, inits));
					Game.Sound.Play(SoundType.World, info.ChuteSound, self.CenterPosition);
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(info.ReadyTextNotification, self.Owner);
				}));

				actor.QueueActivity(new Fly(actor, Target.FromCell(w, endPos)));
				actor.QueueActivity(new RemoveSelf());
			});

			return true;
		}

		public override void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string productionType, TypeDictionary inits)
		{
			var exit = CPos.Zero;
			var exitLocations = new List<CPos>();

			var info = (ProductionParadropInfo)Info;
			var actorType = info.ActorType;

			var altitude = self.World.Map.Rules.Actors[actorType].TraitInfo<AircraftInfo>().CruiseAltitude;

			// Clone the initializer dictionary for the new actor
			var td = new TypeDictionary();
			foreach (var init in inits)
				td.Add(init);

			if (self.OccupiesSpace != null)
			{
				exit = self.Location;
				if (exitinfo != null)
					exit += exitinfo.ExitCell;

				var spawn = self.World.Map.CenterOfCell(exit) + new WVec(WDist.Zero, WDist.Zero, altitude);
				var to = self.World.Map.CenterOfCell(exit);

				var initialFacing = (exitinfo != null && exitinfo.Facing.HasValue) ? exitinfo.Facing.Value : (to - spawn).Yaw;

				exitLocations = rp.Value != null && rp.Value.Path.Count > 0 ? rp.Value.Path : new List<CPos> { exit };

				td.Add(new LocationInit(exit));
				td.Add(new CenterPositionInit(spawn));
				td.Add(new FacingInit(initialFacing));
				td.Add(new CreationActivityDelayInit(exitinfo == null ? 0 : exitinfo.ExitDelay));
			}

			self.World.AddFrameEndTask(w =>
			{
				var newUnit = self.World.CreateActor(producee.Name, td);
				newUnit.Trait<Parachutable>().IgnoreActor = self;

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
					foreach (var cell in exitLocations)
						newUnit.QueueActivity(new AttackMoveActivity(newUnit, () => move.MoveTo(cell, 1, evaluateNearestMovableCell: true, targetLineColor: Color.OrangeRed)));

				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, exit);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType, td);
			});
		}
	}
}
