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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class IgnoreAbstractActors : UpdateRule
	{
		readonly Dictionary<string, List<MiniYamlNode>> actors = new Dictionary<string, List<MiniYamlNode>>();

		public override string Name { get { return "Abstract actors are ignored while parsing rules"; } }
		public override string Description
		{
			get
			{
				return "Actor ids starting with '^' are now reserved for abstract inheritance templates.\n" +
					"Definitions that may be affected are listed for inspection so that they can be renamed if necessary.";
			}
		}

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			actors.Clear();
			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (!actorNode.Key.StartsWith("^", StringComparison.Ordinal))
				yield break;

			var name = actorNode.Key;
			if (!actors.ContainsKey(name))
				actors[name] = new List<MiniYamlNode>();

			actors[name].Add(actorNode);
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (actors.Any())
				yield return "Actor ids starting with '^' are now reserved for abstract\n" +
					"inheritance templates, and will not be parsed by the game.\n" +
					"Check the following definitions and rename them if they are not used for inheritance:\n" +
					UpdateUtils.FormatMessageList(actors.Select(n => n.Key + " (" + n.Value.Select(v => v.Location.Filename).JoinWith(", ") + ")"));
		}
	}
}
