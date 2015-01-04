#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using Eluant;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class CombatProperties : ScriptActorProperties, Requires<AttackBaseInfo>, Requires<IMoveInfo>
	{
		readonly IMove move;

		public CombatProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			move = self.Trait<IMove>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Seek out and attack nearby targets.")]
		public void Hunt()
		{
			Self.QueueActivity(new Hunt(Self));
		}

		[ScriptActorPropertyActivity]
		[Desc("Move to a cell, but stop and attack anything within range on the way. " +
			"closeEnough defines an optional range (in cells) that will be considered " +
			"close enough to complete the activity.")]
		public void AttackMove(CPos cell, int closeEnough = 0)
		{
			Self.QueueActivity(new AttackMoveActivity(Self, move.MoveTo(cell, closeEnough)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Patrol along a set of given waypoints.  The action is repeated by default, " +
			"and the actor will wait for `wait` ticks at each waypoint.")]
		public void Patrol(CPos[] waypoints, bool loop = true, int wait = 0)
		{
			foreach (var wpt in waypoints)
			{
				Self.QueueActivity(new AttackMoveActivity(Self, move.MoveTo(wpt, 2)));
				Self.QueueActivity(new Wait(wait));
			}

			if (loop)
				Self.QueueActivity(new CallFunc(() => Patrol(waypoints, loop, wait)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Patrol along a set of given waypoints until a condition becomes true. " +
			"The actor will wait for `wait` ticks at each waypoint.")]
		public void PatrolUntil(CPos[] waypoints, LuaFunction func, int wait = 0)
		{
			Patrol(waypoints, false, wait);

			var repeat = func.Call(Self.ToLuaValue(Context)).First().ToBoolean();
			if (repeat)
				using (var f = func.CopyReference() as LuaFunction)
					Self.QueueActivity(new CallFunc(() => PatrolUntil(waypoints, f, wait)));
		}
	}
}
