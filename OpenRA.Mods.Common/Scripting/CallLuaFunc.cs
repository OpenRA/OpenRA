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

using System;
using Eluant;
using OpenRA.Activities;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Activities
{
	public sealed class CallLuaFunc : Activity, IDisposable
	{
		readonly ScriptContext context;
		LuaFunction function;

		public CallLuaFunc(LuaFunction function, ScriptContext context)
		{
			this.function = (LuaFunction)function.CopyReference();
			this.context = context;
		}

		public override bool Tick(Actor self)
		{
			try
			{
				function?.Call().Dispose();
			}
			catch (Exception ex)
			{
				context.FatalError(ex);
			}

			Dispose();
			return true;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			base.Cancel(self, keepQueue);
			Dispose();
			return;
		}

		public void Dispose()
		{
			if (function == null)
				return;

			function.Dispose();
			function = null;
		}
	}
}
