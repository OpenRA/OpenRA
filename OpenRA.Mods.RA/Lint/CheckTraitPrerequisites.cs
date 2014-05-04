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
	public class CheckTraitPrerequisites : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			foreach (var actorInfo in map.Rules.Actors.Where(a => !a.Key.StartsWith("^")))
				try
				{
					var traits = actorInfo.Value.TraitsInConstructOrder().ToArray();
					if (traits.Length == 0)
						emitWarning("Actor {0} has no traits. Is this intended?".F(actorInfo.Key));
				}
				catch(Exception e)
				{
					emitError("Actor {0} is not constructible; failure: {1}".F(actorInfo.Key, e.Message));
				}
		}
	}
}