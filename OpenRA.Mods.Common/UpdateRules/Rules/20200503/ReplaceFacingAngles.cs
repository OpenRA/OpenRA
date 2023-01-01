#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	class ReplaceFacingAngles : UpdateRule
	{
		static readonly Dictionary<string, string[]> TraitFields = new Dictionary<string, string[]>()
		{
			{ "Aircraft", new[] { "InitialFacing", "PreviewFacing", "TurnSpeed", "IdleTurnSpeed" } },
			{ "Mobile", new[] { "InitialFacing", "PreviewFacing", "TurnSpeed" } },
			{ "Husk", new[] { "PreviewFacing" } },
			{ "Turreted", new[] { "InitialFacing", "TurnSpeed" } },
			{ "GrantConditionOnDeploy", new[] { "Facing" } },
			{ "FreeActor", new[] { "Facing" } },
			{ "ProductionAirdrop", new[] { "Facing" } },
			{ "EditorCursorLayer", new[] { "PreviewFacing" } },
			{ "MPStartUnits", new[] { "BaseActorFacing", "SupportActorsFacing" } },
			{ "Refinery", new[] { "DockAngle" } },
			{ "Cargo", new[] { "PassengerFacing" } },
			{ "Exit", new[] { "Facing" } },
			{ "ThrowsParticle", new[] { "TurnSpeed" } },
			{ "Transforms", new[] { "Facing" } },
			{ "FallsToEarth", new[] { "MaximumSpinSpeed" } },
			{ "ConyardChronoReturn", new[] { "Facing" } },
			{ "TDGunboat", new[] { "InitialFacing", "PreviewFacing" } },
			{ "DropPodsPower", new[] { "PodFacing" } },
			{ "TiberianSunRefinery", new[] { "DockAngle" } },
			{ "AttackAircraft", new[] { "FacingTolerance" } },
			{ "AttackBomber", new[] { "FacingTolerance" } },
			{ "AttackCharges", new[] { "FacingTolerance" } },
			{ "AttackFollow", new[] { "FacingTolerance" } },
			{ "AttackFrontal", new[] { "FacingTolerance" } },
			{ "AttackGarrisoned", new[] { "FacingTolerance" } },
			{ "AttackOmni", new[] { "FacingTolerance" } },
			{ "AttackTurreted", new[] { "FacingTolerance" } },
			{ "AttackLeap", new[] { "FacingTolerance" } },
			{ "AttackPopupTurreted", new[] { "DefaultFacing", "FacingTolerance" } },
			{ "AttackTDGunboatTurreted", new[] { "FacingTolerance" } },
			{ "AttackTesla", new[] { "FacingTolerance" } },
		};

		static readonly Dictionary<string, string[]> ProjectileFields = new Dictionary<string, string[]>()
		{
			{ "Missile", new[] { "HorizontalRateOfTurn", "VerticalRateOfTurn" } }
		};

		public override string Name => "Increase facing angle resolution";

		public override string Description
		{
			get
			{
				return "Trait fields that defined facings (0-255) have been replaced with higher resolution angles (0-1023).\n" +
					"Values for the following trait fields should be multiplied by 4, or if undefined (-1) replaced by an empty definition:\n" +
					UpdateUtils.FormatMessageList(TraitFields.Select(t => t.Key + "\n" + UpdateUtils.FormatMessageList(t.Value))) + "\n" +
					"Values for the following projectile files should be multiplied by 4:\n" +
					UpdateUtils.FormatMessageList(ProjectileFields.Select(t => t.Key + "\n" + UpdateUtils.FormatMessageList(t.Value)));
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var kv in TraitFields)
			{
				foreach (var traitNode in actorNode.ChildrenMatching(kv.Key))
				{
					foreach (var fieldName in kv.Value)
					{
						foreach (var fieldNode in traitNode.ChildrenMatching(fieldName))
						{
							var value = fieldNode.NodeValue<int>();
							fieldNode.Value.Value = value != -1 ? FieldSaver.FormatValue(value != 255 ? 4 * value : 1023) : "";
						}
					}
				}
			}

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var projectileNode in weaponNode.ChildrenMatching("Projectile"))
			{
				if (projectileNode.Value.Value == null)
					continue;

				if (ProjectileFields.TryGetValue(projectileNode.Value.Value, out var fieldNames))
				{
					foreach (var fieldName in fieldNames)
					{
						foreach (var fieldNode in projectileNode.ChildrenMatching(fieldName))
						{
							var value = fieldNode.NodeValue<int>();
							fieldNode.Value.Value = FieldSaver.FormatValue(value != 255 ? 4 * value : 1023);
						}
					}
				}
			}

			yield break;
		}
	}
}
