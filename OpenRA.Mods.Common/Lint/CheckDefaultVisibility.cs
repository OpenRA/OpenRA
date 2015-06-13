#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckDefaultVisibility : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			foreach (var actorInfo in map.Rules.Actors)
			{
				if (actorInfo.Key.StartsWith("^"))
					continue;

				var count = actorInfo.Value.Traits.WithInterface<IDefaultVisibilityInfo>().Count();

				if (count == 0)
					emitError("Actor type `{0}` does not define a default visibility type!".F(actorInfo.Key));
				else if (count > 1)
					emitError("Actor type `{0}` defines multiple default visibility types!".F(actorInfo.Key));
			}
		}
	}
}
