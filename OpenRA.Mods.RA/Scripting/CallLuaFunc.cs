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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class CallLuaFunc : Activity
	{
		LuaFunction function;
		public CallLuaFunc(LuaFunction func)
		{
			function = func.CopyReference() as LuaFunction;
		}

		public override Activity Tick(Actor self)
		{
			if (function != null)
				function.Call().Dispose();

			Dispose();
			return NextActivity;
		}

		public override void Cancel(Actor self)
		{
			Dispose();
			base.Cancel(self);
		}

		public void Dispose()
		{
			if (function == null)
				return;

			GC.SuppressFinalize(this);
			function.Dispose();
			function = null;
		}

		~CallLuaFunc()
		{
			if (function != null)
				Game.RunAfterTick(Dispose);
		}
	}
}
