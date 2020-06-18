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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ChangeOwnerOnExitInfo : PausableConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Map player to use when the vehicle is left.")]
		public readonly string NewOwner = "Neutral";

		public override object Create(ActorInitializer init) { return new ChangeOwnerOnExit(this); }
	}

	public class ChangeOwnerOnExit : PausableConditionalTrait<ChangeOwnerOnExitInfo>, INotifyPassengerExited
	{
		public ChangeOwnerOnExit(ChangeOwnerOnExitInfo info)
			: base(info) { }

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			self.ChangeOwner(self.World.Players.First(p => p.InternalName == Info.NewOwner));
		}
	}
}
