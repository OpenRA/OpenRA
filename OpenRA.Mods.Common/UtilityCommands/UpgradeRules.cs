#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	static class UpgradeRules
	{
		public const int MinimumSupportedVersion = 20151224;

		internal static void TryUpdateColor(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length == 3)
					value = FieldSaver.FormatValue(Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255)));
				else if (parts.Length == 4)
					value = FieldSaver.FormatValue(Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[3]).Clamp(0, 255)));
			}
			catch { }
		}

		internal static void TryUpdateColors(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length % 4 != 0)
					return;

				var colors = new Color[parts.Length / 4];
				for (var i = 0; i < colors.Length; i++)
				{
					colors[i] = Color.FromArgb(
						Exts.ParseIntegerInvariant(parts[4 * i]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 1]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 2]).Clamp(0, 255),
						Exts.ParseIntegerInvariant(parts[4 * i + 3]).Clamp(0, 255));
				}

				value = FieldSaver.FormatValue(colors);
			}
			catch { }
		}

		internal static void TryUpdateHSLColor(ref string value)
		{
			if (value.Length == 0)
				return;

			try
			{
				var parts = value.Split(',');
				if (parts.Length == 3 || parts.Length == 4)
					value = FieldSaver.FormatValue(new HSLColor(
						(byte)Exts.ParseIntegerInvariant(parts[0]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[1]).Clamp(0, 255),
						(byte)Exts.ParseIntegerInvariant(parts[2]).Clamp(0, 255)));
			}
			catch { }
		}

		internal static void UpgradeActorRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20151225 && depth == 2)
				{
					if (node.Key == "Color")
					{
						if (parent.Key.StartsWith("FixedColorPalette"))
							TryUpdateHSLColor(ref node.Value.Value);
						else
							TryUpdateColor(ref node.Value.Value);
					}
					else if (node.Key == "RadarPingColor" || node.Key == "SelectionBoxColor" || node.Key == "BarColor")
						TryUpdateColor(ref node.Value.Value);
					else if (node.Key == "Fog" || node.Key == "Shroud" || node.Key == "ParticleColors")
						TryUpdateColors(ref node.Value.Value);
				}

				// DeathType on Explodes was renamed to DeathTypes
				if (engineVersion < 20151225)
				{
					if (node.Key == "Explodes")
					{
						var dt = node.Value.Nodes.FirstOrDefault(n => n.Key == "DeathType");
						if (dt != null)
							dt.Key = "DeathTypes";
					}
				}

				// Upgrades on DeployToUpgrade were renamed to DeployedUpgrades
				if (engineVersion < 20151122)
				{
					if (node.Key == "DeployToUpgrade")
					{
						var u = node.Value.Nodes.FirstOrDefault(n => n.Key == "Upgrades");
						if (u != null)
							u.Key = "DeployedUpgrades";
					}
				}

				if (engineVersion < 20151225)
				{
					// Rename WithTurret to WithSpriteTurret
					if (depth == 1 && node.Key.StartsWith("WithTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					// Rename WithBarrel to WithSpriteBarrel
					if (depth == 1 && node.Key.StartsWith("WithBarrel"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithSpriteBarrel";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithBarrel"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithSpriteBarrel";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					// Rename WithReloadingTurret to WithReloadingSpriteTurret
					if (depth == 1 && node.Key.StartsWith("WithReloadingTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "WithReloadingSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}

					if (depth == 1 && node.Key.StartsWith("-WithReloadingTurret"))
					{
						var parts = node.Key.Split('@');
						node.Key = "-WithReloadingSpriteTurret";
						if (parts.Length > 1)
							node.Key += "@" + parts[1];
					}
				}

				// Mobile actors immobilized by Carryable, Cargo, DeployToUpgrade, and/or others using upgrade(s)
				if (engineVersion < 20151225 && depth == 0)
				{
					var notMobile = "notmobile";

					var mobileNode = node.Value.Nodes.Find(n => n.Key == "Mobile");
					var carryableNode = node.Value.Nodes.Find(n => n.Key == "Carryable");
					var cargoNode = node.Value.Nodes.Find(n => n.Key == "Cargo");
					var deployToUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DeployToUpgrade");
					var disableUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DisableUpgrade");
					var disableMovementOnUpgradeNode = node.Value.Nodes.Find(n => n.Key == "DisableMovementOnUpgrade");

					Action<MiniYamlNode, string> addNotMobileToTraitUpgrades = (trait, upgradesKey) =>
					{
						if (trait != null)
						{
							var upgrades = trait.Value.Nodes.Find(u => u.Key == upgradesKey);
							if (upgrades == null)
								trait.Value.Nodes.Add(new MiniYamlNode(upgradesKey, notMobile));
							else if (string.IsNullOrEmpty(upgrades.Value.Value))
								upgrades.Value.Value = notMobile;
							else if (!upgrades.Value.Value.Contains(notMobile))
								upgrades.Value.Value += ", " + notMobile;
						}
					};

					if (mobileNode != null)
					{
						var mobileUpgrades = mobileNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes");
						var mobileUpgradeMaxEnabledLevel = mobileNode.Value.Nodes.Find(n => n.Key == "UpgradeMaxEnabledLevel");
						var comma = new char[] { ',' };

						Func<bool> addUpgradeMaxEnabledLevelNode = () =>
						{
							if (mobileUpgradeMaxEnabledLevel == null)
							{
								mobileUpgradeMaxEnabledLevel = new MiniYamlNode("UpgradeMaxEnabledLevel", "0");
								mobileNode.Value.Nodes.Add(mobileUpgradeMaxEnabledLevel);
								return true;
							}
							else
								return mobileUpgradeMaxEnabledLevel.Value.Value == "0";
						};

						// If exactly one upgrade type is in UpgradeTypes and UpgradeMaxEnabledLevel is/can be 0 , then use it as notmobile
						if (mobileUpgrades != null && !string.IsNullOrEmpty(mobileUpgrades.Value.Value)
							&& !mobileUpgrades.Value.Value.Contains(",") && addUpgradeMaxEnabledLevelNode())
							notMobile = mobileUpgrades.Value.Value;

						if (mobileUpgradeMaxEnabledLevel != null && mobileUpgradeMaxEnabledLevel.Value.Value != "0")
							Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");
						else
						{
							Action<string> addImmobilizeUpgradeType = upgradeType =>
							{
								if (mobileUpgrades == null)
								{
									mobileUpgrades = new MiniYamlNode("UpgradeTypes", upgradeType);
									mobileNode.Value.Nodes.Add(mobileUpgrades);
								}
								else if (string.IsNullOrEmpty(mobileUpgrades.Value.Value))
									mobileUpgrades.Value.Value = upgradeType;
								else if (!mobileUpgrades.Value.Value.Split(comma).Contains(upgradeType))
									mobileUpgrades.Value.Value += ", " + upgradeType;
							};

							Predicate<string> addImmobilizeUpgradeTypes = upgradeTypes =>
							{
								if (string.IsNullOrEmpty(upgradeTypes))
									return false;

								foreach (var upgradeType in upgradeTypes.Split(comma))
									addImmobilizeUpgradeType(upgradeType);
								return true;
							};

							Predicate<MiniYamlNode> addUpgradeTypeFromTrait = trait =>
							{
								var upgradeTypesNode = trait.Value.Nodes.Find(n => n.Key == "UpgradeTypes");
								if (upgradeTypesNode == null)
									return false;

								addUpgradeMaxEnabledLevelNode();
								return addImmobilizeUpgradeTypes(upgradeTypesNode.Value.Value);
							};

							var noticeWritten = false;

							Action writeNotice = () =>
							{
								if (noticeWritten)
									return;
								Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");
								noticeWritten = true;
							};

							if (disableUpgradeNode != null && !addUpgradeTypeFromTrait(disableUpgradeNode))
							{
								writeNotice();
								Console.WriteLine("\t\t\tOne or more upgrades may need to be copied from the DisableUpgrade trait to the Mobile trait.");
							}

							if (disableMovementOnUpgradeNode != null)
							{
								if (addUpgradeTypeFromTrait(disableMovementOnUpgradeNode))
									parent.Value.Nodes.Remove(disableMovementOnUpgradeNode);
								else
								{
									writeNotice();
									Console.WriteLine("\t\t\tOne or more upgrades may need to be moved from the DisableMovementOnUpgrade trait to the Mobile trait.");
									Console.WriteLine("\t\t\t\tRemember to remove the DisableMovementOnUpgrade trait.");
								}
							}

							if (carryableNode != null || cargoNode != null || deployToUpgradeNode != null)
							{
								addUpgradeMaxEnabledLevelNode();
								addImmobilizeUpgradeTypes(notMobile);

								addNotMobileToTraitUpgrades(carryableNode, "CarryableUpgrades");
								addNotMobileToTraitUpgrades(cargoNode, "LoadingUpgrades");
								addNotMobileToTraitUpgrades(deployToUpgradeNode, "DeployedUpgrades");
							}
						}
					}
					else if (!node.Value.Nodes.Exists(n => n.Key == "Husk" || n.Key == "Building" || n.Key == "Aircraft" || n.Key == "Immobile"))
					{
						if (carryableNode != null || cargoNode != null || deployToUpgradeNode != null)
						{
							Console.WriteLine("\t\tIf " + node.Key
								+ " has a Mobile trait then adding the following with <upgrade> substituted by an immobilization upgrade for "
								+ node.Key + " may be neeeded:");

							if (carryableNode != null)
							{
								Console.WriteLine("\t\t\tCarryable:");
								Console.WriteLine("\t\t\t\tCarryableUpgrades: <upgrade>");
							}

							if (cargoNode != null)
							{
								Console.WriteLine("\t\t\tCargo:");
								Console.WriteLine("\t\t\t\tLoadingUpgrades: <upgrade>");
							}

							if (deployToUpgradeNode != null)
							{
								Console.WriteLine("\t\t\tDeployToUpgrade:");
								Console.WriteLine("\t\t\t\tDeployedUpgrades: <upgrade>");
							}
						}

						var disableUpgradeUpgradeTypesNode = disableUpgradeNode != null
							? disableUpgradeNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes")
							: null;
						var disableMovementOnUpgradeUpgradeTypesNode = disableMovementOnUpgradeNode != null
							? disableMovementOnUpgradeNode.Value.Nodes.Find(n => n.Key == "UpgradeTypes")
							: null;

						if (disableUpgradeUpgradeTypesNode != null || disableMovementOnUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t" + node.Key + " actor rules may require manual upgrading for immobilization upgrade logic.");

						if (disableUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t\tDisableUpgrade UpgradeTypes: " + disableUpgradeUpgradeTypesNode.Value.Value);

						if (disableMovementOnUpgradeUpgradeTypesNode != null)
							Console.WriteLine("\t\t\tDisableMovementOnUpgrade UpgradeTypes: " + disableMovementOnUpgradeUpgradeTypesNode.Value.Value);

						if (disableMovementOnUpgradeNode != null)
							node.Value.Nodes.Remove(disableMovementOnUpgradeNode);
					}
				}

				// 'CloseEnough' on 'RepairableNear' uses WDist now
				if (engineVersion < 20151225)
				{
					if (node.Key == "RepairableNear")
					{
						var ce = node.Value.Nodes.FirstOrDefault(n => n.Key == "CloseEnough");
						if (ce != null && !ce.Value.Value.Contains("c"))
							ce.Value.Value = ce.Value.Value + "c0";
					}
				}

				// Added width support for line particles
				if (engineVersion < 20151225 && node.Key == "WeatherOverlay")
				{
					var useSquares = node.Value.Nodes.FirstOrDefault(n => n.Key == "UseSquares");
					if (useSquares != null && !FieldLoader.GetValue<bool>("UseSquares", useSquares.Value.Value))
						node.Value.Nodes.Add(new MiniYamlNode("ParticleSize", "1, 1"));
				}

				// Overhauled the actor decorations traits
				if (engineVersion < 20151226)
				{
					if (depth == 1 && (node.Key.StartsWith("WithDecoration") || node.Key.StartsWith("WithRankDecoration")))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "Scale");
						node.Value.Nodes.RemoveAll(n => n.Key == "Offset");
						var sd = node.Value.Nodes.FirstOrDefault(n => n.Key == "SelectionDecoration");
						if (sd != null)
							sd.Key = "RequiresSelection";

						var reference = node.Value.Nodes.FirstOrDefault(n => n.Key == "ReferencePoint");
						if (reference != null)
						{
							var values = FieldLoader.GetValue<string[]>("ReferencePoint", reference.Value.Value);
							values = values.Where(v => v != "HCenter" && v != "VCenter").ToArray();
							if (values.Length == 0)
								values = new[] { "Center" };

							reference.Value.Value = FieldSaver.FormatValue(values);
						}

						var stance = Stance.Ally;
						var showToAllies = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShowToAllies");
						if (showToAllies != null && !FieldLoader.GetValue<bool>("ShowToAllies", showToAllies.Value.Value))
							stance ^= Stance.Ally;
						var showToEnemies = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShowToEnemies");
						if (showToEnemies != null && FieldLoader.GetValue<bool>("ShowToEnemies", showToEnemies.Value.Value))
							stance |= Stance.Enemy;

						if (stance != Stance.Ally)
							node.Value.Nodes.Add(new MiniYamlNode("Stance", FieldSaver.FormatValue(stance)));

						node.Value.Nodes.RemoveAll(n => n.Key == "ShowToAllies");
						node.Value.Nodes.RemoveAll(n => n.Key == "ShowToEnemies");
					}

					if (depth == 1 && node.Key == "Fake")
					{
						node.Key = "WithDecoration@fake";
						node.Value.Nodes.Add(new MiniYamlNode("RequiresSelection", "true"));
						node.Value.Nodes.Add(new MiniYamlNode("Image", "pips"));
						node.Value.Nodes.Add(new MiniYamlNode("Sequence", "tag-fake"));
						node.Value.Nodes.Add(new MiniYamlNode("ReferencePoint", "Top"));
						node.Value.Nodes.Add(new MiniYamlNode("ZOffset", "256"));
					}

					if (depth == 0 && node.Value.Nodes.Any(n => n.Key.StartsWith("PrimaryBuilding")))
					{
						var decNodes = new List<MiniYamlNode>();
						decNodes.Add(new MiniYamlNode("RequiresSelection", "true"));
						decNodes.Add(new MiniYamlNode("Image", "pips"));
						decNodes.Add(new MiniYamlNode("Sequence", "tag-primary"));
						decNodes.Add(new MiniYamlNode("ReferencePoint", "Top"));
						decNodes.Add(new MiniYamlNode("ZOffset", "256"));
						decNodes.Add(new MiniYamlNode("UpgradeTypes", "primary"));
						decNodes.Add(new MiniYamlNode("UpgradeMinEnabledLevel", "1"));
						node.Value.Nodes.Add(new MiniYamlNode("WithDecoration@primary", new MiniYaml("", decNodes)));
					}
				}

				// Refactored the low resources notification to a separate trait
				if (engineVersion < 20151227 && node.Key == "Player")
				{
					var resourcesNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "PlayerResources");

					if (resourcesNode != null)
					{
						var intervalNode = resourcesNode.Value.Nodes.FirstOrDefault(x => x.Key == "AdviceInterval");
						var storageNode = new MiniYamlNode("ResourceStorageWarning", "");

						if (intervalNode != null)
						{
							// The time value is now in seconds, not ticks. We
							// divide by 25 ticks per second at Normal.
							int oldInterval;
							if (int.TryParse(intervalNode.Value.Value, out oldInterval))
								storageNode.Value.Nodes.Add(new MiniYamlNode("AdviceInterval", (oldInterval / 25).ToString()));
							resourcesNode.Value.Nodes.Remove(intervalNode);
						}

						node.Value.Nodes.Add(storageNode);
					}
				}

				// Refactored Health.Radius to HitShapes
				if (engineVersion < 20151227)
				{
					if (node.Key.StartsWith("Health"))
					{
						var radius = node.Value.Nodes.FirstOrDefault(x => x.Key == "Radius");
						if (radius != null)
						{
							var radiusValue = FieldLoader.GetValue<string>("Radius", radius.Value.Value);
							node.Value.Nodes.Add(new MiniYamlNode("Shape", "Circle"));

							var shape = node.Value.Nodes.First(x => x.Key == "Shape");
							shape.Value.Nodes.Add(new MiniYamlNode("Radius", radiusValue));

							node.Value.Nodes.Remove(radius);
						}
					}
				}

				// Remove obsolete TransformOnPassenger trait.
				if (engineVersion < 20160102)
				{
					var removed = node.Value.Nodes.RemoveAll(x => x.Key.Contains("TransformOnPassenger"));
					if (removed > 0)
					{
						Console.WriteLine("TransformOnPassenger has been removed.");
						Console.WriteLine("Use the upgrades system to apply modifiers to the transport actor instead.");
					}
				}

				if (engineVersion < 20160103)
				{
					// Overhauled WithActiveAnimation -> WithIdleAnimation
					if (node.Key == "WithActiveAnimation")
					{
						node.Key = "WithIdleAnimation";
						foreach (var n in node.Value.Nodes)
							if (n.Key == "Sequence")
								n.Key = "Sequences";
					}
				}

				if (engineVersion < 20160107 && depth == 1 && node.Key.StartsWith("Cloak"))
				{
					var defaultCloakType = Traits.UncloakType.Attack
						| Traits.UncloakType.Unload | Traits.UncloakType.Infiltrate | Traits.UncloakType.Demolish;

					// Merge Uncloak types
					var t = defaultCloakType;
					for (var i = node.Value.Nodes.Count - 1; i >= 0; i--)
					{
						var n = node.Value.Nodes[i];
						var v = string.Compare(n.Value.Value, "true", true) == 0;
						Traits.UncloakType flag;
						if (n.Key == "UncloakOnAttack")
							flag = Traits.UncloakType.Attack;
						else if (n.Key == "UncloakOnMove")
							flag = Traits.UncloakType.Move;
						else if (n.Key == "UncloakOnUnload")
							flag = Traits.UncloakType.Unload;
						else if (n.Key == "UncloakOnInfiltrate")
							flag = Traits.UncloakType.Infiltrate;
						else if (n.Key == "UncloakOnDemolish")
							flag = Traits.UncloakType.Demolish;
						else
							continue;
						t = v ? t | flag : t & ~flag;
						node.Value.Nodes.Remove(n);
					}

					if (t != defaultCloakType)
					{
						Console.WriteLine("\t\tCloak type: " + t.ToString());
						var ts = new List<string>();
						if (t.HasFlag(Traits.UncloakType.Attack))
							ts.Add("Attack");
						if (t.HasFlag(Traits.UncloakType.Unload))
							ts.Add("Unload");
						if (t.HasFlag(Traits.UncloakType.Infiltrate))
							ts.Add("Infiltrate");
						if (t.HasFlag(Traits.UncloakType.Demolish))
							ts.Add("Demolish");
						if (t.HasFlag(Traits.UncloakType.Move))
							ts.Add("Move");
						node.Value.Nodes.Add(new MiniYamlNode("UncloakOn", ts.JoinWith(", ")));
					}
				}

				// Rename WithDockingOverlay to WithDockedOverlay
				if (engineVersion < 20160116)
				{
					if (node.Key.StartsWith("WithDockingOverlay"))
						node.Key = "WithDockedOverlay" + node.Key.Substring(18);
				}

				if (engineVersion < 20160116)
				{
					if (node.Key == "DemoTruck")
						node.Key = "AttackSuicides";
				}

				// Replaced GpsRemoveFrozenActor with FrozenUnderFogUpdatedByGps
				if (engineVersion < 20160117)
				{
					if (node.Key == "GpsRemoveFrozenActor")
					{
						node.Key = "FrozenUnderFogUpdatedByGps";
						node.Value.Nodes.Clear();
					}
				}

				// Removed arbitrary defaults from InfiltrateForCash
				if (engineVersion < 20160118)
				{
					if (node.Key == "InfiltrateForCash")
					{
						if (!node.Value.Nodes.Any(n => n.Key == "Percentage"))
							node.Value.Nodes.Add(new MiniYamlNode("Percentage", "50"));

						if (!node.Value.Nodes.Any(n => n.Key == "Minimum"))
							node.Value.Nodes.Add(new MiniYamlNode("Minimum", "500"));

						var sound = node.Value.Nodes.FirstOrDefault(n => n.Key == "SoundToVictim");
						if (sound != null)
						{
							node.Value.Nodes.Remove(sound);
							Console.WriteLine("The 'SoundToVictim' property of the 'InfiltrateForCash' trait has been");
							Console.WriteLine("replaced with a 'Notification' property. Please add the sound file");
							Console.WriteLine("'{0}' to your mod's audio notification yaml and".F(sound.Value.Value));
							Console.WriteLine("update your mod's rules accordingly.");
							Console.WriteLine();
						}
					}
				}

				UpgradeActorRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeWeaponRules(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				if (engineVersion < 20160124)
				{
					node.Value.Nodes.RemoveAll(x => x.Key == "Charges");
				}

				// Enhance CreateEffectWarhead
				if (engineVersion < 20160131)
				{
					if (node.Key.StartsWith("Warhead") && node.Value.Value == "CreateEffect")
					{
						// Add support for multiple explosions to CreateEffectWarhead
						var explosionNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "Explosion");
						if (explosionNode != null)
							explosionNode.Key = "Explosions";

						// Add support for multiple impact sounds to CreateEffectWarhead
						var impactSoundNode = node.Value.Nodes.FirstOrDefault(x => x.Key == "ImpactSound");
						if (impactSoundNode != null)
							impactSoundNode.Key = "ImpactSounds";
					}
				}

				// Rename some speed-related Missile properties
				if (engineVersion < 20160205)
				{
					var mod = Game.ModData.Manifest.Mod.Id;
					if (mod == "ts")
					{
						if (node.Key == "Projectile" && node.Value.Value == "Missile")
						{
							node.Value.Nodes.Add(new MiniYamlNode("MinimumLaunchSpeed", "75"));
							node.Value.Nodes.Add(new MiniYamlNode("Speed", "384"));
						}
					}
					else
					{
						if (node.Key == "MaximumLaunchSpeed" && parent.Value.Value == "Missile")
							node.Key = "Speed";
					}
				}

				UpgradeWeaponRules(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeTileset(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeTileset(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeCursors(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeCursors(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradePlayers(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradePlayers(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeMetrics(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeMetrics(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeLayout(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeLayout(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeActors(int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeActors(engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeMapFormat(ModData modData, IReadWritePackage package)
		{
			if (package == null)
				return;

			var yamlStream = package.GetStream("map.yaml");
			if (yamlStream == null)
				return;

			var yaml = new MiniYaml(null, MiniYaml.FromStream(yamlStream, package.Name));
			var nd = yaml.ToDictionary();
			var mapFormat = FieldLoader.GetValue<int>("MapFormat", nd["MapFormat"].Value);
			if (mapFormat < 6)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(mapFormat, package.Name));

			// Nothing to do
			if (mapFormat >= 8)
				return;

			// Format 6 -> 7 combined the Selectable and UseAsShellmap flags into the Class enum
			if (mapFormat < 7)
			{
				MiniYaml useAsShellmap;
				if (nd.TryGetValue("UseAsShellmap", out useAsShellmap) && bool.Parse(useAsShellmap.Value))
					yaml.Nodes.Add(new MiniYamlNode("Visibility", new MiniYaml("Shellmap")));
				else if (nd["Type"].Value == "Mission" || nd["Type"].Value == "Campaign")
					yaml.Nodes.Add(new MiniYamlNode("Visibility", new MiniYaml("MissionSelector")));
			}

			// Format 7 -> 8 replaced normalized HSL triples with rgb(a) hex colors
			if (mapFormat < 8)
			{
				var players = yaml.Nodes.FirstOrDefault(n => n.Key == "Players");
				if (players != null)
				{
					bool noteHexColors = false;
					bool noteColorRamp = false;
					foreach (var player in players.Value.Nodes)
					{
						var colorRampNode = player.Value.Nodes.FirstOrDefault(n => n.Key == "ColorRamp");
						if (colorRampNode != null)
						{
							Color dummy;
							var parts = colorRampNode.Value.Value.Split(',');
							if (parts.Length == 3 || parts.Length == 4)
							{
								// Try to convert old normalized HSL value to a rgb hex color
								try
								{
									HSLColor color = new HSLColor(
										(byte)Exts.ParseIntegerInvariant(parts[0].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[1].Trim()).Clamp(0, 255),
										(byte)Exts.ParseIntegerInvariant(parts[2].Trim()).Clamp(0, 255));
									colorRampNode.Value.Value = FieldSaver.FormatValue(color);
									noteHexColors = true;
								}
								catch (Exception)
								{
									throw new InvalidDataException("Invalid ColorRamp value.\n File: " + package.Name);
								}
							}
							else if (parts.Length != 1 || !HSLColor.TryParseRGB(parts[0], out dummy))
								throw new InvalidDataException("Invalid ColorRamp value.\n File: " + package.Name);

							colorRampNode.Key = "Color";
							noteColorRamp = true;
						}
					}

					Console.WriteLine("Converted " + package.Name + " to MapFormat 8.");
					if (noteHexColors)
						Console.WriteLine("ColorRamp is now called Color and uses rgb(a) hex value - rrggbb[aa].");
					else if (noteColorRamp)
						Console.WriteLine("ColorRamp is now called Color.");
				}
			}

			yaml.Nodes.First(n => n.Key == "MapFormat").Value = new MiniYaml(Map.SupportedMapFormat.ToString());

			var entries = new Dictionary<string, byte[]>();
			entries.Add("map.yaml", Encoding.UTF8.GetBytes(yaml.Nodes.WriteToString()));
			foreach (var file in package.Contents)
			{
				if (file == "map.yaml")
					continue;

				entries.Add(file, package.GetStream(file).ReadAllBytes());
			}

			package.Write(entries);
		}
	}
}
