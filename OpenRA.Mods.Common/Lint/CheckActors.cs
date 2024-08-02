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
using System.Linq;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckActors : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			var scriptBindings = Game.ModData.ObjectCreator.GetTypesImplementing<ScriptGlobal>()
				.Select(t => Utility.GetCustomAttributes<ScriptGlobalAttribute>(t, true)[0].Name)
				.ToHashSet();
			foreach (var actor in map.ActorDefinitions)
			{
				var name = actor.Value.Value;
				if (!map.Rules.Actors.ContainsKey(name.ToLowerInvariant()))
					emitError($"Actor `{name}` is not defined by any rule.");

				if (scriptBindings.Contains(actor.Key))
					emitError($"Named actor `{actor.Key}` conflicts with a script global of the same name. Consider renaming the actor.");
			}
		}
	}
}
