#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class DefineSoundDefaults : UpdateRule
	{
		public override string Name { get { return "Move mod-specific sound defaults to yaml"; } }
		public override string Description
		{
			get
			{
				return "Mod-specific default sound values have been removed from several traits.\n" +
					"The original values are added back via yaml.";
			}
		}

		Tuple<string, string, string, List<MiniYamlNode>>[] fields =
		{
			Tuple.Create("ParaDrop", "ChuteSound", "chute1.aud", new List<MiniYamlNode>()),
			Tuple.Create("EjectOnDeath", "ChuteSound", "chute1.aud", new List<MiniYamlNode>()),
			Tuple.Create("ProductionParadrop", "ChuteSound", "chute1.aud", new List<MiniYamlNode>()),
			Tuple.Create("Building", "BuildSounds", "placbldg.aud, build5.aud", new List<MiniYamlNode>()),
			Tuple.Create("Building", "UndeploySounds", "cashturn.aud", new List<MiniYamlNode>())
		};

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			// Reset state for each mod/map
			foreach (var field in fields)
				field.Item4.Clear();

			yield break;
		}

		string BuildMessage(Tuple<string, string, string, List<MiniYamlNode>> field)
		{
			return "The default value for {0}.{1} has been removed.\n".F(field.Item1, field.Item2)
				+ "You may wish to explicitly define `{0}: {1}` at the following\n".F(field.Item2, field.Item3)
				+ "locations if the sound has not already been inherited from a parent definition.\n"
				+ UpdateUtils.FormatMessageList(field.Item4.Select(n => n.Location.ToString()));
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			foreach (var field in fields)
				if (field.Item4.Any())
					yield return BuildMessage(field);
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var field in fields)
			{
				foreach (var traitNode in actorNode.ChildrenMatching(field.Item1))
				{
					var node = traitNode.LastChildMatching(field.Item2);
					if (node == null)
						field.Item4.Add(traitNode);
				}
			}

			yield break;
		}
	}
}
