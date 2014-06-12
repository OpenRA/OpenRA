#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Combat")]
	public class CombatProperties : ScriptActorProperties, Requires<AttackBaseInfo>, Requires<IMoveInfo>
	{
		public CombatProperties(Actor self) : base(self) { }

		[ScriptActorPropertyActivity]
		[Desc("Seek out and attack nearby targets.")]
		public void Hunt()
		{
			self.QueueActivity(new Hunt(self));
		}

		[ScriptActorPropertyActivity]
		[Desc("Move to a cell, but stop and attack anything within range on the way. " +
			"closeEnough defines an optional range (in cells) that will be considered " +
			"close enough to complete the activity.")]
		public void AttackMove(CPos cell, int closeEnough = 0)
		{
			self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Move.Move(cell, WRange.FromCells(closeEnough))));
		}
	}
}