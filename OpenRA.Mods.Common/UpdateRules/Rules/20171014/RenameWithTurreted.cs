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
	public class RenameWithTurreted : UpdateRule
	{
		public override string Name { get { return "Rename 'WithTurretedAttackAnimation' and 'WithTurretedSpriteBody'"; } }
		public override string Description
		{
			get
			{
				return "'WithTurretedAttackAnimation' was renamed to 'WithTurretAttackAnimation'.\n" +
					"'WithTurretedSpriteBody' was renamed to 'WithEmbeddedTurretSpriteBody'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var wtaa in actorNode.ChildrenMatching("WithTurretedAttackAnimation"))
				wtaa.RenameKey("WithTurretAttackAnimation");

			foreach (var wtsb in actorNode.ChildrenMatching("WithTurretedSpriteBody"))
				wtsb.RenameKey("WithEmbeddedTurretSpriteBody");

			yield break;
		}
	}
}
