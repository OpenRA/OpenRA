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

using System;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("DateTime")]
	public class DateGlobal : ScriptGlobal
	{
		readonly TimeLimitManager tlm;

		public DateGlobal(ScriptContext context)
			: base(context)
		{
			tlm = context.World.WorldActor.TraitOrDefault<TimeLimitManager>();
		}

		[Desc("True on the 31st of October.")]
		public bool IsHalloween
		{
			get { return DateTime.Today.Month == 10 && DateTime.Today.Day == 31; }
		}

		[Desc("Get the current game time (in ticks).")]
		public int GameTime
		{
			get { return Context.World.WorldTick; }
		}

		[Desc("Converts the number of seconds into game time (ticks).")]
		public int Seconds(int seconds)
		{
			return seconds * 25;
		}

		[Desc("Converts the number of minutes into game time (ticks).")]
		public int Minutes(int minutes)
		{
			return Seconds(minutes * 60);
		}

		[Desc("Return or set the time limit (in ticks). When setting, the time limit will count from now. Setting the time limit to 0 will disable it.")]
		public int TimeLimit
		{
			get
			{
				return tlm != null ? tlm.TimeLimit : 0;
			}

			set
			{
				if (tlm != null)
					tlm.TimeLimit = value == 0 ? 0 : value + GameTime;
				else
					throw new LuaException("Cannot set TimeLimit, TimeLimitManager trait is missing.");
			}
		}

		[Desc("The notification string used for custom time limit warnings. See the TimeLimitManager trait documentation for details.")]
		public string TimeLimitNotification
		{
			get
			{
				return tlm != null ? tlm.Notification : null;
			}

			set
			{
				if (tlm != null)
					tlm.Notification = value;
				else
					throw new LuaException("Cannot set TimeLimitNotification, TimeLimitManager trait is missing.");
			}
		}
	}
}
