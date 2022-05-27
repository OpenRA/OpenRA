#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can be used to make a unit partly uncontrollable by the player.")]
	public class RejectsOrdersInfo : ConditionalTraitInfo
	{
		[Desc("Explicit list of rejected orders. Leave empty to reject all minus those listed under Except.")]
		public readonly HashSet<string> Reject = new HashSet<string>();

		[Desc("List of orders that should *not* be rejected.",
			"Also overrides other instances of this trait's Reject fields.")]
		public readonly HashSet<string> Except = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new RejectsOrders(this); }
	}

	public class RejectsOrders : ConditionalTrait<RejectsOrdersInfo>
	{
		public HashSet<string> Reject => Info.Reject;
		public HashSet<string> Except => Info.Except;

		public RejectsOrders(RejectsOrdersInfo info)
			: base(info) { }
	}

	public static class RejectsOrdersExts
	{
		public static bool AcceptsOrder(this Actor self, string orderString)
		{
			var rejectsOrdersTraits = self.TraitsImplementing<RejectsOrders>().Where(Exts.IsTraitEnabled).ToArray();
			if (rejectsOrdersTraits.Length == 0)
				return true;

			var reject = rejectsOrdersTraits.SelectMany(t => t.Reject);
			var except = rejectsOrdersTraits.SelectMany(t => t.Except);

			return except.Contains(orderString) || (reject.Any() && !reject.Contains(orderString));
		}
	}
}
