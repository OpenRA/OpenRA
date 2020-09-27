#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Tag trait for actors with `DeliversCash`.")]
	public class AcceptsDeliveredCashInfo : TraitInfo
	{
		[Desc("Accepted `DeliversCash` types. Leave empty to accept all types.")]
		public readonly HashSet<string> ValidTypes = new HashSet<string>();

		[Desc("Player relationships the owner of the delivering actor needs.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Play a randomly selected sound from this list when accepting cash.")]
		public readonly string[] Sounds = { };

		public override object Create(ActorInitializer init) { return new AcceptsDeliveredCash(init.Self, this); }
	}

	public class AcceptsDeliveredCash : INotifyCashTransfer
	{
		readonly AcceptsDeliveredCashInfo info;

		public AcceptsDeliveredCash(Actor self, AcceptsDeliveredCashInfo info)
		{
			this.info = info;
		}

		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor)
		{
			if (info.Sounds.Length > 0)
				Game.Sound.Play(SoundType.World, info.Sounds, self.World, self.CenterPosition);
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}
