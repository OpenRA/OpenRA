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
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Date")]
	public class DateGlobal : ScriptGlobal
	{
		public DateGlobal(ScriptContext context)
			: base(context) { }

		[Desc("True on the 31st of October.")]
		public bool IsHalloween
		{
			get { return DateTime.Today.Month == 10 && DateTime.Today.Day == 31; }
		}
	}

	[ScriptGlobal("Time")]
	public class TimeGlobal : ScriptGlobal
	{
		public TimeGlobal(ScriptContext context)
			: base(context) { }

		[Desc("Get the current game time (in ticks)")]
		public int GameTime
		{
			get { return context.World.WorldTick; }
		}
	}
}
