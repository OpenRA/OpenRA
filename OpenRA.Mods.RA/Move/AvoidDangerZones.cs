#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using System.Diagnostics;

namespace OpenRA.Mods.RA.Move
{
	class AvoidDangerZonesInfo : ITraitInfo, Requires<MobileInfo>
	{
		public object Create(ActorInitializer init) { return new AvoidDangerZones(init.self, this); }
	}

	class AvoidDangerZones : INotifyDangerZoneCreatedNearby
	{
		readonly AvoidDangerZonesInfo Info;

		public AvoidDangerZones(Actor self, AvoidDangerZonesInfo info)
		{
			Info = info;
		}

		public bool ShouldAvoid(Actor self, DangerZone zone)
		{
			// Don't avoid danger zones created by the enemy:
			return self.Owner.Stances[zone.CreatedBy.Owner] != Stance.Enemy;
		}

		public void OnNotifyDangerZoneCreatedNearby(Actor self, DangerZone zone)
		{
			// Should we avoid this new zone?
			if (!ShouldAvoid(self, zone)) return;

			Activity nextInQueue = null;

			// If we're not idle, assume we're moving somewhere else:
			if (self.IsIdle) goto scatter;
			
			var act = self.GetCurrentActivity();
			Debug.Assert(act != null);

			var actType = act.GetType();
			// FIXME:
			if (actType == typeof(Wait)) goto scatter;
			// Requeue the attack for after the move:
			if (actType == typeof(Attack))
			{
				nextInQueue = ((Attack)act).Clone();
				goto scatter;
			}
			// Requeue the original move for after the immediate move:
			if (actType == typeof(Move))
			{
				nextInQueue = ((Move)act).Clone();
				goto scatter;
			}
			return;

		scatter:
			// Find a safety cell that's the shortest distance from our location out of the blast radius:
			var zoneToMe = (PVecFloat)(self.CenterLocation - zone.PixelLocation);
			var zoneToMeLength = zoneToMe.Length;

			CPos safeCell;
			if (zoneToMeLength > 0f)
			{
				var safePos = self.CenterLocation + (PVecInt)((zoneToMe * (0.80f * zone.PixelRadius)) / zoneToMeLength);
				safeCell = safePos.ToCPos();
			}
			else
			{
				safeCell = self.Location;
			}

			// Cancel the current activity and move the unit towards safety:
			var mobile = self.Trait<Mobile>();
			safeCell = mobile.NearestMoveableCell(safeCell);

			self.CancelActivity();
			self.QueueActivity(mobile.MoveTo(safeCell, 0));
			self.SetTargetLine(Target.FromCell(safeCell), System.Drawing.Color.DodgerBlue, false);

			// Try to obey the last order:
			if (nextInQueue != null)
				self.QueueActivity(nextInQueue);
		}
	}
}
