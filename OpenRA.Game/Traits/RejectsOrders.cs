#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits
{
	[Desc("Can be used to make a unit partly uncontrollable by the player.")]
	public class RejectsOrdersInfo : ITraitInfo
	{
		[Desc("Possible values include Attack, AttackMove, Guard, Move.")]
		public readonly string[] Except = { };

		public object Create(ActorInitializer init) { return new RejectsOrders(this); }
	}

	public class RejectsOrders
	{
		public string[] Except { get { return info.Except; } }

		readonly RejectsOrdersInfo info;

		public RejectsOrders(RejectsOrdersInfo info)
		{
			this.info = info;
		}
	}

	public static class RejectsOrdersExts
	{
		public static bool AcceptsOrder(this Actor self, string orderString)
		{
			var r = self.TraitOrDefault<RejectsOrders>();
			return r == null || r.Except.Contains(orderString);
		}
	}
}
