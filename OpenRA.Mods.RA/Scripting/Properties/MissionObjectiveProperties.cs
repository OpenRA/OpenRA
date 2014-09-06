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
using OpenRA.Traits;
using OpenRA.Scripting;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("MissionObjectives")]
	public class MissionObjectiveProperties : ScriptPlayerProperties
	{
		readonly MissionObjectives mo;

		public MissionObjectiveProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			mo = player.PlayerActor.Trait<MissionObjectives>();
		}

		[ScriptActorPropertyActivity]
		[Desc("Add a primary mission objective for this player. The function returns the " +
		      "ID of the newly created objective, so that it can be referred to later.")]
		public int AddPrimaryObjective(string description)
		{
			return mo.Add(player, description, ObjectiveType.Primary);
		}

		[ScriptActorPropertyActivity]
		[Desc("Add a secondary mission objective for this player. The function returns the " +
		      "ID of the newly created objective, so that it can be referred to later.")]
		public int AddSecondaryObjective(string description)
		{
			return mo.Add(player, description, ObjectiveType.Secondary);
		}

		[ScriptActorPropertyActivity]
		[Desc("Mark an objective as completed.  This needs the objective ID returned " +
			"by AddObjective as argument.  When the player has completed all primary " +
			"objectives, (s)he has won the game.")]
		public void MarkCompletedObjective(int id)
		{
			mo.MarkCompleted(player, id);
		}

		[ScriptActorPropertyActivity]
		[Desc("Mark an objective as failed.  This needs the objective ID returned " +
			"by AddObjective as argument.  Secondary objectives do not have any " +
			"influence whatsoever on the outcome of the game.")]
		public void MarkFailedObjective(int id)
		{
			mo.MarkFailed(player, id);
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns true if the player has lost all units/actors that have the MustBeDestroyed trait.")]
		public bool HasNoRequiredUnits()
		{
			return player.HasNoRequiredUnits();
		}
	}
}
