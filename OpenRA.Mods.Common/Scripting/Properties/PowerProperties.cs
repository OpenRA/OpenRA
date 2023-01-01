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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;
using PowerTrait = OpenRA.Mods.Common.Traits.Power;

namespace OpenRA.Mods.Common.Scripting
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
		public int PowerProvided => pm.PowerProvided;

		[Desc("Returns the power used by the player.")]
		public int PowerDrained => pm.PowerDrained;

		[Desc("Returns the player's power state " +
		      "(\"Normal\", \"Low\" or \"Critical\").")]
		public string PowerState => pm.PowerState.ToString();

		[Desc("Triggers low power for the chosen amount of ticks.")]
		public void TriggerPowerOutage(int ticks)
		{
			pm.TriggerPowerOutage(ticks);
		}
	}

	[ScriptPropertyGroup("Power")]
	public class ActorPowerProperties : ScriptActorProperties, Requires<PowerInfo>
	{
		readonly PowerTrait[] power;

		public ActorPowerProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			power = self.TraitsImplementing<PowerTrait>().ToArray();
		}

		[Desc("Returns the power drained/provided by this actor.")]
		public int Power
		{
			get { return power.Sum(p => p.GetEnabledPower()); }
		}
	}
}
