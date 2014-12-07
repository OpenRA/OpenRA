#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Common.UtilityCommands
{
	static class UpgradeSequences
	{
		internal static void UpgradeActorSequences(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Tick is now measured in real game ticks not miliseconds
				if (engineVersion < 20141207)
				{
					if (node.Key == "Tick")
					{
						var value = int.Parse(node.Value.Value);
						var tick = value / 40;
						if (tick <= 0)
							tick = 1;

						node.Value.Value = tick.ToString();
					}
				}

				UpgradeActorSequences(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}
	}
}
