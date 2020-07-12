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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	class ChangeTargetLineDelayToMilliseconds : UpdateRule
	{
		public override string Name { get { return "Changed DrawLineToTarget.Delay interpretation from ticks to milliseconds."; } }
		public override string Description
		{
			get
			{
				return "Going forward, the value of the `Delay` attribute of the `DrawLineToTarget` trait will be\n" +
					"interpreted as milliseconds instead of ticks.\n";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var dltt in actorNode.ChildrenMatching("DrawLineToTarget", includeRemovals: false))
			{
				var delayNode = dltt.LastChildMatching("Delay", false);
				if (delayNode != null)
				{
					int delay;
					if (Exts.TryParseIntegerInvariant(delayNode.Value.Value, out delay))
						delayNode.ReplaceValue((delay * 1000 / 25).ToString());
				}
			}

			yield break;
		}
	}
}
