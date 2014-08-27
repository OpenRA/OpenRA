#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class MobileProperties : ScriptActorProperties, Requires<MobileInfo>
	{
		public MobileProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[ScriptActorPropertyActivity]
		[Desc("Moves within the cell grid. closeEnough defines an optional range " +
			"(in cells) that will be considered close enough to complete the activity.")]
		public void Move(CPos cell, double closeEnough = 0.0)
		{
			self.QueueActivity(new Move.Move(self, cell, SubCell.Any, WRange.Zero, new WRange(closeEnough)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Moves within the cell grid, ignoring lane biases.")]
		public void ScriptedMove(CPos cell)
		{
			self.QueueActivity(new Move.Move(self, cell, SubCell.Any));
		}
	}
}