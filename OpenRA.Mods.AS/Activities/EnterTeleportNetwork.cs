#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	class EnterTeleportNetwork : Enter
	{
		string type;

		public EnterTeleportNetwork(Actor self, Actor target, EnterBehaviour enterBehaviour, string type)
			: base(self, target, enterBehaviour)
		{
			this.type = type;
		}

		protected override bool CanReserve(Actor self)
		{
			return Target.Actor.IsValidTeleportNetworkUser(self);
		}

		protected override void OnInside(Actor self)
		{
			// entered the teleport network canal but the entrance is dead immediately.
			if (Target.Actor.IsDead || self.IsDead)
				return;

			// Find the primary teleport network exit.
			var pri = Target.Actor.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().First(x => x.Type == type).PrimaryActor;

			var exitinfo = pri.Info.TraitInfo<ExitInfo>();
			var rp = pri.TraitOrDefault<RallyPoint>();

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
			// Cancel that.
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
						if (rp != null)
							self.QueueActivity(new AttackMoveActivity(
								self, move.MoveTo(rp.Location, 1)));
						else
							self.QueueActivity(new AttackMoveActivity(
								self, move.MoveTo(exitLocation, 1)));
					}
				}

				self.SetTargetLine(dest, rp != null ? Color.Red : Color.Green, false);
			});
		}
	}
}
