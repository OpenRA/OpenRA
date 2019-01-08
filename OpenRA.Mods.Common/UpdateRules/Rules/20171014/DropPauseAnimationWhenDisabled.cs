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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class DropPauseAnimationWhenDisabled : UpdateRule
	{
		public override string Name { get { return "Drop 'PauseAnimationWhenDisabled' from 'WithSpriteBody', replacing it with conditions."; } }
		public override string Description
		{
			get
			{
				return "'WithSpriteBody' is a 'PausableConditionalTrait' now, allowing to drop the 'PauseAnimationWhenDisabled' property\n" +
					"and to use 'PauseCondition' instead. (With a default value of 'disabled' in this case.)";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var pauseAnimation = actorNode.LastChildMatching("PauseAnimationWhenDisabled");
			if (pauseAnimation == null)
				yield break;

			pauseAnimation.RenameKey("PauseCondition");
			pauseAnimation.ReplaceValue("disabled");

			yield return "'PauseAnimationWhenDisabled' was removed from 'WithSpriteBody' and replaced by conditions.\n" +
				"You might need to review your condition setup.";
		}
	}
}
