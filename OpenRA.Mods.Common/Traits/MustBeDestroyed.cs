#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actors with this trait must be destroyed for a game to end.")]
	public class MustBeDestroyedInfo : ITraitInfo
	{
		[Desc("In a short game only actors that have this value set to true need to be destroyed.")]
		public bool RequiredForShortGame = false;

		public object Create(ActorInitializer init) { return new MustBeDestroyed(this); }
	}

	public class MustBeDestroyed
	{
		public readonly MustBeDestroyedInfo Info;

		public MustBeDestroyed(MustBeDestroyedInfo info)
		{
			Info = info;
		}
	}
}
