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
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	static class UpgradeRules
	{
		public const int MinimumSupportedVersion = 20171014;

		static void RenameNodeKey(MiniYamlNode node, string key)
		{
			if (node == null)
				return;

			var parts = node.Key.Split('@');
			node.Key = key;
			if (parts.Length > 1)
				node.Key += "@" + parts[1];
		}

		internal static string MultiplyByFactor(int oldValue, int factor)
		{
			oldValue = oldValue * factor;
			return oldValue.ToString();
		}

		internal static void UpgradeActorRules(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			var addNodes = new List<MiniYamlNode>();

			foreach (var node in nodes)
			{
				// Replace Mobile.OnRails hack with dedicated TDGunboat traits in Mods.Cnc
				if (engineVersion < 20171015)
				{
					var mobile = node.Value.Nodes.FirstOrDefault(n => n.Key == "Mobile");
					if (mobile != null)
					{
						var onRailsNode = mobile.Value.Nodes.FirstOrDefault(n => n.Key == "OnRails");
						var onRails = onRailsNode != null ? FieldLoader.GetValue<bool>("OnRails", onRailsNode.Value.Value) : false;
						if (onRails)
						{
							var speed = mobile.Value.Nodes.FirstOrDefault(n => n.Key == "Speed");
							var initFacing = mobile.Value.Nodes.FirstOrDefault(n => n.Key == "InitialFacing");
							var previewFacing = mobile.Value.Nodes.FirstOrDefault(n => n.Key == "PreviewFacing");
							var tdGunboat = new MiniYamlNode("TDGunboat", "");
							if (speed != null)
								tdGunboat.Value.Nodes.Add(speed);
							if (initFacing != null)
								tdGunboat.Value.Nodes.Add(initFacing);
							if (previewFacing != null)
								tdGunboat.Value.Nodes.Add(previewFacing);

							node.Value.Nodes.Add(tdGunboat);

							var attackTurreted = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("AttackTurreted", StringComparison.Ordinal));
							if (attackTurreted != null)
								RenameNodeKey(attackTurreted, "AttackTDGunboatTurreted");

							node.Value.Nodes.Remove(mobile);
						}
					}
				}

				// Introduced TakeOffOnCreation and TakeOffOnResupply booleans to aircraft
				if (engineVersion < 20171015)
				{
					if (node.Key.StartsWith("Aircraft", StringComparison.Ordinal))
					{
						var canHover = node.Value.Nodes.FirstOrDefault(n => n.Key == "CanHover");
						var isHeli = canHover != null ? FieldLoader.GetValue<bool>("CanHover", canHover.Value.Value) : false;
						if (isHeli)
						{
							Console.WriteLine("Helicopters taking off automatically while planes don't is no longer hardcoded.");
							Console.WriteLine("Instead, this is controlled via the TakeOffOnResupply field.");
							Console.WriteLine("Please check if your aircraft behave as intended or need manual adjustments.");
							node.Value.Nodes.Add(new MiniYamlNode("TakeOffOnResupply", "true"));
						}

						// Upgrade rule for setting VTOL to true for CanHover actors
						if (isHeli)
							node.Value.Nodes.Add(new MiniYamlNode("VTOL", "true"));
					}
				}

				// nuke launch animation is now it's own trait
				if (engineVersion < 20171015)
				{
					if (depth == 1 && node.Key.StartsWith("NukePower", StringComparison.Ordinal))
					{
						node.Value.Nodes.RemoveAll(n => n.Key == "ActivationSequence");
						addNodes.Add(new MiniYamlNode("WithNukeLaunchAnimation", new MiniYaml("")));
					}
				}

				if (engineVersion < 20171015)
				{
					if (node.Key.StartsWith("WithTurretedAttackAnimation", StringComparison.Ordinal))
						RenameNodeKey(node, "WithTurretAttackAnimation");
					if (node.Key.StartsWith("WithTurretedSpriteBody", StringComparison.Ordinal))
						RenameNodeKey(node, "WithEmbeddedTurretSpriteBody");
				}

				if (engineVersion < 20171015)
				{
					if (node.Key.StartsWith("PlayerPaletteFromCurrentTileset", StringComparison.Ordinal))
					{
						node.Value.Nodes.Add(new MiniYamlNode("Filename", ""));
						node.Value.Nodes.Add(new MiniYamlNode("Tileset", ""));
						RenameNodeKey(node, "PaletteFromFile");
						Console.WriteLine("The trait PlayerPaletteFromCurrentTileset has been removed. Use PaletteFromFile with a Tileset filter.");
					}
				}

				if (engineVersion < 20171021)
				{
					if (node.Key.StartsWith("Capturable", StringComparison.Ordinal) || node.Key.StartsWith("ExternalCapturable", StringComparison.Ordinal))
					{
						// Type renamed to Types
						var type = node.Value.Nodes.FirstOrDefault(n => n.Key == "Type");
						if (type != null)
							RenameNodeKey(type, "Types");

						// Allow(Allies|Neutral|Enemies) replaced with a ValidStances enum
						var stance = Stance.Neutral | Stance.Enemy;
						var allowAllies = node.Value.Nodes.FirstOrDefault(n => n.Key == "AllowAllies");
						if (allowAllies != null)
						{
							if (FieldLoader.GetValue<bool>("AllowAllies", allowAllies.Value.Value))
								stance |= Stance.Ally;
							else
								stance &= ~Stance.Ally;

							node.Value.Nodes.Remove(allowAllies);
						}

						var allowNeutral = node.Value.Nodes.FirstOrDefault(n => n.Key == "AllowNeutral");
						if (allowNeutral != null)
						{
							if (FieldLoader.GetValue<bool>("AllowNeutral", allowNeutral.Value.Value))
								stance |= Stance.Neutral;
							else
								stance &= ~Stance.Neutral;

							node.Value.Nodes.Remove(allowNeutral);
						}

						var allowEnemies = node.Value.Nodes.FirstOrDefault(n => n.Key == "AllowEnemies");
						if (allowEnemies != null)
						{
							if (FieldLoader.GetValue<bool>("AllowEnemies", allowEnemies.Value.Value))
								stance |= Stance.Enemy;
							else
								stance &= ~Stance.Enemy;

							node.Value.Nodes.Remove(allowEnemies);
						}

						if (stance != (Stance.Neutral | Stance.Enemy))
							node.Value.Nodes.Add(new MiniYamlNode("ValidStances", stance.ToString()));
					}
				}

				// Self-reload properties were decoupled from AmmoPool to ReloadAmmoPool.
				if (engineVersion < 20171104)
				{
					var poolNumber = 0;
					var ammoPools = node.Value.Nodes.Where(n => n.Key.StartsWith("AmmoPool", StringComparison.Ordinal));
					foreach (var pool in ammoPools.ToList())
					{
						var selfReloads = pool.Value.Nodes.FirstOrDefault(n => n.Key == "SelfReloads");
						if (selfReloads != null && FieldLoader.GetValue<bool>("SelfReloads", selfReloads.Value.Value))
						{
							poolNumber++;
							var name = pool.Value.Nodes.FirstOrDefault(n => n.Key == "Name");
							var selfReloadDelay = pool.Value.Nodes.FirstOrDefault(n => n.Key == "SelfReloadDelay");
							var reloadCount = pool.Value.Nodes.FirstOrDefault(n => n.Key == "ReloadCount");
							var reset = pool.Value.Nodes.FirstOrDefault(n => n.Key == "ResetOnFire");
							var rearmSound = pool.Value.Nodes.FirstOrDefault(n => n.Key == "RearmSound");
							var reloadOnCond = new MiniYamlNode("ReloadAmmoPool@" + poolNumber.ToString(), "");

							if (name != null)
							{
								var ap = new MiniYamlNode("AmmoPool", name.Value.Value);
								reloadOnCond.Value.Nodes.Add(ap);
							}

							if (selfReloadDelay != null)
							{
								var rd = selfReloadDelay;
								RenameNodeKey(rd, "Delay");
								reloadOnCond.Value.Nodes.Add(rd);
								pool.Value.Nodes.Remove(selfReloads);
								pool.Value.Nodes.Remove(selfReloadDelay);
							}

							if (reloadCount != null)
							{
								var rc = reloadCount;
								RenameNodeKey(rc, "Count");
								reloadOnCond.Value.Nodes.Add(rc);
								pool.Value.Nodes.Remove(reloadCount);
							}

							if (reset != null)
							{
								reloadOnCond.Value.Nodes.Add(reset);
								pool.Value.Nodes.Remove(reset);
							}

							if (rearmSound != null)
							{
								var rs = rearmSound;
								RenameNodeKey(rs, "Sound");
								reloadOnCond.Value.Nodes.Add(rs);
								pool.Value.Nodes.Remove(rearmSound);
							}

							node.Value.Nodes.Add(reloadOnCond);
						}
					}
				}

				// Armament.OutOfAmmo has been replaced by pausing on condition (usually provided by AmmoPool)
				if (engineVersion < 20171104)
				{
					var reloadAmmoPool = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("ReloadAmmoPool", StringComparison.Ordinal));
					var armaments = node.Value.Nodes.Where(n => n.Key.StartsWith("Armament", StringComparison.Ordinal));
					var ammoPools = node.Value.Nodes.Where(n => n.Key.StartsWith("AmmoPool", StringComparison.Ordinal));

					if (reloadAmmoPool == null && armaments.Any() && ammoPools.Any())
					{
						foreach (var pool in ammoPools)
						{
							var nameNode = pool.Value.Nodes.FirstOrDefault(n => n.Key == "Armaments");
							var name = nameNode != null ? FieldLoader.GetValue<string>("Armaments", nameNode.Value.Value) : "primary, secondary";
							var anyMatchingArmament = false;
							var ammoNoAmmo = new MiniYamlNode("AmmoCondition", "ammo");
							var armNoAmmo = new MiniYamlNode("PauseOnCondition", "!ammo");

							foreach (var arma in armaments)
							{
								var armaNameNode = arma.Value.Nodes.FirstOrDefault(n => n.Key == "Name");
								var armaName = armaNameNode != null ? FieldLoader.GetValue<string>("Name", armaNameNode.Value.Value) : "primary";
								if (name.Contains(armaName))
								{
									anyMatchingArmament = true;
									arma.Value.Nodes.Add(armNoAmmo);
								}
							}

							if (anyMatchingArmament)
							{
								pool.Value.Nodes.Add(ammoNoAmmo);
								Console.WriteLine("Aircraft returning to base is now triggered when all armaments are paused via condition.");
								Console.WriteLine("Check if any of your actors with AmmoPools may need further changes.");
							}
						}
					}
				}

				if (engineVersion < 20171112)
				{
					// CanPowerDown now provides a condition instead of triggering Actor.Disabled
					var canPowerDown = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("CanPowerDown", StringComparison.Ordinal));
					if (canPowerDown != null)
					{
						canPowerDown.Value.Nodes.Add(new MiniYamlNode("PowerdownCondition", "powerdown"));

						var image = canPowerDown.Value.Nodes.FirstOrDefault(n => n.Key == "IndicatorImage");
						var seq = canPowerDown.Value.Nodes.FirstOrDefault(n => n.Key == "IndicatorSequence");
						var pal = canPowerDown.Value.Nodes.FirstOrDefault(n => n.Key == "IndicatorPalette");
						var imageValue = image != null ? FieldLoader.GetValue<string>("IndicatorImage", image.Value.Value) : "poweroff";
						var seqValue = seq != null ? FieldLoader.GetValue<string>("IndicatorSequence", seq.Value.Value) : "offline";
						var palValue = pal != null ? FieldLoader.GetValue<string>("IndicatorPalette", pal.Value.Value) : "chrome";

						var indicator = new MiniYamlNode("WithDecoration@POWERDOWN", "");
						indicator.Value.Nodes.Add(new MiniYamlNode("Image", imageValue));
						indicator.Value.Nodes.Add(new MiniYamlNode("Sequence", seqValue));
						indicator.Value.Nodes.Add(new MiniYamlNode("Palette", palValue));
						indicator.Value.Nodes.Add(new MiniYamlNode("RequiresCondition", "powerdown"));
						indicator.Value.Nodes.Add(new MiniYamlNode("ReferencePoint", "Center"));

						node.Value.Nodes.Add(indicator);
						if (image != null)
							canPowerDown.Value.Nodes.Remove(image);
						if (seq != null)
							canPowerDown.Value.Nodes.Remove(seq);
						if (pal != null)
							canPowerDown.Value.Nodes.Remove(pal);

						Console.WriteLine("CanPowerDown now provides a condition instead of disabling the actor directly.");
						Console.WriteLine("Review your condition setup to make sure all relevant traits are disabled by that condition.");
						Console.WriteLine("Look at the official mods if you need examples.");
					}

					// RequiresPower has been replaced with GrantConditionOnPowerState.
					var requiresPower = node.Value.Nodes.FirstOrDefault(n => n.Key == "RequiresPower");
					if (requiresPower != null)
					{
						requiresPower.Key = "GrantConditionOnPowerState@LOWPOWER";
						requiresPower.Value.Nodes.Add(new MiniYamlNode("Condition", "lowpower"));
						requiresPower.Value.Nodes.Add(new MiniYamlNode("ValidPowerStates", "Low, Critical"));

						Console.WriteLine("RequiresPower has been replaced with GrantConditionOnPowerState.");
						Console.WriteLine("As the name implies, this new trait toggles a condition depending on the power state.");
						Console.WriteLine("Review your condition setup to make sure all relevant traits are disabled/enabled by that condition.");
						Console.WriteLine("Possible PowerStates are: Normal (0 or positive), Low (negative but higher than 50% of required power) and Critical (below Low).");
						Console.WriteLine("Look at the official mods if you need examples.");
					}

					// Made WithSpriteBody a PausableConditionalTrait, allowing to drop the PauseAnimationWhenDisabled property
					var wsbPause = node.Value.Nodes.FirstOrDefault(n => n.Key == "PauseAnimationWhenDisabled");
					if (wsbPause != null)
					{
						wsbPause.Key = "PauseOnCondition";
						wsbPause.Value.Value = "disabled";
					}
				}

				if (engineVersion < 20171120)
				{
					// AreaTypes support is added to GivesBuildableArea and it is required.
					var givesBuildableArea = node.Value.Nodes.FirstOrDefault(n => n.Key == "GivesBuildableArea");
					if (givesBuildableArea != null)
						givesBuildableArea.Value.Nodes.Add(new MiniYamlNode("AreaTypes", "building"));

					// RequiresBuildableArea trait is added and Building.Adjacent is moved there.
					var building = node.Value.Nodes.FirstOrDefault(n => n.Key == "Building");
					if (building != null)
					{
						var adjacent = building.Value.Nodes.FirstOrDefault(n => n.Key == "Adjacent");
						var areaTypes = new MiniYamlNode("AreaTypes", "building");
						var requiresBuildableArea = new MiniYamlNode("RequiresBuildableArea", "");

						requiresBuildableArea.Value.Nodes.Add(areaTypes);
						if (adjacent != null)
							requiresBuildableArea.Value.Nodes.Add(adjacent);

						node.Value.Nodes.Add(requiresBuildableArea);
						building.Value.Nodes.Remove(adjacent);
					}
				}

				// Split Selection- and RenderSize
				if (engineVersion < 20171115)
				{
					var autoSelSize = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("AutoSelectionSize", StringComparison.Ordinal));
					if (autoSelSize != null)
						node.Value.Nodes.Add(new MiniYamlNode("AutoRenderSize", ""));

					var customSelSize = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("CustomSelectionSize", StringComparison.Ordinal));
					if (customSelSize != null)
					{
						var bounds = customSelSize.Value.Nodes.FirstOrDefault(n => n.Key == "CustomBounds");
						var customRenderSize = new MiniYamlNode("CustomRenderSize", "");
						if (bounds != null)
							customRenderSize.Value.Nodes.Add(bounds);

						node.Value.Nodes.Add(customRenderSize);
					}
				}

				if (engineVersion < 20171208)
				{
					// Move SelectionDecorations.VisualBounds to Selectable.Bounds
					if (node.Key.StartsWith("AutoRenderSize", StringComparison.Ordinal))
						RenameNodeKey(node, "Interactable");

					if (node.Key.StartsWith("CustomRenderSize", StringComparison.Ordinal))
					{
						RenameNodeKey(node, "Interactable");
						var boundsNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "CustomBounds");
						if (boundsNode != null)
							RenameNodeKey(boundsNode, "Bounds");
					}

					if (node.Key.StartsWith("SelectionDecorations", StringComparison.Ordinal))
					{
						var boundsNode = node.Value.Nodes.FirstOrDefault(n => n.Key == "VisualBounds");
						if (boundsNode != null)
						{
							RenameNodeKey(boundsNode, "DecorationBounds");
							node.Value.Nodes.Remove(boundsNode);
							var selectable = parent.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("Selectable", StringComparison.Ordinal));
							if (selectable == null)
							{
								selectable = new MiniYamlNode("Selectable", new MiniYaml(""));
								addNodes.Add(selectable);
							}

							selectable.Value.Nodes.Add(boundsNode);
						}
					}

					if (node.Key == "-Selectable")
						addNodes.Add(new MiniYamlNode("Interactable", new MiniYaml("")));

					if (depth == 0)
					{
						node.Value.Nodes.RemoveAll(n => n.Key.StartsWith("CustomSelectionSize", StringComparison.Ordinal));
						node.Value.Nodes.RemoveAll(n => n.Key.StartsWith("AutoSelectionSize", StringComparison.Ordinal));
					}
				}

				// Multiply all health and damage in shipping mods by 100 to avoid issues caused by rounding
				if (engineVersion < 20171212)
				{
					var mod = modData.Manifest.Id;
					if (mod == "cnc" || mod == "ra" || mod == "d2k" || mod == "ts")
					{
						if (node.Key == "HP" && parent.Key == "Health")
						{
							var oldValue = FieldLoader.GetValue<int>(node.Key, node.Value.Value);
							if (mod == "d2k")
								node.Value.Value = MultiplyByFactor(oldValue, 10);
							else
								node.Value.Value = MultiplyByFactor(oldValue, 100);
						}

						if (node.Key.StartsWith("SelfHealing"))
						{
							var step = node.Value.Nodes.FirstOrDefault(n => n.Key == "Step");
							if (step == null)
								node.Value.Nodes.Add(new MiniYamlNode("Step", "500"));
							else if (step != null)
							{
								var oldValue = FieldLoader.GetValue<int>(step.Key, step.Value.Value);
								if (mod == "d2k")
									step.Value.Value = MultiplyByFactor(oldValue, 10);
								else
									step.Value.Value = MultiplyByFactor(oldValue, 100);
							}
						}

						if (node.Key == "RepairsUnits")
						{
							var step = node.Value.Nodes.FirstOrDefault(n => n.Key == "HpPerStep");
							if (step == null)
								node.Value.Nodes.Add(new MiniYamlNode("HpPerStep", "1000"));
							else if (step != null)
							{
								var oldValue = FieldLoader.GetValue<int>(step.Key, step.Value.Value);
								if (mod == "d2k")
									step.Value.Value = MultiplyByFactor(oldValue, 10);
								else
									step.Value.Value = MultiplyByFactor(oldValue, 100);
							}
						}

						if (node.Key == "RepairableBuilding")
						{
							var step = node.Value.Nodes.FirstOrDefault(n => n.Key == "RepairStep");
							if (step == null)
								node.Value.Nodes.Add(new MiniYamlNode("RepairStep", "700"));
							else if (step != null)
							{
								var oldValue = FieldLoader.GetValue<int>(step.Key, step.Value.Value);
								if (mod == "d2k")
									step.Value.Value = MultiplyByFactor(oldValue, 10);
								else
									step.Value.Value = MultiplyByFactor(oldValue, 100);
							}
						}

						if (node.Key == "Burns")
						{
							var step = node.Value.Nodes.FirstOrDefault(n => n.Key == "Damage");
							if (step == null)
								node.Value.Nodes.Add(new MiniYamlNode("Damage", "100"));
							else if (step != null)
							{
								var oldValue = FieldLoader.GetValue<int>(step.Key, step.Value.Value);
								if (mod == "d2k")
									step.Value.Value = MultiplyByFactor(oldValue, 10);
								else
									step.Value.Value = MultiplyByFactor(oldValue, 100);
							}
						}

						if (node.Key == "DamagedByTerrain")
						{
							var step = node.Value.Nodes.FirstOrDefault(n => n.Key == "Damage");
							if (step != null)
							{
								var oldValue = FieldLoader.GetValue<int>(step.Key, step.Value.Value);
								if (mod == "d2k")
									step.Value.Value = MultiplyByFactor(oldValue, 10);
								else
									step.Value.Value = MultiplyByFactor(oldValue, 100);
							}
						}
					}
				}

				if (engineVersion < 20171212)
				{
					if (node.Key.StartsWith("SpawnMPUnits", StringComparison.Ordinal))
					{
						var locked = node.Value.Nodes.FirstOrDefault(n => n.Key == "Locked");
						if (locked != null)
							locked.Key = "DropdownLocked";
					}

					if (node.Key.StartsWith("Shroud", StringComparison.Ordinal))
					{
						var fogLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "FogLocked");
						if (fogLocked != null)
							fogLocked.Key = "FogCheckboxLocked";

						var fogEnabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "FogEnabled");
						if (fogEnabled != null)
							fogEnabled.Key = "FogCheckboxEnabled";

						var exploredMapLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "ExploredMapLocked");
						if (exploredMapLocked != null)
							exploredMapLocked.Key = "ExploredMapCheckboxLocked";

						var exploredMapEnabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "ExploredMapEnabled");
						if (exploredMapEnabled != null)
							exploredMapEnabled.Key = "ExploredMapCheckboxEnabled";
					}

					if (node.Key.StartsWith("MapOptions", StringComparison.Ordinal))
					{
						var shortGameLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShortGameLocked");
						if (shortGameLocked != null)
							shortGameLocked.Key = "ShortGameCheckboxLocked";

						var shortGameEnabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "ShortGameEnabled");
						if (shortGameEnabled != null)
							shortGameEnabled.Key = "ShortGameCheckboxEnabled";

						var techLevelLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "TechLevelLocked");
						if (techLevelLocked != null)
							techLevelLocked.Key = "TechLevelDropdownLocked";

						var gameSpeedLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "GameSpeedLocked");
						if (gameSpeedLocked != null)
							gameSpeedLocked.Key = "GameSpeedDropdownLocked";
					}

					if (node.Key.StartsWith("MapCreeps", StringComparison.Ordinal))
					{
						var locked = node.Value.Nodes.FirstOrDefault(n => n.Key == "Locked");
						if (locked != null)
							locked.Key = "CheckboxLocked";

						var enabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "Enabled");
						if (enabled != null)
							enabled.Key = "CheckboxEnabled";
					}

					if (node.Key.StartsWith("MapBuildRadius", StringComparison.Ordinal))
					{
						var alllyLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "AllyBuildRadiusLocked");
						if (alllyLocked != null)
							alllyLocked.Key = "AllyBuildRadiusCheckboxLocked";

						var allyEnabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "AllyBuildRadiusEnabled");
						if (allyEnabled != null)
							allyEnabled.Key = "AllyBuildRadiusCheckboxEnabled";

						var buildRadiusLocked = node.Value.Nodes.FirstOrDefault(n => n.Key == "BuildRadiusLocked");
						if (buildRadiusLocked != null)
							buildRadiusLocked.Key = "BuildRadiusCheckboxLocked";

						var buildRadiusEnabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "BuildRadiusEnabled");
						if (buildRadiusEnabled != null)
							buildRadiusEnabled.Key = "BuildRadiusCheckboxEnabled";
					}

					if (node.Key.StartsWith("DeveloperMode", StringComparison.Ordinal))
					{
						var locked = node.Value.Nodes.FirstOrDefault(n => n.Key == "Locked");
						if (locked != null)
							locked.Key = "CheckboxLocked";

						var enabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "Enabled");
						if (enabled != null)
							enabled.Key = "CheckboxEnabled";
					}

					if (node.Key.StartsWith("CrateSpawner", StringComparison.Ordinal))
					{
						var locked = node.Value.Nodes.FirstOrDefault(n => n.Key == "Locked");
						if (locked != null)
							locked.Key = "CheckboxLocked";

						var enabled = node.Value.Nodes.FirstOrDefault(n => n.Key == "Enabled");
						if (enabled != null)
							enabled.Key = "CheckboxEnabled";
					}

					if (node.Key.StartsWith("PlayerResources", StringComparison.Ordinal))
					{
						var locked = node.Value.Nodes.FirstOrDefault(n => n.Key == "Locked");
						if (locked != null)
							locked.Key = "DefaultCashDropdownLocked";
					}
				}

				// Made Gate not inherit Building
				if (engineVersion < 20171119)
				{
					var gate = node.Value.Nodes.FirstOrDefault(n => n.Key == "Gate");
					if (gate != null)
					{
						var openSound = gate.Value.Nodes.FirstOrDefault(n => n.Key == "OpeningSound");
						var closeSound = gate.Value.Nodes.FirstOrDefault(n => n.Key == "ClosingSound");
						var closeDelay = gate.Value.Nodes.FirstOrDefault(n => n.Key == "CloseDelay");
						var transitDelay = gate.Value.Nodes.FirstOrDefault(n => n.Key == "TransitionDelay");
						var blockHeight = gate.Value.Nodes.FirstOrDefault(n => n.Key == "BlocksProjectilesHeight");

						gate.Key = "Building";
						var newGate = new MiniYamlNode("Gate", "");

						if (openSound != null)
						{
							newGate.Value.Nodes.Add(openSound);
							gate.Value.Nodes.Remove(openSound);
						}

						if (closeSound != null)
						{
							newGate.Value.Nodes.Add(closeSound);
							gate.Value.Nodes.Remove(closeSound);
						}

						if (closeDelay != null)
						{
							newGate.Value.Nodes.Add(closeDelay);
							gate.Value.Nodes.Remove(closeDelay);
						}

						if (transitDelay != null)
						{
							newGate.Value.Nodes.Add(transitDelay);
							gate.Value.Nodes.Remove(transitDelay);
						}

						if (blockHeight != null)
						{
							newGate.Value.Nodes.Add(blockHeight);
							gate.Value.Nodes.Remove(blockHeight);
						}

						node.Value.Nodes.Add(newGate);
					}
				}

				// Removed IDisable interface and all remaining usages
				if (engineVersion < 20171119)
				{
					var doc = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("DisableOnCondition", StringComparison.Ordinal));
					if (doc != null)
					{
						Console.WriteLine("Actor.IsDisabled has been removed in favor of pausing/disabling traits via conditions.");
						Console.WriteLine("DisableOnCondition was a stop-gap solution that has been removed along with it.");
						Console.WriteLine("You'll have to use RequiresCondition or PauseOnCondition on individual traits to 'disable' actors.");
						node.Value.Nodes.Remove(doc);
					}

					var grant = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("GrantConditionOnDisabled", StringComparison.Ordinal));
					if (grant != null)
					{
						Console.WriteLine("Actor.IsDisabled has been removed in favor of pausing/disabling traits via conditions.");
						Console.WriteLine("GrantConditionOnDisabled was a stop-gap solution that has been removed along with it.");
						Console.WriteLine("You'll have to use RequiresCondition or PauseOnCondition on individual traits to 'disable' actors.");
						node.Value.Nodes.Remove(grant);
					}
				}

				// CanPowerDown was replaced with a more general trait for toggling a condition
				if (engineVersion < 20171225)
				{
					var cpd = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("CanPowerDown", StringComparison.Ordinal));
					if (cpd != null)
					{
						RenameNodeKey(cpd, "ToggleConditionOnOrder");

						RenameNodeKey(cpd.Value.Nodes.FirstOrDefault(n => n.Key == "PowerupSound"), "DisabledSound");
						RenameNodeKey(cpd.Value.Nodes.FirstOrDefault(n => n.Key == "PowerupSpeech"), "DisabledSpeech");
						RenameNodeKey(cpd.Value.Nodes.FirstOrDefault(n => n.Key == "PowerdownSound"), "EnabledSound");
						RenameNodeKey(cpd.Value.Nodes.FirstOrDefault(n => n.Key == "PowerdownSpeech"), "EnabledSpeech");
						cpd.Value.Nodes.Add(new MiniYamlNode("OrderName", "PowerDown"));

						var condition = cpd.Value.Nodes.FirstOrDefault(n => n.Key == "PowerdownCondition");
						if (condition != null)
							RenameNodeKey(condition, "Condition");
						else
							cpd.Value.Nodes.Add(new MiniYamlNode("Condition", "powerdown"));

						if (cpd.Value.Nodes.RemoveAll(n => n.Key == "CancelWhenDisabled") > 0)
						{
							Console.WriteLine("CancelWhenDisabled was removed when CanPowerDown was replaced by ToggleConditionOnOrder");
							Console.WriteLine("Use PauseOnCondition instead of RequiresCondition to replicate the behavior of 'false'.");
						}

						node.Value.Nodes.Add(new MiniYamlNode("PowerMultiplier@POWERDOWN", new MiniYaml("", new List<MiniYamlNode>()
						{
							new MiniYamlNode("RequiresCondition", condition.Value.Value),
							new MiniYamlNode("Modifier", "0")
						})));
					}
				}

				if (engineVersion < 20171228)
				{
					var chargeTime = node.Value.Nodes.FirstOrDefault(n => n.Key == "ChargeTime");
					if (chargeTime != null)
					{
						var chargeTimeValue = FieldLoader.GetValue<int>("ChargeTime", chargeTime.Value.Value);
						if (chargeTimeValue > 0)
							chargeTime.Value.Value = (chargeTimeValue * 25).ToString();

						RenameNodeKey(chargeTime, "ChargeInterval");
					}

					if (node.Key.StartsWith("GpsPower", StringComparison.Ordinal))
					{
						var revealDelay = node.Value.Nodes.FirstOrDefault(n => n.Key == "RevealDelay");
						var revealDelayValue = revealDelay != null ? FieldLoader.GetValue<int>("RevealDelay", revealDelay.Value.Value) : 0;
						if (revealDelay != null && revealDelayValue > 0)
							revealDelay.Value.Value = (revealDelayValue * 25).ToString();
					}

					if (node.Key.StartsWith("ChronoshiftPower", StringComparison.Ordinal))
					{
						var duration = node.Value.Nodes.FirstOrDefault(n => n.Key == "Duration");
						var durationValue = duration != null ? FieldLoader.GetValue<int>("Duration", duration.Value.Value) : 0;
						if (duration != null && durationValue > 0)
							duration.Value.Value = (durationValue * 25).ToString();
					}
				}

				if (engineVersion < 20180308)
				{
					if (node.Key == "WormSpawner")
						RenameNodeKey(node, "ActorSpawner");

					if (node.Key == "WormManager")
					{
						RenameNodeKey(node, "ActorSpawnManager");

						var wormSignature = node.Value.Nodes.FirstOrDefault(n => n.Key == "WormSignature");
						if (wormSignature != null)
							wormSignature.Key = "Actors";
					}
				}

				// Removed WithReloadingSpriteTurret
				if (engineVersion < 20180308)
				{
					var reloadingTurret = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("WithReloadingSpriteTurret", StringComparison.Ordinal));
					if (reloadingTurret != null)
					{
						var ammoPool = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("AmmoPool", StringComparison.Ordinal));
						if (ammoPool != null)
							ammoPool.Value.Nodes.Add(new MiniYamlNode("AmmoCondition", "ammo"));

						RenameNodeKey(reloadingTurret, "WithSpriteTurret");
						var noAmmoTurret = new MiniYamlNode("WithSpriteTurret@NoAmmo", "");
						var reqAmmoCondition = new MiniYamlNode("RequiresCondition", "ammo");
						var reqNoAmmoCondition = new MiniYamlNode("RequiresCondition", "!ammo");

						reloadingTurret.Value.Nodes.Add(reqAmmoCondition);
						noAmmoTurret.Value.Nodes.Add(reqNoAmmoCondition);
						node.Value.Nodes.Add(noAmmoTurret);

						Console.WriteLine("WithReloadingSpriteTurret has been removed in favor of using stacked AmmoPool.AmmoConditions.");
						Console.WriteLine("Check if your affected actors need further changes.");
					}
				}

				if (engineVersion < 20180309)
				{
					if (node.Key == "ParaDrop")
					{
						var soundNodePD = node.Value.Nodes.FirstOrDefault(n => n.Key == "ChuteSound");
						if (soundNodePD == null)
							node.Value.Nodes.Add(new MiniYamlNode("ChuteSound", "chute1.aud"));
					}

					if (depth == 1 && node.Key == "EjectOnDeath")
					{
						var soundNodeEOD = node.Value.Nodes.FirstOrDefault(n => n.Key == "ChuteSound");
						if (soundNodeEOD == null)
							node.Value.Nodes.Add(new MiniYamlNode("ChuteSound", "chute1.aud"));
					}

					if (node.Key.StartsWith("ProductionParadrop", StringComparison.Ordinal))
					{
						var soundNodePP = node.Value.Nodes.FirstOrDefault(n => n.Key == "ChuteSound");
						if (soundNodePP == null)
							node.Value.Nodes.Add(new MiniYamlNode("ChuteSound", "chute1.aud"));
					}

					if (node.Key == "Building")
					{
						var soundNodeB1 = node.Value.Nodes.FirstOrDefault(n => n.Key == "BuildSounds");
						if (soundNodeB1 == null)
							node.Value.Nodes.Add(new MiniYamlNode("BuildSounds", "placbldg.aud, build5.aud"));

						var soundNodeB2 = node.Value.Nodes.FirstOrDefault(n => n.Key == "UndeploySounds");
						if (soundNodeB2 == null)
							node.Value.Nodes.Add(new MiniYamlNode("UndeploySounds", "cashturn.aud"));
					}
				}

				// Split aim animation logic from WithTurretAttackAnimation to separate WithTurretAimAnimation
				if (engineVersion < 20180309)
				{
					var turAttackAnim = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("WithTurretAttackAnimation", StringComparison.Ordinal));
					if (turAttackAnim != null)
					{
						var atkSequence = turAttackAnim.Value.Nodes.FirstOrDefault(n => n.Key == "AttackSequence");
						var aimSequence = turAttackAnim.Value.Nodes.FirstOrDefault(n => n.Key == "AimSequence");

						// If only AimSequence is null, just rename AttackSequence to Sequence (ReloadPrefix is very unlikely to be defined in that case).
						// If only AttackSequence is null, just rename the trait and property (the delay properties will likely be undefined).
						// If both aren't null, split/copy everything relevant to the new WithTurretAimAnimation.
						// If both are null (extremely unlikely), do nothing.
						if (atkSequence == null && aimSequence != null)
						{
							RenameNodeKey(turAttackAnim, "WithTurretAimAnimation");
							RenameNodeKey(aimSequence, "Sequence");
						}
						else if (atkSequence != null && aimSequence == null)
							RenameNodeKey(atkSequence, "Sequence");
						else if (atkSequence != null && aimSequence != null)
						{
							var aimAnim = new MiniYamlNode("WithTurretAimAnimation", "");
							RenameNodeKey(aimSequence, "Sequence");
							aimAnim.Value.Nodes.Add(aimSequence);
							turAttackAnim.Value.Nodes.Remove(aimSequence);

							var relPrefix = turAttackAnim.Value.Nodes.FirstOrDefault(n => n.Key == "ReloadPrefix");
							var turr = turAttackAnim.Value.Nodes.FirstOrDefault(n => n.Key == "Turret");
							var arm = turAttackAnim.Value.Nodes.FirstOrDefault(n => n.Key == "Armament");
							if (relPrefix != null)
							{
								aimAnim.Value.Nodes.Add(relPrefix);
								turAttackAnim.Value.Nodes.Remove(relPrefix);
							}

							if (turr != null)
								aimAnim.Value.Nodes.Add(turr);
							if (arm != null)
								aimAnim.Value.Nodes.Add(arm);

							RenameNodeKey(atkSequence, "Sequence");
							node.Value.Nodes.Add(aimAnim);
						}
					}
				}

				// Removed AimSequence from WithSpriteTurret, use WithTurretAimAnimation instead
				if (engineVersion < 20180309)
				{
					var spriteTurret = node.Value.Nodes.FirstOrDefault(n => n.Key.StartsWith("WithSpriteTurret", StringComparison.Ordinal));
					if (spriteTurret != null)
					{
						var aimSequence = spriteTurret.Value.Nodes.FirstOrDefault(n => n.Key == "AimSequence");
						if (aimSequence != null)
						{
							var aimAnim = new MiniYamlNode("WithTurretAimAnimation", "");
							RenameNodeKey(aimSequence, "Sequence");
							aimAnim.Value.Nodes.Add(aimSequence);
							spriteTurret.Value.Nodes.Remove(aimSequence);
							node.Value.Nodes.Add(aimAnim);
						}
					}
				}

				UpgradeActorRules(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}

			foreach (var a in addNodes)
				nodes.Add(a);
		}

		internal static void UpgradeWeaponRules(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Multiply all health and damage in shipping mods by 100 to avoid issues caused by rounding
				if (engineVersion < 20171212)
				{
					var mod = modData.Manifest.Id;
					if (mod == "cnc" || mod == "ra" || mod == "d2k" || mod == "ts")
					{
						if (node.Key == "Damage" && (parent.Value.Value == "SpreadDamage" || parent.Value.Value == "TargetDamage"))
						{
							var oldValue = FieldLoader.GetValue<int>(node.Key, node.Value.Value);
							if (mod == "d2k")
								node.Value.Value = MultiplyByFactor(oldValue, 10);
							else
								node.Value.Value = MultiplyByFactor(oldValue, 100);
						}
					}
				}

				if (engineVersion < 20180219)
				{
					if (node.Key.StartsWith("Warhead", StringComparison.Ordinal) && node.Value.Value == "CreateEffect")
					{
						var victimScanRadius = node.Value.Nodes.FirstOrDefault(n => n.Key == "VictimScanRadius");
						if (victimScanRadius != null)
						{
							if (FieldLoader.GetValue<int>(victimScanRadius.Key, victimScanRadius.Value.Value) == 0)
								node.Value.Nodes.Add(new MiniYamlNode("ImpactActors", "false"));

							node.Value.Nodes.Remove(victimScanRadius);
						}
					}

					if (node.Key.StartsWith("Projectile"))
						node.Value.Nodes.RemoveAll(n => n.Key == "BounceBlockerScanRadius" || n.Key == "BlockerScanRadius" || n.Key == "AreaVictimScanRadius");
				}

				UpgradeWeaponRules(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeSequences(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeSequences(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeTileset(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Removed IsWater.
				if (engineVersion < 20180222)
				{
					if (node.Key == "TerrainType" || node.Key.StartsWith("TerrainType@", StringComparison.Ordinal))
					{
						var isWater = node.Value.Nodes.FirstOrDefault(n => n.Key == "IsWater");
						if (isWater != null)
							node.Value.Nodes.Remove(isWater);
					}
				}
			}
		}

		internal static void UpgradeCursors(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeCursors(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradePlayers(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradePlayers(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeMetrics(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeMetrics(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeChromeLayout(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeChromeLayout(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
			}
		}

		internal static void UpgradeActors(ModData modData, int engineVersion, ref List<MiniYamlNode> nodes, MiniYamlNode parent, int depth)
		{
			foreach (var node in nodes)
			{
				// Add rules here
				UpgradeActors(modData, engineVersion, ref node.Value.Nodes, node, depth + 1);
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
			if (mapFormat < 11)
				throw new InvalidDataException("Map format {0} is not supported.\n File: {1}".F(mapFormat, package.Name));

			if (mapFormat < Map.SupportedMapFormat)
			{
				yaml.Nodes.First(n => n.Key == "MapFormat").Value = new MiniYaml(Map.SupportedMapFormat.ToString());
				Console.WriteLine("Converted {0} to MapFormat {1}.", package.Name, Map.SupportedMapFormat);
			}

			package.Update("map.yaml", Encoding.UTF8.GetBytes(yaml.Nodes.WriteToString()));
		}
	}
}
