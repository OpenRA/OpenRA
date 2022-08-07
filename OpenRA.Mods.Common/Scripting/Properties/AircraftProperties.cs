#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class AircraftProperties : ScriptActorProperties, Requires<AircraftInfo>
	{
		readonly DockManager dockManager;
		readonly Rearmable rearmable;

		public AircraftProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			dockManager = self.TraitOrDefault<DockManager>();
			rearmable = self.TraitOrDefault<Rearmable>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Fly within the cell grid.")]
		public void Move(CPos cell)
		{
			Self.QueueActivity(new Fly(Self, Target.FromCell(Self.World, cell)));
		}

		[ScriptActorPropertyActivity]
		[Desc("Return to the base, which is either the destination given, or an auto-selected one otherwise.")]
		public void ReturnToBase(Dock destination = null)
		{
			Self.QueueActivity(new DockActivity(dockManager, rearmable, destination));
		}

		[ScriptActorPropertyActivity]
		[Desc("Queues a landing activity on the specified actor.")]
		public void Land(Actor landOn)
		{
			Self.QueueActivity(new Land(Self, Target.FromActor(landOn)));
		}
	}
}
