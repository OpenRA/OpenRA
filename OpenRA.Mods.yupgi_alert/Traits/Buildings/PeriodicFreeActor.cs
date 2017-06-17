#region Copyright & License Information
/*
 * Modded from FreeActor and Production.
 * Modded by Boolbada of OP Mod
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

/* Works without other modules or engine modification! */

using System.Drawing;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Player receives a unit for free once the building is placed.",
		"If you want more than one unit to be delivered, copy this section and assign IDs like FreeActorWithDelivery@2, ...")]
	public class PeriodicFreeActorInfo : FreeActorInfo
	{
		[Desc("Period of the actor spawn in ticks")]
		public readonly int Period = 250;

		[Desc("Color of the progress bar")]
		public readonly Color Color = Color.Blue;

		public override object Create(ActorInitializer init) { return new PeriodicFreeActor(init, this); }
	}

	public class PeriodicFreeActor : ITick, ISelectionBar
	{
		readonly PeriodicFreeActorInfo info;
		readonly Actor self;
		readonly RallyPoint rp;

		int ticks = 0;

		public PeriodicFreeActor(ActorInitializer init, PeriodicFreeActorInfo info)
		{
			self = init.Self;
			this.info = info;
			rp = self.TraitOrDefault<RallyPoint>();
		}

		void CreateActor(Actor self)
		{
			var exitinfo = self.Info.TraitInfos<ExitInfo>().Random(self.World.SharedRandom);
			var exit = self.Location + exitinfo.ExitCell;
			var rpLocation = rp != null ? rp.Location : exit;

			self.World.AddFrameEndTask(w =>
			{
				var newUnit = w.CreateActor(info.Actor, new TypeDictionary
				{
					new ParentActorInit(self),
					new CenterPositionInit(self.CenterPosition + exitinfo.SpawnOffset),
					new OwnerInit(self.Owner),
					new FacingInit(info.Facing),
				});

				var move = newUnit.TraitOrDefault<IMove>();
				if (move != null)
				{
					newUnit.SetTargetLine(Target.FromCell(self.World, rpLocation), rp != null ? Color.Red : Color.Green, false);

					if (exitinfo.MoveIntoWorld)
					{
						if (exitinfo.ExitDelay > 0)
							newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

						newUnit.QueueActivity(move.MoveIntoWorld(newUnit, exit));
						newUnit.QueueActivity(new AttackMoveActivity(
							newUnit, move.MoveTo(rpLocation, 1)));
					}
				}

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit);
			});
		}

		void ITick.Tick(Actor self)
		{
			if (ticks++ < info.Period)
				return;

			ticks = 0;
			CreateActor(self);
		}

		float ISelectionBar.GetValue()
		{
			return (float)ticks / info.Period;
		}

		Color ISelectionBar.GetColor()
		{
			return Color.Blue;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
