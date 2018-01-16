#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckActors : ILintMapPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var actorTypes = map.ActorDefinitions.Select(a => a.Value.Value);
			foreach (var actor in actorTypes)
				if (!map.Rules.Actors.Keys.Contains(actor.ToLowerInvariant()))
					emitError("Actor {0} is not defined by any rule.".F(actor));
		}
	}
}