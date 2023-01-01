#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("MissionObjectives")]
	public class MissionObjectiveProperties : ScriptPlayerProperties, Requires<MissionObjectivesInfo>
	{
		readonly MissionObjectives mo;
		readonly bool shortGame;

		public MissionObjectiveProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			mo = player.PlayerActor.Trait<MissionObjectives>();
			shortGame = player.World.WorldActor.Trait<MapOptions>().ShortGame;
		}

		[ScriptActorPropertyActivity]
		[Desc("Add a mission objective for this player. The function returns the " +
			"ID of the newly created objective, so that it can be referred to later.")]
		public int AddObjective(string description, string type = "Primary", bool required = true)
		{
			return mo.Add(Player, description, type, required);
		}

		[ScriptActorPropertyActivity]
		[Desc("Add a primary mission objective for this player. The function returns the " +
			"ID of the newly created objective, so that it can be referred to later.")]
		public int AddPrimaryObjective(string description)
		{
			return AddObjective(description);
		}

		[ScriptActorPropertyActivity]
		[Desc("Add a secondary mission objective for this player. The function returns the " +
			"ID of the newly created objective, so that it can be referred to later.")]
		public int AddSecondaryObjective(string description)
		{
			return AddObjective(description, "Secondary", false);
		}

		[ScriptActorPropertyActivity]
		[Desc("Mark an objective as completed.  This needs the objective ID returned " +
			"by AddObjective as argument.  When this player has completed all primary " +
			"objectives, (s)he has won the game.")]
		public void MarkCompletedObjective(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			mo.MarkCompleted(Player, id);
		}

		[ScriptActorPropertyActivity]
		[Desc("Mark an objective as failed.  This needs the objective ID returned " +
			"by AddObjective as argument.  Secondary objectives do not have any " +
			"influence whatsoever on the outcome of the game.")]
		public void MarkFailedObjective(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			mo.MarkFailed(Player, id);
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns true if the objective has been successfully completed, false otherwise.")]
		public bool IsObjectiveCompleted(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			return mo.Objectives[id].State == ObjectiveState.Completed;
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns true if the objective has been failed, false otherwise.")]
		public bool IsObjectiveFailed(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			return mo.Objectives[id].State == ObjectiveState.Failed;
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns the description of an objective.")]
		public string GetObjectiveDescription(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			return mo.Objectives[id].Description;
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns the type of an objective.")]
		public string GetObjectiveType(int id)
		{
			if (id < 0 || id >= mo.Objectives.Count)
				throw new LuaException("Objective ID is out of range.");

			return mo.Objectives[id].Type;
		}

		[ScriptActorPropertyActivity]
		[Desc("Returns true if this player has lost all units/actors that have " +
			"the MustBeDestroyed trait (according to the short game option).")]
		public bool HasNoRequiredUnits()
		{
			return Player.HasNoRequiredUnits(shortGame);
		}
	}
}
