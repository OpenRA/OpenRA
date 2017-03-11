#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using Eluant;

namespace OpenRA.Scripting
{
	public sealed class AIScriptContext : ScriptContext
	{
		public AIScriptContext(World world, IEnumerable<string> scripts)
			: base(world, null, scripts)
		{
		}

		public void ActivateAI(string factionName, string internalName)
		{
			if (FatalErrorOccurred)
				return;

			using (var activateAI = (LuaFunction)Runtime.Globals["ActivateAI"])
			{
				activateAI.Call(factionName, internalName).Dispose();
			}
		}

		public LuaVararg CallLuaFunc(string func, params LuaValue[] args)
		{
			if (FatalErrorOccurred)
				return null;

			using (var f = (LuaFunction)Runtime.Globals[func])
				return f.Call(args);
		}
	}
}
