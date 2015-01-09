#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class PlaneProperties : ScriptActorProperties, Requires<PlaneInfo>
	{
		public PlaneProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[ScriptActorPropertyActivity]
		[Desc("Fly within the cell grid.")]
		public void Move(CPos cell)
		{
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Return to the base, which is either the airfield given, or an auto-selected one otherwise.")]
		public void ReturnToBase(Actor airfield = null)
		{
			Self.QueueActivity(new ReturnToBase(Self, airfield));
		}
	}

	[ScriptPropertyGroup("Combat")]
	public class PlaneCombatProperties : ScriptActorProperties, Requires<AttackPlaneInfo>
	{
		public PlaneCombatProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Fly an attack against the target actor.")]
		public void Attack(Actor target)
		{
			Self.QueueActivity(new FlyAttack(Target.FromActor(target)));
		}
	}
}