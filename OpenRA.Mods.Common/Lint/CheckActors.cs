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

namespace OpenRA.Mods.Common.Lint
{
	public class CheckActors : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			var actorTypes = map.ActorDefinitions.Select(a => a.Value.Value);
			foreach (var actor in actorTypes)
				if (!map.Rules.Actors.Keys.Contains(actor.ToLowerInvariant()))
					emitError($"Actor {actor} is not defined by any rule.");
		}
	}
}
