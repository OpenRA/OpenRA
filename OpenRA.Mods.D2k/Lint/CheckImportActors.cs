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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.D2k.UtilityCommands;

namespace OpenRA.Mods.D2k.Lint
{
	public class CheckImportActors : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			foreach (var actorData in D2kMapImporter.ActorDataByActorCode.Values)
			{
				if (!rules.Actors.ContainsKey(actorData.First))
					emitError("Undefined actor {0} in map import code.".F(actorData.First));
			}
		}
	}
}
