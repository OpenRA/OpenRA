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
using OpenRA.Mods.Common.Power;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Power")]
	public class PlayerPowerProperties : ScriptPlayerProperties, Requires<PowerManagerInfo>
	{
		readonly PowerManager pm;

		public PlayerPowerProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			pm = player.PlayerActor.Trait<PowerManager>();
		}

		[Desc("Returns the total of the power the player has.")]
		public int PowerProvided
		{
			get { return pm.PowerProvided; }
		}

		[Desc("Returns the power used by the player.")]
		public int PowerDrained
		{
			get { return pm.PowerDrained; }
		}

		[Desc("Returns the player's power state " +
			"(\"Normal\", \"Low\" or \"Critical\").")]
		public string PowerState
		{
			get { return pm.PowerState.ToString(); }
		}

		[Desc("Triggers low power for the chosen amount of ticks.")]
		public void TriggerPowerOutage(int ticks)
		{
			pm.TriggerPowerOutage(ticks);
		}
	}

	[ScriptPropertyGroup("Power")]
	public class ActorPowerProperties : ScriptActorProperties, Requires<PowerInfo>
	{
		readonly PowerInfo pi;

		public ActorPowerProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			pi = self.Info.Traits.GetOrDefault<PowerInfo>();
		}

		[Desc("Returns the power drained/provided by this actor.")]
		public int Power
		{
			get { return pi.Amount; }
		}
	}
}