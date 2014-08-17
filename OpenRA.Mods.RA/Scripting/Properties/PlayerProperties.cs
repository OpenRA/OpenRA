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
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class PlayerProperties : ScriptPlayerProperties
	{
		readonly Player p;

		public PlayerProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			p = player;
		}

		[Desc("The player's name.")]
		public string PlayerName { get { return p.PlayerName; } }
	}
}
