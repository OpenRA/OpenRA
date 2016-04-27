#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class MobileProperties : ScriptActorProperties, Requires<MobileInfo>
	{
		readonly Mobile mobile;

		public MobileProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			mobile = self.Trait<Mobile>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Moves within the cell grid. closeEnough defines an optional range " +
			"(in cells) that will be considered close enough to complete the activity.")]
		public void Move(CPos cell, int closeEnough = 0)
		{
			Self.QueueActivity(new Move(Self, cell, WDist.FromCells(closeEnough)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Moves within the cell grid, ignoring lane biases.")]
		public void ScriptedMove(CPos cell)
		{
			Self.QueueActivity(new Move(Self, cell));
		}

		[ScriptActorPropertyActivity]
		[Desc("Moves from outside the world into the cell grid.")]
		public void MoveIntoWorld(CPos cell)
		{
			Self.QueueActivity(mobile.MoveIntoWorld(Self, cell, mobile.ToSubCell));
		}

		[ScriptActorPropertyActivity]
		[Desc("Leave the current position in a random direction.")]
		public void Scatter()
		{
			mobile.Nudge(Self, Self, true);
		}

		[ScriptActorPropertyActivity]
		[Desc("Move to and enter the transport.")]
		public void EnterTransport(Actor transport)
		{
			Self.QueueActivity(new EnterTransport(Self, transport, 1, true));
		}

		[Desc("Whether the actor can move (false if immobilized).")]
		public bool IsMobile { get { return !mobile.IsTraitDisabled; } }
	}
}