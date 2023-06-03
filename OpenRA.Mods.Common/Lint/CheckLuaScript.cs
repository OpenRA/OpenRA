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
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckLuaScript : ILintMapPass, ILintServerMapPass
	{
		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			CheckLuaScriptFileExistance(emitError, map.Package, modData.DefaultFileSystem, map.Rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			CheckLuaScriptFileExistance(emitError, map.Package, modData.DefaultFileSystem, mapRules);
		}

		static void CheckLuaScriptFileExistance(Action<string> emitError, IReadOnlyPackage package, IReadOnlyFileSystem fileSystem, Ruleset mapRules)
		{
			var luaScriptInfo = mapRules.Actors[SystemActors.World].TraitInfoOrDefault<LuaScriptInfo>();
			if (luaScriptInfo == null)
				return;

			foreach (var script in luaScriptInfo.Scripts)
				if (!package.Contains(script) && !fileSystem.Exists(script))
					emitError($"Lua script `{script}` does not exist.");
		}
	}
}
