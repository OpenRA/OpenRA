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
	[Desc("Play the Build voice of this actor when trained.")]
	public class AnnounceOnBuildInfo : ITraitInfo
	{
		[Desc("Voice to use when built/trained.")]
		[VoiceReference] public readonly string Voice = "Build";

		[Desc("Should the voice be played for the owner alone?")]
		public readonly bool OnlyToOwner = false;

		public object Create(ActorInitializer init) { return new AnnounceOnBuild(init.Self, this); }
	}

	public class AnnounceOnBuild : INotifyBuildComplete
	{
		readonly AnnounceOnBuildInfo info;

		public AnnounceOnBuild(Actor self, AnnounceOnBuildInfo info)
		{
			this.info = info;
		}

		public void BuildingComplete(Actor self)
		{
			if (info.OnlyToOwner && self.Owner != self.World.RenderPlayer)
				return;

			self.PlayVoice(info.Voice);
		}
	}
}
