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
	public class DefineSoundDefaults : UpdateRule
	{
		public override string Name { get { return "Move mod-specific sound defaults to yaml"; } }
		public override string Description
		{
			get
			{
				return "Mod-specific default sound values have been removed from several traits\n" +
					"(" + fields.Select(f => f.Item1 + "." + f.Item2).JoinWith(", ") + ")\n" +
					"Uses of these traits are listed for inspection so the values can be overriden in yaml.";
			}
		}

		Tuple<string, string, string, List<string>>[] fields =
		{
			Tuple.Create("ParaDrop", "ChuteSound", "chute1.aud", new List<string>()),
			Tuple.Create("EjectOnDeath", "ChuteSound", "chute1.aud", new List<string>()),
			Tuple.Create("ProductionParadrop", "ChuteSound", "chute1.aud", new List<string>()),
			Tuple.Create("Building", "BuildSounds", "placbldg.aud, build5.aud", new List<string>()),
			Tuple.Create("Building", "UndeploySounds", "cashturn.aud", new List<string>())
		};

		string BuildMessage(Tuple<string, string, string, List<string>> field)
		{
			return "The default value for {0}.{1} has been removed.\n".F(field.Item1, field.Item2) +
				"You may wish to explicitly define `{0}: {1}` on the `{2}` trait \n".F(field.Item2, field.Item3, field.Item1) +
				"definitions on the following actors (if they have not already been inherited from a parent).\n" +
				UpdateUtils.FormatMessageList(field.Item4);
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			foreach (var field in fields)
			{
				if (field.Item4.Any())
					yield return BuildMessage(field);

				field.Item4.Clear();
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var field in fields)
			{
				foreach (var traitNode in actorNode.ChildrenMatching(field.Item1))
				{
					var node = traitNode.LastChildMatching(field.Item2);
					if (node == null)
						field.Item4.Add("{0} ({1})".F(actorNode.Key, traitNode.Location.Filename));
				}
			}

			yield break;
		}
	}
}
