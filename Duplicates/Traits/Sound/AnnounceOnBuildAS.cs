#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Play the Build voice of this actor when trained.")]
	public class AnnounceOnBuildASInfo : ITraitInfo
	{
		[Desc("Voice to use when built/trained.")]
		[VoiceReference] public readonly string Voice = "Build";

		[Desc("Should the voice be played for the owner alone?")]
		public readonly bool OnlyToOwner = false;

		public object Create(ActorInitializer init) { return new AnnounceOnBuildAS(init.Self, this); }
	}

	public class AnnounceOnBuildAS : INotifyBuildComplete
	{
		readonly AnnounceOnBuildASInfo info;

		public AnnounceOnBuildAS(Actor self, AnnounceOnBuildASInfo info)
		{
			this.info = info;
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			if (info.OnlyToOwner && self.Owner != self.World.RenderPlayer)
				return;

			self.PlayVoice(info.Voice);
		}
	}
}
