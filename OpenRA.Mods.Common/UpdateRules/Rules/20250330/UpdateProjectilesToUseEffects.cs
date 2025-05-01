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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class UpdateProjectilesToUseEffects : UpdateRule, IBeforeUpdateWeapons
	{
		public override string Name => "Projectile effects where separated from projectiles.";

		public override string Description =>
			"Extract effects into separate effects for projectiles.";

		IEnumerable<string> IBeforeUpdateWeapons.BeforeUpdateWeapons(ModData modData,
			List<MiniYamlNodeBuilder> resolvedWeapons)
		{
			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNodeBuilder weaponNode)
		{
			var effects = new List<MiniYamlNodeBuilder>();
			foreach (var projectile in weaponNode.ChildrenMatching("Projectile"))
			{
				switch (projectile.Value.Value)
				{
					case "Missile":
						effects.AddRange(UpdateMissile(projectile));
						break;
					case "Bullet":
						effects.AddRange(UpdateBullet(projectile));
						break;
					case "GravityBomb":
						effects.AddRange(UpdateGravityBomb(projectile));
						break;
				}
			}

			foreach (var effect in effects)
			{
				if (effect.IsRemoval())
				{
					effect.Value.Nodes.Clear();
					weaponNode.AddNode(effect);
					continue;
				}

				var oldNode = weaponNode.LastChildMatching(effect.Key);
				if (oldNode == null)
					weaponNode.AddNode(effect);
				else
				{
					if (!oldNode.IsRemoval())
						foreach (var node in effect.Value.Nodes)
							oldNode.AddNode(node);
				}
			}

			yield break;
		}

		static IEnumerable<MiniYamlNodeBuilder> UpdateMissile(MiniYamlNodeBuilder projectile)
		{
			var spriteEffect = new MiniYamlNodeBuilder("Effect@SPRITE", "Sprite");
			var contrailEffect = new MiniYamlNodeBuilder("Effect@CONTRAIL", "Contrail");
			var trailEffect = new MiniYamlNodeBuilder("Effect@EMITTER", "Emitter");

			var removeEffects = new List<MiniYamlNodeBuilder>();
			foreach (var property in projectile.Value.Nodes)
			{
				var removal = property.IsRemoval();
				var key = removal ? property.Key[1..] : property.Key;
				switch (key)
				{
					case "Image":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(spriteEffect);
						}
						else
						{
							spriteEffect.AddNode(property);
							removeEffects.Add(property);
						}

						continue;
					case "Sequences":
					case "Palette":
					case "IsPlayerPalette":
					case "Shadow":
					case "ShadowColor":
						removeEffects.Add(property);
						spriteEffect.AddNode(property);
						continue;
					case "ContrailLength":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(contrailEffect);
						}
						else
						{
							property.RenameKey(key[8..]);
							removeEffects.Add(property);
							contrailEffect.AddNode(property);
						}

						continue;
					case "ContrailDelay":
					case "ContrailZOffset":
					case "ContrailStartWidth":
					case "ContrailEndWidth":
					case "ContrailStartColor":
					case "ContrailEndColor":
					case "ContrailStartColorUsePlayerColor":
					case "ContrailStartColorAlpha":
					case "ContrailEndColorUsePlayerColor":
					case "ContrailEndColorAlpha":
						property.RenameKey(removal ? '-' + key[8..] : key[8..]);
						removeEffects.Add(property);
						contrailEffect.AddNode(property);
						continue;
					case "TrailImage":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(trailEffect);
						}
						else
						{
							property.RenameKey(key[5..]);
							removeEffects.Add(property);
							trailEffect.AddNode(property);
						}

						continue;
					case "TrailSequences":
					case "TrailPalette":
					case "TrailInterval":
					case "TrailDelay":
					case "TrailUsePlayerPalette":
						property.RenameKey(removal ? '-' + key[5..] : key[5..]);
						removeEffects.Add(property);
						trailEffect.AddNode(property);
						continue;
					case "TrailWhenDeactivated":
						removeEffects.Add(property);
						continue;
				}
			}

			foreach (var effect in removeEffects)
				projectile.RemoveNode(effect);

			if (spriteEffect.IsRemoval() || spriteEffect.Value.Nodes.Count != 0)
				yield return spriteEffect;

			if (contrailEffect.IsRemoval() || contrailEffect.Value.Nodes.Count != 0)
				yield return contrailEffect;

			if (trailEffect.IsRemoval() || trailEffect.Value.Nodes.Count != 0)
				yield return trailEffect;
		}

		static IEnumerable<MiniYamlNodeBuilder> UpdateBullet(MiniYamlNodeBuilder projectile)
		{
			var spriteEffect = new MiniYamlNodeBuilder("Effect@SPRITE", "Sprite");
			var contrailEffect = new MiniYamlNodeBuilder("Effect@CONTRAIL", "Contrail");
			var trailEffect = new MiniYamlNodeBuilder("Effect@EMITTER", "Emitter");

			var removeEffects = new List<MiniYamlNodeBuilder>();
			foreach (var property in projectile.Value.Nodes)
			{
				var removal = property.IsRemoval();
				var key = removal ? property.Key[1..] : property.Key;

				switch (key)
				{
					case "Image":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(spriteEffect);
						}
						else
						{
							spriteEffect.AddNode(property);
							removeEffects.Add(property);
						}

						continue;
					case "Sequences":
					case "Palette":
					case "IsPlayerPalette":
					case "Shadow":
					case "ShadowColor":
						removeEffects.Add(property);
						spriteEffect.AddNode(property);
						continue;
					case "ContrailLength":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(contrailEffect);
						}
						else
						{
							property.RenameKey(key[8..]);
							removeEffects.Add(property);
							contrailEffect.AddNode(property);
						}

						continue;
					case "ContrailDelay":
					case "ContrailZOffset":
					case "ContrailStartWidth":
					case "ContrailEndWidth":
					case "ContrailStartColor":
					case "ContrailEndColor":
					case "ContrailStartColorUsePlayerColor":
					case "ContrailStartColorAlpha":
					case "ContrailEndColorUsePlayerColor":
					case "ContrailEndColorAlpha":
						property.RenameKey(removal ? '-' + key[8..] : key[8..]);
						removeEffects.Add(property);
						contrailEffect.AddNode(property);
						continue;
					case "TrailImage":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(trailEffect);
						}
						else
						{
							property.RenameKey(key[5..]);
							removeEffects.Add(property);
							trailEffect.AddNode(property);
						}

						continue;
					case "TrailSequences":
					case "TrailPalette":
					case "TrailInterval":
					case "TrailDelay":
					case "TrailUsePlayerPalette":
						property.RenameKey(removal ? '-' + key[5..] : key[5..]);
						removeEffects.Add(property);
						trailEffect.AddNode(property);
						continue;
				}
			}

			foreach (var effect in removeEffects)
				projectile.RemoveNode(effect);

			if (spriteEffect.IsRemoval() || spriteEffect.Value.Nodes.Count != 0)
				yield return spriteEffect;

			if (contrailEffect.IsRemoval() || contrailEffect.Value.Nodes.Count != 0)
				yield return contrailEffect;

			if (trailEffect.IsRemoval() || trailEffect.Value.Nodes.Count != 0)
				yield return trailEffect;
		}

		static IEnumerable<MiniYamlNodeBuilder> UpdateGravityBomb(MiniYamlNodeBuilder projectile)
		{
			var spriteEffect = new MiniYamlNodeBuilder("Effect@SPRITE", "Sprite");

			var removeEffects = new List<MiniYamlNodeBuilder>();
			foreach (var property in projectile.Value.Nodes)
			{
				var removal = property.IsRemoval();
				var key = removal ? property.Key[1..] : property.Key;

				switch (key)
				{
					case "Image":
						if (removal)
						{
							removeEffects.Add(property);
							MarkForRemoval(spriteEffect);
						}
						else
						{
							spriteEffect.AddNode(property);
							removeEffects.Add(property);
						}

						continue;
					case "Sequences":
					case "Palette":
					case "IsPlayerPalette":
					case "Shadow":
					case "ShadowColor":
						spriteEffect.AddNode(property);
						removeEffects.Add(property);
						continue;
					case "OpenSequence":
						property.RenameKey("StartSequence");
						spriteEffect.AddNode(property);
						removeEffects.Add(property);
						continue;
				}
			}

			foreach (var effect in removeEffects)
				projectile.RemoveNode(effect);

			if (spriteEffect.IsRemoval() || spriteEffect.Value.Nodes.Count != 0)
				yield return spriteEffect;
		}

		static void MarkForRemoval(MiniYamlNodeBuilder node)
		{
			node.Key = '-' + node.Key;
			node.Value.Nodes.Clear();
		}
	}
}
