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
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using System.Collections.Generic;
using System;
using System.Drawing;

namespace OpenRA.Mods.RA
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

		public override Actor DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string raceVariant)
		{
			var exit = self.Location + exitinfo.ExitCell;
			var spawn = self.CenterPosition + exitinfo.SpawnOffset;

			var td = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					new LocationInit(exit),
					new CenterPositionInit(spawn)
				};

			if (raceVariant != null)
				td.Add(new RaceInit(raceVariant));

			var newUnit = self.World.CreateActor(false, producee.Name, td);

			if (!self.IsDead)
				foreach (var t in self.TraitsImplementing<INotifyProduction>())
					t.UnitProduced(self, newUnit, exit);

			var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
			foreach (var notify in notifyOthers)
				notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);

			var bi = newUnit.Info.Traits.GetOrDefault<BuildableInfo>();
			if (bi != null && bi.InitialActivity != null)
				newUnit.QueueActivity(Game.CreateObject<Activity>(bi.InitialActivity));

			foreach (var t in newUnit.TraitsImplementing<INotifyBuildComplete>())
				t.BuildingComplete(newUnit);

			return newUnit;
		}

		public override bool Produce(Actor self, IEnumerable<ActorInfo> producees, string raceVariant)
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

				var cargoBay = a.TraitOrDefault<Cargo>();

				if (cargoBay == null)
				{
					throw new Exception("Invalid airdrop actor: missing trait <Cargo>");
				}

				foreach (var producee in producees)
				{
					var newUnit = DoProduction(self, producee, exit, raceVariant);
					cargoBay.Load(a, newUnit);
				}

				a.QueueActivity(new Fly(a, Target.FromCell(w, self.Location + new CVec(9, 0))));
				a.QueueActivity(new Land(Target.FromActor(self)));
				a.QueueActivity(new UnloadCargo(a, true, rp.Value.Location));
				a.QueueActivity(new Fly(a, Target.FromCell(w, endPos)));
				a.QueueActivity(new RemoveSelf());
			});

			return true;
		}
	}
}
