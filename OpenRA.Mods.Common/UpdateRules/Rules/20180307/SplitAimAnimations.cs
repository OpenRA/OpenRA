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
	public class SplitAimAnimations : UpdateRule
	{
		public override string Name { get { return "Introduce WithAimAnimation and WithTurretAimAnimation traits"; } }
		public override string Description
		{
			get
			{
				return "WithAttackAnimation.AimSequence, WithTurretAttackAnimation.AimSequence\n" +
					"as well as WithSpriteTurret.AimSequence have been split to new With*AimAnimation traits.\n" +
					"Furthermore, ReloadPrefixes have been removed in favor of condition-based solutions.";
			}
		}

		readonly List<Tuple<string, string>> aimAnimLocations = new List<Tuple<string, string>>();
		readonly List<Tuple<string, string>> reloadPrefixLocations = new List<Tuple<string, string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message1 = "AimSequences have been split to With*AimAnimation.\n"
				+ "The following actors have been updated and might need manual adjustments:\n"
				+ UpdateUtils.FormatMessageList(aimAnimLocations.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (aimAnimLocations.Any())
				yield return message1;

			aimAnimLocations.Clear();

			var message2 = "ReloadPrefixes have been removed.\n"
				+ "Instead, grant a condition on reloading via Armament.ReloadingCondition to enable\n"
				+ "an alternate sprite body (with reloading sequences) on the following actors:\n"
				+ UpdateUtils.FormatMessageList(reloadPrefixLocations.Select(n => n.Item1 + " (" + n.Item2 + ")"));

			if (reloadPrefixLocations.Any())
				yield return message2;

			reloadPrefixLocations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var turretAttack = actorNode.LastChildMatching("WithTurretAttackAnimation");
			if (turretAttack != null)
			{
				var attackSequence = turretAttack.LastChildMatching("AttackSequence");
				var aimSequence = turretAttack.LastChildMatching("AimSequence");
				var reloadPrefix = turretAttack.LastChildMatching("ReloadPrefix");

				if (aimSequence != null)
					aimAnimLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

				if (reloadPrefix != null)
					reloadPrefixLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

				// If only AttackSequence is not null, just rename AttackSequence to Sequence.
				// If only the prefix isn't null (extremely unlikely, but you never know), just rename the trait.
				// If AttackSequence is null but AimSequence isn't, rename the trait and property.
				// If both aren't null, split/copy everything relevant to the new WithTurretAimAnimation.
				// If both are null (extremely unlikely), do nothing.
				if (attackSequence != null && aimSequence == null)
					attackSequence.RenameKey("Sequence");
				else if (attackSequence == null && aimSequence == null && reloadPrefix != null)
				{
					turretAttack.RemoveNode(reloadPrefix);
					turretAttack.RenameKey("WithTurretAimAnimation");
				}
				else if (attackSequence == null && aimSequence != null)
				{
					turretAttack.RenameKey("WithTurretAimAnimation");
					aimSequence.RenameKey("Sequence");
					if (reloadPrefix != null)
						turretAttack.RemoveNode(reloadPrefix);
				}
				else if (attackSequence != null && aimSequence != null)
				{
					var turretAim = new MiniYamlNode("WithTurretAimAnimation", "");
					aimSequence.MoveAndRenameNode(turretAttack, turretAim, "Sequence");

					var turret = turretAttack.LastChildMatching("Turret");
					var armament = turretAttack.LastChildMatching("Armament");
					if (reloadPrefix != null)
						turretAttack.RemoveNode(reloadPrefix);

					if (turret != null)
						turretAim.AddNode(turret);
					if (armament != null)
						turretAim.AddNode(armament);

					attackSequence.RenameKey("Sequence");
					actorNode.AddNode(turretAim);
				}
			}

			var spriteTurret = actorNode.LastChildMatching("WithSpriteTurret");
			if (spriteTurret != null)
			{
				var aimSequence = spriteTurret.LastChildMatching("AimSequence");
				if (aimSequence != null)
				{
					aimAnimLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

					var aimAnim = new MiniYamlNode("WithTurretAimAnimation", "");
					aimSequence.MoveAndRenameNode(spriteTurret, aimAnim, "Sequence");
					actorNode.AddNode(aimAnim);
				}
			}

			var attackAnim = actorNode.LastChildMatching("WithAttackAnimation");
			if (attackAnim != null)
			{
				var attackSequence = attackAnim.LastChildMatching("AttackSequence");
				var aimSequence = attackAnim.LastChildMatching("AimSequence");
				var reloadPrefix = attackAnim.LastChildMatching("ReloadPrefix");

				if (aimSequence != null)
					aimAnimLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

				if (reloadPrefix != null)
					reloadPrefixLocations.Add(Tuple.Create(actorNode.Key, actorNode.Location.Filename));

				// If only AttackSequence is not null, just rename AttackSequence to Sequence.
				// If only the prefix isn't null (extremely unlikely, but you never know), just rename the trait.
				// If AttackSequence is null but AimSequence isn't, rename the trait and property.
				// If both sequences aren't null, split/copy everything relevant to the new WithAimAnimation.
				// If both sequences and the prefix are null (extremely unlikely), do nothing.
				if (attackSequence != null && aimSequence == null && reloadPrefix == null)
					attackSequence.RenameKey("Sequence");
				else if (attackSequence == null && aimSequence == null && reloadPrefix != null)
				{
					attackAnim.RemoveNode(reloadPrefix);
					attackAnim.RenameKey("WithAimAnimation");
				}
				else if (attackSequence == null && aimSequence != null)
				{
					attackAnim.RenameKey("WithAimAnimation");
					aimSequence.RenameKey("Sequence");
					if (reloadPrefix != null)
						attackAnim.RemoveNode(reloadPrefix);
				}
				else if (attackSequence != null && aimSequence != null)
				{
					var aimAnim = new MiniYamlNode("WithAimAnimation", "");
					aimSequence.MoveAndRenameNode(attackAnim, aimAnim, "Sequence");

					var body = attackAnim.LastChildMatching("Body");
					var armament = attackAnim.LastChildMatching("Armament");
					if (reloadPrefix != null)
						attackAnim.RemoveNode(reloadPrefix);

					if (body != null)
						aimAnim.AddNode(body);
					if (armament != null)
						aimAnim.AddNode(armament);

					attackSequence.RenameKey("Sequence");
					actorNode.AddNode(aimAnim);
				}
			}

			yield break;
		}
	}
}
