#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Play the Build voice of this actor when trained.")]
	public class AnnounceOnBuildInfo : TraitInfo<AnnounceOnBuild> { }

	public class AnnounceOnBuild : INotifyBuildComplete
	{
		public void BuildingComplete(Actor self)
		{
			foreach (var voiced in self.TraitsImplementing<IVoiced>())
				voiced.PlayVoice("Build", self, self.Owner.Country.Race);
		}
	}
}
