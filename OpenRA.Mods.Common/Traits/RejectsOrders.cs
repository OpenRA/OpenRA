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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be used to make a unit partly uncontrollable by the player.")]
	public class RejectsOrdersInfo : UpgradableTraitInfo
	{
		[Desc("Possible values include Attack, AttackMove, Guard, Move.")]
		public readonly HashSet<string> Except = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new RejectsOrders(this); }
	}

	public class RejectsOrders : UpgradableTrait<RejectsOrdersInfo>
	{
		public HashSet<string> Except { get { return Info.Except; } }

		public RejectsOrders(RejectsOrdersInfo info)
			: base(info) { }
	}

	public static class RejectsOrdersExts
	{
		public static bool AcceptsOrder(this Actor self, string orderString)
		{
			var r = self.TraitOrDefault<RejectsOrders>();
			return r == null || r.IsTraitDisabled || r.Except.Contains(orderString);
		}
	}
}
