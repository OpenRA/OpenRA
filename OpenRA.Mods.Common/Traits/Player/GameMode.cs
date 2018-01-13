#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Defines a game mode and the condition that is being granted when it is active. Use this to enable",
		"different game modes/VictoryCondition traits on the same map. Attach this to the Player or World actor.")]
	public class GameModeInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Internal name used for this game mode.")]
		public readonly string InternalName = null;

		[FieldLoader.Require]
		[Desc("Name of the game mode as shown in the UI.")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The Condition being granted when this game mode is active.")]
		public readonly string Condition = null;

		[Desc("Set this to true if multiple cooperative players have a distinct set of " +
			"objectives that each of them has to complete to win the game. This is mainly " +
			"useful for multiplayer coop missions. Do not use this for skirmish team games.")]
		public readonly bool Cooperative = false;

		[Desc("If set to true, this setting causes the game to end immediately once the first " +
			"player (or team of cooperative players) fails or completes his objectives.  If " +
			"set to false, players that fail their objectives will stick around and become observers.")]
		public readonly bool EarlyGameOver = false;

		public object Create(ActorInitializer init) { return new GameMode(this); }
	}

	public class GameMode
	{
		public readonly GameModeInfo Info;

		public GameMode(GameModeInfo info)
		{
			Info = info;
		}
	}
}
