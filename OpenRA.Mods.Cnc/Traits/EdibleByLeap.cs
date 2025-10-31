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

using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Allows this actor to be the target of an attack leap.")]
	public class EdibleByLeapInfo : TraitInfo<EdibleByLeap> { }

	public class EdibleByLeap
	{
		Actor leaper;

		public bool CanLeap(Actor targeter)
		{
			return leaper == null || leaper.IsDead || leaper == targeter;
		}

		public bool GetLeapAtBy(Actor targeter)
		{
			if (leaper != null && !leaper.IsDead && leaper != targeter)
				return false;

			leaper = targeter;
			return true;
		}
	}
}
