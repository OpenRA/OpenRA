#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("DateTime")]
	public class DateGlobal : ScriptGlobal
	{
		public DateGlobal(ScriptContext context)
			: base(context) { }

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
	}
}
