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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddEditorPlayer : UpdateRule
	{
		public override string Name { get { return "Add EditorPlayer"; } }
		public override string Description
		{
			get
			{
				return "Map editor now requires an EditorPlayer to avoid loading unnecessary player traits.\n" +
					"A warning is displayed if this actor has not been defined.";
			}
		}

		bool warningRequired = true;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (warningRequired)
			{
				warningRequired = false;
				yield return "The map editor now requires an EditorPlayer actor.\n" +
					"Please add an EditorPlayer with the traits AlwaysVisible and Shroud to player.yaml\n(or a different rules yaml file of your choice).";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.KeyMatches("EditorPlayer"))
				warningRequired = false;

			yield break;
		}
	}
}
