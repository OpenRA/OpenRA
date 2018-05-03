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
	public class SplitTurretAimAnimation : UpdateRule
	{
		public override string Name { get { return "Introduce WithTurretAimAnimation trait"; } }
		public override string Description
		{
			get
			{
				return "WithSpriteTurret.AimSequence and WithTurretAttackAnimation.AimSequence\n" +
					"have been split into a new WithTurretAimAnimation trait.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var turretAttack = actorNode.LastChildMatching("WithTurretAttackAnimation");
			if (turretAttack != null)
			{
				var attackSequence = turretAttack.LastChildMatching("AttackSequence");
				var aimSequence = turretAttack.LastChildMatching("AimSequence");

				// If only AimSequence is null, just rename AttackSequence to Sequence (ReloadPrefix is very unlikely to be defined in that case).
				// If only AttackSequence is null, just rename the trait and property (the delay properties will likely be undefined).
				// If both aren't null, split/copy everything relevant to the new WithTurretAimAnimation.
				// If both are null (extremely unlikely), do nothing.
				if (attackSequence == null && aimSequence != null)
				{
					turretAttack.RenameKeyPreservingSuffix("WithTurretAimAnimation");
					aimSequence.RenameKeyPreservingSuffix("Sequence");
				}
				else if (attackSequence != null && aimSequence == null)
					attackSequence.RenameKeyPreservingSuffix("Sequence");
				else if (attackSequence != null && aimSequence != null)
				{
					var turretAim = new MiniYamlNode("WithTurretAimAnimation", "");
					aimSequence.RenameKeyPreservingSuffix("Sequence");
					turretAim.AddNode(aimSequence);
					turretAttack.RemoveNode(aimSequence);

					var reloadPrefix = turretAttack.LastChildMatching("ReloadPrefix");
					var turret = turretAttack.LastChildMatching("Turret");
					var armament = turretAttack.LastChildMatching("Armament");
					if (reloadPrefix != null)
					{
						turretAim.AddNode(reloadPrefix);
						turretAttack.RemoveNode(reloadPrefix);
					}

					if (turret != null)
						turretAim.AddNode(turret);
					if (armament != null)
						turretAim.AddNode(armament);

					attackSequence.RenameKeyPreservingSuffix("Sequence");
					actorNode.AddNode(turretAim);
				}
			}

			var spriteTurret = actorNode.LastChildMatching("WithSpriteTurret");
			if (spriteTurret != null)
			{
				var aimSequence = spriteTurret.LastChildMatching("AimSequence");
				if (aimSequence != null)
				{
					var aimAnim = new MiniYamlNode("WithTurretAimAnimation", "");
					aimSequence.RenameKeyPreservingSuffix("Sequence");
					aimAnim.AddNode(aimSequence);
					spriteTurret.RemoveNode(aimSequence);
					actorNode.AddNode(aimAnim);
				}
			}

			yield break;
		}
	}
}
