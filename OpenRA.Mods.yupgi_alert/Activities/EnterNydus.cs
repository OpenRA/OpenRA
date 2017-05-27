#region Copyright & License Information
/*
 * Modded by Boolbada of OP mod, from Engineer repair enter activity.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.yupgi_alert.Traits;

namespace OpenRA.Mods.yupgi_alert.Activities
{
	class EnterNydus : Enter
	{
		public EnterNydus(Actor self, Actor target, EnterBehaviour enterBehaviour)
			: base(self, target, enterBehaviour)
		{
		}

		protected override bool CanReserve(Actor self)
		{
			// Primary building is where you come out!
			return !Target.Actor.IsPrimaryNydusExit();
		}

		protected override void OnInside(Actor self)
		{
			if (Target.Actor.IsDead)
				// entered the nydus canal but the entrance is dead immediately. haha;;
				return;

			// Find the primary nydus exit.
			var pri = self.Owner.PlayerActor.Trait<NydusCounter>().PrimaryActor;
			if (pri == null)
				// Unfortunately, primary exit is killed for some reason and not exists at this time.
				return;

			var exitinfo = pri.Info.TraitInfo<ExitInfo>();
			var rp = pri.TraitOrDefault<RallyPoint>();

			// I took these code from Production.cs:DoPrudiction,
			// as exiting nydus canal is just like production. (of used product Kappa)
			var exit = CPos.Zero; // spawn point
			var exitLocation = CPos.Zero; // dest to move (cell pos)
			var dest = Target.Invalid; // destination to move (in Target)

			if (pri.OccupiesSpace != null)
			{
				exit = pri.Location + exitinfo.ExitCell;
				var spawn = pri.CenterPosition + exitinfo.SpawnOffset;
				var to = self.World.Map.CenterOfCell(exit);

				var initialFacing = exitinfo.Facing;
				if (exitinfo.Facing < 0)
				{
					var delta = to - spawn;
					if (delta.HorizontalLengthSquared == 0)
						initialFacing = 0;
					else
						initialFacing = delta.Yaw.Facing;

					var fi = self.TraitOrDefault<IFacing>();
					if (fi != null)
						fi.Facing = initialFacing;
				}

				exitLocation = rp != null ? rp.Location : exit;
				dest = Target.FromCell(self.World, exitLocation);
			}

			// Teleport myself to primary actor.
			self.Trait<IPositionable>().SetPosition(self, exit);

			// self still have enter-exit on its mind. ('cos enternydus implements enter behav.)
			// Cancle that.
			this.Done(self);

			// Cancel all activities (like PortableChrono does)
			self.CancelActivity();

			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				var move = self.TraitOrDefault<IMove>();
				if (move != null)
				{
					if (exitinfo.MoveIntoWorld)
					{
						// Exit delay is ignored.
						//if (exitinfo.ExitDelay > 0)
						//	newUnit.QueueActivity(new Wait(exitinfo.ExitDelay, false));

						self.QueueActivity(new AttackMoveActivity(
							self, move.MoveTo(exitLocation, 1)));
					}
				}

				self.SetTargetLine(dest, rp != null ? Color.Red : Color.Green, false);
			});
		}
	}
}
