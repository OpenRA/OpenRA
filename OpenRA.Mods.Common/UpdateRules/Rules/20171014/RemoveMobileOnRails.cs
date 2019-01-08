#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveMobileOnRails : UpdateRule
	{
		public override string Name { get { return "Notify Mobile.OnRails removal"; } }
		public override string Description
		{
			get
			{
				return "The OnRails parameter on Mobile has been removed.\n" +
					"Actors that want to duplicate the left-right movement of the original Tiberian Dawn gunboat\n" +
					"should replace the Mobile and AttackTurreted traits with TDGunboat and AttackTDGunboatTurreted.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var removedOnRails = actorNode.ChildrenMatching("Mobile")
				.Sum(m => m.Value.Nodes.RemoveAll(n => n.Key == "OnRails"));

			if (removedOnRails == 0)
				yield break;

			yield return "Mobile.OnRails is no longer supported for actor type {0}.\n".F(actorNode.Key)
				+ "If you want to duplicate the left-right movement of the original Tiberian Dawn gunboat\n"
				+ "you must manually replace Mobile with the new TDGunboat trait, and AttackTurreted with AttackTDGunboatTurreted.";
		}
	}
}
