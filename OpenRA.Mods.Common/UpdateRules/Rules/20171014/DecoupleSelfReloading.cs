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
	public class DecoupleSelfReloading : UpdateRule
	{
		public override string Name { get { return "Replace 'SelfReloads' with 'ReloadAmmoPool'"; } }
		public override string Description
		{
			get
			{
				return "'SelfReloads', 'SelfReloadDelay', 'ReloadCount', 'ResetOnFire'\n" +
					"and 'RearmSound' were renamed and moved from 'AmmoPool' to a new 'ReloadAmmoPool' trait.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var poolNumber = 0;
			var ammoPools = actorNode.ChildrenMatching("AmmoPool").ToList();
			foreach (var pool in ammoPools)
			{
				var selfReloads = pool.LastChildMatching("SelfReloads");
				if (selfReloads == null || !selfReloads.NodeValue<bool>())
					continue;

				poolNumber++;
				var reloadOnCond = new MiniYamlNode("ReloadAmmoPool@" + poolNumber, "");

				var name = pool.LastChildMatching("Name");
				if (name != null)
					reloadOnCond.AddNode("AmmoPool", name.NodeValue<string>());

				var selfReloadDelay = pool.LastChildMatching("SelfReloadDelay");
				if (selfReloadDelay != null)
				{
					selfReloadDelay.RenameKey("Delay");
					reloadOnCond.AddNode(selfReloadDelay);
					pool.RemoveNodes("SelfReloadDelay");
				}

				pool.RemoveNodes("SelfReloads");

				var reloadCount = pool.LastChildMatching("ReloadCount");
				if (reloadCount != null)
				{
					reloadCount.RenameKey("Count");
					reloadOnCond.AddNode(reloadCount);
					pool.RemoveNodes("ReloadCount");
				}

				var reset = pool.LastChildMatching("ResetOnFire");
				if (reset != null)
				{
					reloadOnCond.AddNode(reset);
					pool.RemoveNodes("ResetOnFire");
				}

				var rearmSound = pool.LastChildMatching("RearmSound");
				if (rearmSound != null)
				{
					rearmSound.RenameKey("Sound");
					reloadOnCond.AddNode(rearmSound);
					pool.RemoveNodes("RearmSound");
				}

				actorNode.AddNode(reloadOnCond);
			}

			yield break;
		}
	}
}
