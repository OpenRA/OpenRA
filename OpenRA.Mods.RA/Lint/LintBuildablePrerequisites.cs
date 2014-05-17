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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class LintBuildablePrerequisites : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			var providedPrereqs = map.Rules.Actors.Keys.Concat(
				map.Rules.Actors.SelectMany(a => a.Value.Traits
					.WithInterface<ProvidesCustomPrerequisiteInfo>()
					.Select(p => p.Prerequisite))).ToArray();

			// TODO: this check is case insensitive while the real check in-game is not
			foreach (var i in map.Rules.Actors)
			{
				var bi = i.Value.Traits.GetOrDefault<BuildableInfo>();
				if (bi != null)
					foreach (var prereq in bi.Prerequisites)
						if (!providedPrereqs.Contains(prereq.Replace("!", "")))
							emitError("Buildable actor {0} has prereq {1} not provided by anything.".F(i.Key, prereq));
			}
		}
	}
}
