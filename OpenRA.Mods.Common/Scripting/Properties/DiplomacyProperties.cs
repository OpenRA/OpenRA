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
using Eluant;
using OpenRA.Network;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Diplomacy")]
	public class DiplomacyProperties : ScriptPlayerProperties
	{
		public DiplomacyProperties(ScriptContext context, Player player)
			: base(context, player) { }

		[Desc("Returns true if the player is allied with the other player.")]
		public bool IsAlliedWith(Player targetPlayer)
		{
			return player.IsAlliedWith(targetPlayer);
		}

		[Desc("Changes the current stance of the player against the target player. " +
			"Allowed keywords for new stance: Ally, Neutral, Enemy.")]
		public void SetStance(Player targetPlayer, string newStance)
		{
			var emergingStance = Enum<Stance>.Parse(newStance);
			player.SetStance(targetPlayer, emergingStance);
		}
	}
}