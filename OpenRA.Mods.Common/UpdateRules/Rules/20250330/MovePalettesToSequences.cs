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
	public class MovePalettesToSequences : UpdateRule, IBeforeUpdateActors, IBeforeUpdateWeapons, IBeforeUpdateSequences
	{
		public const string TerrainPaletteInternalName = "terrain";

		public override string Name => "Move Palettes to Sequences";
		public override string Description => "Moves palette definitions from individual actors to a shared sequence.";

		record struct SequenceDefinitions
		{
			public string Sequence;
			public string Palette;
			public string ShadowPalette;
			public bool IsPlayerPalette;
		}

		readonly Dictionary<string, List<string>> traitRemovals = new()
		{
			["RenderSprites"] = ["Palette", "PlayerPalette"],
			["RenderSpritesEditorOnly"] = ["Palette", "PlayerPalette"],
			["FootprintPlaceBuildingPreview"] = ["Palette"],
			["ActorPreviewPlaceBuildingPreview"] = ["Palette"],
			["SequencePlaceBuildingPreview"] = ["Palette"],
			["D2kActorPreviewPlaceBuildingPreview"] = ["Palette"],
			["OrderEffects"] = ["TerrainFlashPalette"],
			["GpsDot"] = ["IndicatorPalettePrefix"],
			["WithDeadBridgeSpriteBody"] = ["EditorSequence", "EditorPalette"],
			["WithIdleOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithInfantryBody"] = ["Palette", "IsPlayerPalette"],
			["WithParachute"] = ["Palette", "IsPlayerPalette"],
			["WithSpriteTurret"] = ["Palette", "IsPlayerPalette"],
			["WithSwitchableOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithSupportPowerActivationOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithChargeSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithFacingSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithResourceLevelSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithWallSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithHarvesterSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithBridgeSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithAttackOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithBuildingPlacedOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithChargeOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithResourceLevelOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithRepairOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithProductionOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithMakeOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithHarvestOverlay"] = ["Palette"],
			["WithDockingOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithDockedOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithDamageOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithEmbeddedTurretSpriteBody"] = ["Palette", "IsPlayerPalette"],
			["WithDeliveryOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithSplitAttackPaletteInfantryBody"] = ["Palette", "IsPlayerPalette", "SplitAttackPalette"],
			["WithDisguisingInfantryBody"] = ["Palette", "IsPlayerPalette"],
			["WithCrumbleOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithTeslaChargeOverlay"] = ["Palette", "IsPlayerPalette"],
			["WithDeathAnimation"] = ["DeathSequencePalette", "DeathPaletteIsPlayerPalette", "CrushedSequencePalette", "CrushedPaletteIsPlayerPalette"],
			["WithResourceAnimation"] = ["Palette"],
			["GrantConditionOnSubterraneanLayer"] = ["SubterraneanTransitionPalette"],
			["Parachutable"] = ["GroundCorpsePalette", "WaterCorpsePalette"],
			["WithAircraftLandingEffect"] = ["Palette"],
			["SpawnActorPower"] = ["EffectPalette", "EffectPaletteIsPlayerPalette"],
			["SmudgeLayer"] = ["Palette", "SmokePalette"],
			["CrateAction"] = ["Palette"],
			["DuplicateUnitCrateAction"] = ["Palette"],
			["ExplodeCrateAction"] = ["Palette"],
			["GiveCashCrateAction"] = ["Palette"],
			["GiveUnitCrateAction"] = ["Palette"],
			["GrantExternalConditionCrateAction"] = ["Palette"],
			["HealActorsCrateAction"] = ["Palette"],
			["HideMapCrateAction"] = ["Palette"],
			["LevelUpCrateAction"] = ["Palette"],
			["RevealMapCrateAction"] = ["Palette"],
			["SupportPowerCrateAction"] = ["Palette"],
			["GainsExperience"] = ["LevelUpPalette"],
			["LeavesTrails"] = ["Palette"],
			["Cloak"] = ["EffectPalette", "EffectPaletteIsPlayerPalette"],
			["Buldable"] = ["IconPalette", "IconPaletteIsPlayerPalette"],
			["RallyPoint"] = ["Palette", "IsPlayerPalette"],
			["PlaceBeacon"] = ["Palette", "IsPlayerPalette"],
			["SupportPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["AirstrikePower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette", "DirectionArrowPalette"],
			["NukePower"] = ["MissilePalette", "IsPlayerPalette", "TrailPalette", "TrailUsePlayerPalette",
				"IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["GpsPower"] = ["DoorPalette", "DoorPaletteIsPlayerPalette", "SatellitePalette", "SatellitePaletteIsPlayerPalette",
				"IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["ParatroopersPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette", "DirectionArrowPalette"],
			["AttackOrderPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["ChronoshiftPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette", "TargetOverlayPalette"],
			["DropPodsPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette", "EntryEffectPalette"],
			["GrantPrerequisiteChargeDrainPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["IonCannonPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette", "EffectPalette"],
			["GrantExternalConditionPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["ProduceActorPower"] = ["IconPalette", "BeaconPalette", "BeaconPaletteIsPlayerPalette", "BeaconPosterPalette"],
			["Armament"] = ["MuzzlePalette"],
			["AttackGarrisoned"] = ["MuzzlePalette"],
			["FloatingSpriteEmitter"] = ["Palette", "IsPlayerPalette"],
			["TSVeinsRenderer"] = ["Palette"],
			["DrawLineToTarget"] = ["Palette"],
			["BuildableTerrainOverlay"] = ["Palette"],
			["WithDecoration"] = ["Palette", "IsPlayerPalette"],
			["WithBuildingRepairDecoration"] = ["Palette", "IsPlayerPalette"],
			["InfiltrateForDecoration"] = ["Palette", "IsPlayerPalette"],
			["ShroudRenderer"] = ["ShroudPalette", "FogPalette"],
			["ProductionIconOverlayManager"] = ["Palette"],
			["WithAmmoPipsDecoration"] = ["Palette"],
			["WithCargoPipsDecoration"] = ["Palette"],
			["WithResourceStoragePipsDecoration"] = ["Palette"],
			["WithSpriteControlGroupDecoration"] = ["Palette"],
			["WithStoresResourcesPipsDecoration"] = ["Palette"],
		};

		readonly Dictionary<string, List<string>> projectileRemovals = new()
		{
			["TeslaZap"] = ["Palette"],
			["Bullet"] = ["Palette", "IsPlayerPalette", "TrailPalette", "TrailUsePlayerPalette"],
			["Missile"] = ["Palette", "IsPlayerPalette", "TrailPalette", "TrailUsePlayerPalette"],
			["LaserZap"] = ["HitAnimPalette", "LaunchEffectPalette"],
			["GravityBomb"] = ["Palette", "IsPlayerPalette"],
			["Railgun"] = ["HitAnimPalette"]
		};

		readonly Dictionary<string, List<string>> warheadRemovals = new()
		{
			["CreateEffect"] = ["ExplosionPalette", "UsePlayerPalette"],
		};

		readonly Dictionary<string, List<string>> widgetRemovals = new()
		{
			["SupportPowers"] = ["ClockPalette"],
			["ProductionPalette"] = ["ClockPalette", "NotBuildablePalette"],
		};

		readonly Dictionary<string, HashSet<SequenceDefinitions>> sequenceDefinitions = [];
		readonly Dictionary<string, string> queuedSequenceDefinitions = [];
		readonly Dictionary<string, List<MiniYamlNodeBuilder>> globalArmaments = [];
		List<MiniYamlNodeBuilder> resolvedSequences;

		SequenceDefinitions AddSequenceDefinitions(string image, string sequence, string palette, string shadowPalette = null, bool isPlayerPalette = false)
		{
			if (string.IsNullOrEmpty(image) || string.IsNullOrEmpty(sequence) || (string.IsNullOrEmpty(palette) && string.IsNullOrEmpty(shadowPalette)))
				return default;

			var definition = new SequenceDefinitions
			{
				Sequence = sequence,
				Palette = palette,
				ShadowPalette = shadowPalette,
				IsPlayerPalette = isPlayerPalette
			};

			var lowerImage = image.ToLowerInvariant();
			if (!sequenceDefinitions.TryGetValue(lowerImage, out var sequences))
				sequenceDefinitions[lowerImage] = [definition];
			else
				sequences.Add(definition);

			return definition;
		}

		static bool TryGetString(MiniYamlNodeBuilder node, out string value, string propertyName, string defaultValue = null)
		{
			var valueNode = node.LastChildMatching(propertyName);
			if (valueNode == null || valueNode.IsRemoval())
			{
				value = defaultValue;
				return !string.IsNullOrEmpty(value);
			}

			value = valueNode.Value.Value;
			return !string.IsNullOrEmpty(value);
		}

		static bool ParseBool(MiniYamlNodeBuilder node, string propertyName, bool defaultValue)
		{
			var valueNode = node.LastChildMatching(propertyName);
			if (valueNode == null || valueNode.IsRemoval())
				return defaultValue;

			return FieldLoader.GetValue<bool>(valueNode.Key, valueNode.Value.Value);
		}

		static string[] ParseStringArray(MiniYamlNodeBuilder node, string propertyName, string[] defaultValue = null)
		{
			var valueNode = node.LastChildMatching(propertyName);
			if (valueNode == null || valueNode.IsRemoval())
				return defaultValue ?? [];

			return FieldLoader.GetValue<string[]>(valueNode.Key, valueNode.Value.Value);
		}

		static Dictionary<string, T> ParseDictionary<T>(MiniYamlNodeBuilder node, string propertyName, Dictionary<string, T> defaultValue = null)
		{
			var valueNode = node.LastChildMatching(propertyName);
			if (valueNode == null || valueNode.IsRemoval())
				return defaultValue ?? [];

			return (Dictionary<string, T>)FieldLoader.GetValue(valueNode.Key, typeof(Dictionary<string, T>), valueNode.Value.Build());
		}

		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			// We needs all armaments to be able to move MuzzlePalettes.
			foreach (var actorNode in resolvedActors)
			{
				foreach (var armament in actorNode.ChildrenMatching("Armament"))
				{
					if (globalArmaments.TryGetValue(actorNode.Key, out var armaments))
						armaments.Add(armament);
					else
						globalArmaments[actorNode.Key] = [armament];
				}
			}

			foreach (var actorNode in resolvedActors)
			{
				var renderSprites = actorNode.LastChildMatching("RenderSprites")
					?? actorNode.LastChildMatching("RenderSpritesEditorOnly");

				Dictionary<string, HashSet<SequenceDefinitions>> handledRenderSprites = [];

				if (renderSprites != null && !renderSprites.IsRemoval())
				{
					var isPlayerPalette = false;
					if (!TryGetString(renderSprites, out var actorPalette, "Palette")
						&& TryGetString(renderSprites, out var actorPlayerPalette, "PlayerPalette", "player"))
					{
						actorPalette = actorPlayerPalette;
						isPlayerPalette = true;
					}

					var factionImages = ParseDictionary<string>(renderSprites, "FactionImages");
					if (factionImages.Count > 0)
					{
						foreach (var factionImage in factionImages)
						{
							handledRenderSprites[factionImage.Value.ToLowerInvariant()] =
							[
								new SequenceDefinitions
								{
									Sequence = null,
									Palette = actorPalette,
									IsPlayerPalette = isPlayerPalette
								}
							];
						}
					}
					else if (TryGetString(renderSprites, out var actorImage, "Image", actorNode.Key))
					{
						handledRenderSprites[actorImage.ToLowerInvariant()] =
						[
							new SequenceDefinitions
							{
								Sequence = null,
								Palette = actorPalette,
								IsPlayerPalette = isPlayerPalette
							}
						];
					}
				}

				string[] prefixesPrefixes =
				[
					"critical-",
					"damaged-",
					"scratched-",
					"scuffed-"
				];

				HashSet<SequenceDefinitions> AddRenderSequences(
					string sequence,
					string palette,
					string shadowPalette = null,
					bool isPlayerPalette = false,
					string image = null)
				{
					var results = new HashSet<SequenceDefinitions>();
					foreach (var (renderImage, sequences) in handledRenderSprites)
					{
						var targetImage = image ?? renderImage;
						if (palette == null && shadowPalette == null)
						{
							foreach (var seq in sequences)
							{
								results.Add(AddSequenceDefinitions(targetImage, sequence, seq.Palette, shadowPalette, seq.IsPlayerPalette));
								foreach (var prefix in prefixesPrefixes)
									results.Add(AddSequenceDefinitions(targetImage, prefix + sequence, seq.Palette, shadowPalette, seq.IsPlayerPalette));
							}
						}
						else
						{
							results.Add(AddSequenceDefinitions(targetImage, sequence, palette, shadowPalette, isPlayerPalette));
							foreach (var prefix in prefixesPrefixes)
								results.Add(AddSequenceDefinitions(targetImage, prefix + sequence, palette, shadowPalette, isPlayerPalette));
						}
					}

					return results;
				}

				// Firs pass
				List<string> spriteBodyPrefixes = [];
				HashSet<SequenceDefinitions> handledInfantryBodies = [];
				var spriteBodyPalettes = new Dictionary<string, (string Palette, bool IsPlayerPalette)>();
				string drawLineToTargetPalette = null;
				foreach (var traitNode in actorNode.Value.Nodes)
				{
					if (traitNode.IsRemoval())
						continue;

					switch (traitNode.GetKey())
					{
						case "WithInfantryBody":
						case "WithDisguisingInfantryBody":
						{
							var (sequences, palette, isPlayerPalette) = HandleWithInfantryBody(traitNode);

							foreach (var seq in sequences)
								handledInfantryBodies.UnionWith(AddRenderSequences(seq.Sequence, seq.Palette, seq.ShadowPalette, seq.IsPlayerPalette));
							break;
						}

						case "WithSplitAttackPaletteInfantryBody":
						{
							var (sequences, palette, isPlayerPalette) = HandleWithInfantryBody(traitNode);

							foreach (var seq in sequences)
								handledInfantryBodies.UnionWith(AddRenderSequences(seq.Sequence, seq.Palette, seq.ShadowPalette, seq.IsPlayerPalette));

							if (!TryGetString(traitNode, out var suffix, "SplitAttackSuffix", "muzzle"))
								continue;

							TryGetString(traitNode, out var splitAttackPalette, "SplitAttackPalette");
							if (TryGetString(traitNode, out var defaultAttackSeq, "DefaultAttackSequence"))
								handledInfantryBodies.UnionWith(AddRenderSequences(defaultAttackSeq + '-' + suffix, splitAttackPalette));

							var attackSeqs = ParseDictionary<string[]>(traitNode, "AttackSequences");
							foreach (var seq in attackSeqs.Values)
								foreach (var s in seq)
									handledInfantryBodies.UnionWith(AddRenderSequences(s + '-' + suffix, splitAttackPalette));

							break;
						}

						case "WithSpriteBody":
						case "WithChargeSpriteBody":
						case "WithFacingSpriteBody":
						case "WithResourceLevelSpriteBody":
						case "WithEmbeddedTurretSpriteBody":
						case "WithDeadBridgeSpriteBody":
						case "WithWallSpriteBody":
						{
							(var sequences, var name, var spriteBodyPalette, var isPlayerPalette) = HandleWithSpriteBody(traitNode);
							spriteBodyPalettes[name] = (spriteBodyPalette, isPlayerPalette);

							foreach (var seq in sequences)
								AddRenderSequences(seq.Sequence, spriteBodyPalette, seq.ShadowPalette, seq.IsPlayerPalette);

							break;
						}

						case "WithGateSpriteBody":
						{
							(var sequences, var name, var spriteBodyPalette, var isPlayerPalette) = HandleWithSpriteBody(traitNode);
							spriteBodyPalettes[name] = (spriteBodyPalette, isPlayerPalette);

							foreach (var seq in sequences)
								AddRenderSequences(seq.Sequence, spriteBodyPalette, seq.ShadowPalette, seq.IsPlayerPalette);

							if (TryGetString(traitNode, out var openSequence, "OpenSequence"))
								AddRenderSequences(openSequence, spriteBodyPalette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithHarvesterSpriteBody":
						{
							(var sequences, var name, var spriteBodyPalette, var isPlayerPalette) = HandleWithSpriteBody(traitNode);
							spriteBodyPalettes[name] = (spriteBodyPalette, isPlayerPalette);

							var images = ParseStringArray(traitNode, "ImageByFullness");
							if (images.Length == 0)
							{
								foreach (var seq in sequences)
									AddRenderSequences(seq.Sequence, spriteBodyPalette, seq.ShadowPalette, seq.IsPlayerPalette);
							}
							else
								foreach (var image in images)
									foreach (var seq in sequences)
										AddRenderSequences(seq.Sequence, spriteBodyPalette, seq.ShadowPalette, seq.IsPlayerPalette, image);

							break;
						}

						case "WithBridgeSpriteBody":
						{
							(var bSequences, var name, var spriteBodyPalette, var isPlayerPalette) = HandleWithSpriteBody(traitNode);
							spriteBodyPalettes[name] = (spriteBodyPalette, isPlayerPalette);

							foreach (var seq in bSequences)
								AddRenderSequences(seq.Sequence, spriteBodyPalette, seq.ShadowPalette, seq.IsPlayerPalette);

							var sequences = ParseStringArray(traitNode, "Sequences", ["idle"]);
							foreach (var seq in sequences)
								AddRenderSequences(seq, spriteBodyPalette, isPlayerPalette: isPlayerPalette);

							var aDestroyedSequences = ParseStringArray(traitNode, "ADestroyedSequences", ["adestroyed"]);
							foreach (var sequence in aDestroyedSequences)
								AddRenderSequences(sequence, spriteBodyPalette, isPlayerPalette: isPlayerPalette);

							var bDestroyedSequences = ParseStringArray(traitNode, "BDestroyedSequences", ["bdestroyed"]);
							foreach (var sequence in bDestroyedSequences)
								AddRenderSequences(sequence, spriteBodyPalette, isPlayerPalette: isPlayerPalette);

							var abDestroyedSequences = ParseStringArray(traitNode, "ABDestroyedSequences", ["abdestroyed"]);
							foreach (var sequence in abDestroyedSequences)
								AddRenderSequences(sequence, spriteBodyPalette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "DrawLineToTarget":
						{
							TryGetString(traitNode, out drawLineToTargetPalette, "Palette", TerrainPaletteInternalName);
							break;
						}

						case "ScaredyCat":
						{
							if (!TryGetString(traitNode, out var prefix, "PanicSequencePrefix", "panic-"))
								continue;

							spriteBodyPrefixes.Add(prefix);
							break;
						}

						case "TakeCover":
						{
							if (!TryGetString(traitNode, out var prefix, "ProneSequencePrefix", "prone-"))
								continue;

							spriteBodyPrefixes.Add(prefix);
							break;
						}
					}
				}

				foreach (var prefix in spriteBodyPrefixes)
					foreach (var seq in handledInfantryBodies)
						AddRenderSequences(prefix + seq.Sequence, seq.Palette, seq.ShadowPalette, seq.IsPlayerPalette);

				// Second pass
				foreach (var traitNode in actorNode.Value.Nodes)
				{
					if (traitNode.IsRemoval())
						continue;

					switch (traitNode.GetKey())
					{
						case "FootprintPlaceBuildingPreview":
						case "ActorPreviewPlaceBuildingPreview":
						{
							HandleFootprintPlaceBuildingPreview(traitNode, modData);
							break;
						}

						case "SequencePlaceBuildingPreview":
						{
							var palette = HandleFootprintPlaceBuildingPreview(traitNode, modData);
							if (palette == null)
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence", "idle"))
								AddRenderSequences(sequence, palette);
							break;
						}

						case "D2kActorPreviewPlaceBuildingPreview":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "overlay"))
								continue;

							if (TryGetString(traitNode, out var tileValidName, "TileValidName", "build-valid"))
								AddSequenceDefinitions(image, tileValidName, palette);

							if (TryGetString(traitNode, out var tileInvalidName, "TileInvalidName", "build-invalid"))
								AddSequenceDefinitions(image, tileInvalidName, palette);

							if (TryGetString(traitNode, out var tileUnsafeName, "TileUnsafeName", "build-unsafe"))
								AddSequenceDefinitions(image, tileUnsafeName, palette);
							break;
						}

						case "OrderEffects":
						{
							if (!TryGetString(traitNode, out var palette, "TerrainFlashPalette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "TerrainFlashImage"))
								continue;

							if (!TryGetString(traitNode, out var sequence, "TerrainFlashSequence"))
								continue;

							AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "GpsDot":
						{
							if (!TryGetString(traitNode, out var palette, "IndicatorPalettePrefix", "player"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "gpsdot"))
								continue;

							if (!TryGetString(traitNode, out var sequence, "String", "Infantry"))
								continue;

							AddSequenceDefinitions(image, sequence, palette, isPlayerPalette: true);
							break;
						}

						case "WithIdleOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							TryGetString(traitNode, out var image, "Image");

							if (TryGetString(traitNode, out var startSeq, "StartSequence"))
								AddRenderSequences(startSeq, palette, isPlayerPalette: isPlayerPalette, image: image);

							if (TryGetString(traitNode, out var seq, "Sequence", "idle-overlay"))
								AddRenderSequences(seq, palette, isPlayerPalette: isPlayerPalette, image: image);

							break;
						}

						case "WithProductionDoorOverlay":
						{
							if (TryGetString(traitNode, out var sequence, "Sequence", "build-door"))
								AddRenderSequences(sequence, null);
							break;
						}

						case "WithParachute":
						{
							if (!TryGetString(traitNode, out var image, "Image"))
								continue;

							var hasShadowImage = TryGetString(traitNode, out var shadowImage, "ShadowImage");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", true);
							TryGetString(traitNode, out var palette, "Palette", "player");

							if (TryGetString(traitNode, out var openingSequence, "OpeningSequence"))
							{
								AddRenderSequences(openingSequence, palette, isPlayerPalette: isPlayerPalette, image: image);
								if (hasShadowImage)
									AddRenderSequences(openingSequence, null, palette, image: shadowImage);
							}

							if (TryGetString(traitNode, out var sequence, "Sequence"))
							{
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette, image: image);
								if (hasShadowImage)
									AddRenderSequences(sequence, null, palette, image: shadowImage);
							}

							if (TryGetString(traitNode, out var closingSequence, "ClosingSequence"))
							{
								AddRenderSequences(closingSequence, palette, isPlayerPalette: isPlayerPalette, image: image);
								if (hasShadowImage)
									AddRenderSequences(closingSequence, null, palette, image: shadowImage);
							}

							break;
						}

						case "WithSpriteBarrel":
						{
							if (TryGetString(traitNode, out var sequence, "Sequence", "barrel"))
								AddRenderSequences(sequence, null);
							break;
						}

						case "WithSpriteTurret":
						{
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							TryGetString(traitNode, out var palette, "Palette");

							if (TryGetString(traitNode, out var sequence, "Sequence", "turret"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithSwitchableOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							TryGetString(traitNode, out var image, "Image");

							if (TryGetString(traitNode, out var switchingSequence, "SwitchingSequence"))
								AddRenderSequences(switchingSequence, palette, isPlayerPalette: isPlayerPalette, image: image);

							if (TryGetString(traitNode, out var enabledSequence, "EnabledSequence"))
								AddRenderSequences(enabledSequence, palette, isPlayerPalette: isPlayerPalette, image: image);

							if (TryGetString(traitNode, out var disabledSequence, "DisabledSequence"))
								AddRenderSequences(disabledSequence, palette, isPlayerPalette: isPlayerPalette, image: image);

							break;
						}

						case "WithSupportPowerActivationOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithAttackOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							if (TryGetString(traitNode, out var sequence, "Sequence", "attack"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithBuildingPlacedOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							if (TryGetString(traitNode, out var sequence, "Sequence", "crane-overlay"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithChargeOverlay":
						{
							if (!TryGetString(traitNode, out var sequence, "Sequence", "active"))
								continue;

							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);

							break;
						}

						case "WithResourceLevelOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "resources"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithRepairOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);

							if (TryGetString(traitNode, out var startSequence, "StartSequence"))
								AddRenderSequences(startSequence, palette, isPlayerPalette: isPlayerPalette);

							if (TryGetString(traitNode, out var endSequence, "EndSequence"))
								AddRenderSequences(endSequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithProductionOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "production-overlay"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithMakeOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithHarvestOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette", "effect");

							if (TryGetString(traitNode, out var sequence, "Sequence", "harvest"))
								AddRenderSequences(sequence, palette);
							break;
						}

						case "WithDockingOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "unload-overlay"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithDockedOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "docking-overlay"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithDamageOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (!TryGetString(traitNode, out var image, "Image", "smoke_m"))
								continue;

							if (TryGetString(traitNode, out var idleSequence, "IdleSequence", "idle"))
								AddRenderSequences(idleSequence, palette, isPlayerPalette: isPlayerPalette, image: image);

							if (TryGetString(traitNode, out var loopSequence, "LoopSequence", "loop"))
								AddRenderSequences(loopSequence, palette, isPlayerPalette: isPlayerPalette, image: image);

							if (TryGetString(traitNode, out var endSequence, "EndSequence", "end"))
								AddRenderSequences(endSequence, palette, isPlayerPalette: isPlayerPalette, image: image);
							break;
						}

						case "WithBuildingBib":
						{
							TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName);

							if (TryGetString(traitNode, out var sequence, "Sequence", "bib"))
								AddRenderSequences(sequence, palette);
							break;
						}

						case "WithDeliveryOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithCrumbleOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "crumble-overlay"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithTeslaChargeOverlay":
						{
							TryGetString(traitNode, out var palette, "Palette");
							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, palette, isPlayerPalette: isPlayerPalette);
							break;
						}

						case "WithDeathAnimation":
						{
							if (TryGetString(traitNode, out var deathPalette, "DeathSequencePalette", "player"))
							{
								var isDeathPlayerPalette = ParseBool(traitNode, "DeathPaletteIsPlayerPalette", true);
								var useDeathTypeSuffix = ParseBool(traitNode, "UseDeathTypeSuffix", true);

								if (TryGetString(traitNode, out var deathSequence, "DeathSequence", "die"))
								{
									var deathTypes = ParseDictionary<string[]>(traitNode, "DeathTypes");
									if (useDeathTypeSuffix && deathTypes.Count > 0)
									{
										foreach (var tulple in deathTypes)
											foreach (var deathType in tulple.Value)
												foreach (var actorImage in handledRenderSprites.Keys)
													AddSequenceDefinitions(actorImage, deathSequence + deathType, deathPalette, isPlayerPalette: isDeathPlayerPalette);
									}
									else
										foreach (var actorImage in handledRenderSprites.Keys)
											AddSequenceDefinitions(actorImage, deathSequence, deathPalette, isPlayerPalette: isDeathPlayerPalette);

									if (TryGetString(traitNode, out var fallbackSequence, "FallbackSequence"))
										foreach (var actorImage in handledRenderSprites.Keys)
											AddSequenceDefinitions(actorImage, fallbackSequence, deathPalette, isPlayerPalette: isDeathPlayerPalette);
								}
							}

							if (TryGetString(traitNode, out var crushedPalette, "CrushedSequencePalette", "effect"))
							{
								var isCrushedPlayerPalette = ParseBool(traitNode, "CrushedPaletteIsPlayerPalette", false);
								if (TryGetString(traitNode, out var crushedSequence, "CrushedSequence", "effect"))
									foreach (var actorImage in handledRenderSprites.Keys)
										AddSequenceDefinitions(actorImage, crushedSequence, crushedPalette, isPlayerPalette: isCrushedPlayerPalette);
							}

							break;
						}

						case "WithResourceAnimation":
						{
							if (!TryGetString(traitNode, out var palette, "Palette"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image"))
								continue;

							var sequences = ParseStringArray(traitNode, "Sequences", ["idle"]);
							foreach (var sequence in sequences)
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "GrantConditionOnSubterraneanLayer":
						{
							if (!TryGetString(traitNode, out var palette, "SubterraneanTransitionPalette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "SubterraneanTransitionImage"))
								continue;

							if (TryGetString(traitNode, out var sequence, "SubterraneanTransitionSequence"))
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "Parachutable":
						{
							if (!TryGetString(traitNode, out var image, "Image", "explosion"))
								continue;

							if (TryGetString(traitNode, out var palette, "GroundCorpsePalette", "effect")
								&& TryGetString(traitNode, out var groundCorpseSequence, "GroundCorpseSequence"))
								AddSequenceDefinitions(image, groundCorpseSequence, palette);

							if (TryGetString(traitNode, out var waterPalette, "WaterCorpsePalette", "effect")
								&& TryGetString(traitNode, out var waterCorpseSequence, "WaterCorpseSequence"))
								AddSequenceDefinitions(image, waterCorpseSequence, waterPalette);
							break;
						}

						case "WithAircraftLandingEffect":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image"))
								continue;

							var sequences = ParseStringArray(traitNode, "Sequences", ["idle"]);
							foreach (var sequence in sequences)
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "SpawnActorPower":
						{
							if (!TryGetString(traitNode, out var effectPalette, "EffectPalette"))
								continue;

							var isEffectPlayerPalette = ParseBool(traitNode, "EffectPaletteIsPlayerPalette", false);
							if (!TryGetString(traitNode, out var effectImage, "EffectImage"))
								continue;

							if (TryGetString(traitNode, out var effectSequence, "EffectSequence"))
								AddSequenceDefinitions(effectImage, effectSequence, effectPalette, isPlayerPalette: isEffectPlayerPalette);
							break;
						}

						case "SmudgeLayer":
						{
							if (TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName))
								if (TryGetString(traitNode, out var image, "Sequence", "scorch"))
									queuedSequenceDefinitions[image] = palette;

							if (TryGetString(traitNode, out var smokePalette, "SmokePalette", "effect"))
							{
								if (!TryGetString(traitNode, out var image, "SmokeImage"))
									continue;

								var sequences = ParseStringArray(traitNode, "SmokeSequences");
								foreach (var sequence in sequences)
									AddSequenceDefinitions(image, sequence, smokePalette);
							}

							break;
						}

						case "CrateAction":
						case "DuplicateUnitCrateAction":
						case "ExplodeCrateAction":
						case "GiveCashCrateAction":
						case "GiveUnitCrateAction":
						case "GrantExternalConditionCrateAction":
						case "HealActorsCrateAction":
						case "HideMapCrateAction":
						case "LevelUpCrateAction":
						case "RevealMapCrateAction":
						case "SupportPowerCrateAction":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "crate-effects"))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence"))
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "GainsExperience":
						{
							if (!TryGetString(traitNode, out var palette, "LevelUpPalette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "LevelUpImage"))
								continue;

							if (TryGetString(traitNode, out var sequence, "LevelUpSequence", "levelup"))
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "LeavesTrails":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image"))
								continue;

							var sequences = ParseStringArray(traitNode, "Sequences", ["idle"]);
							foreach (var sequence in sequences)
								AddSequenceDefinitions(image, sequence, palette);
							break;
						}

						case "Cloak":
						{
							if (TryGetString(traitNode, out var effectPalette, "EffectPalette", "effect"))
							{
								if (!TryGetString(traitNode, out var effectImage, "EffectImage"))
									continue;

								var isEffectPlayerPalette = ParseBool(traitNode, "EffectPaletteIsPlayerPalette", false);
								if (TryGetString(traitNode, out var cloakEffect, "CloakEffectSequence"))
									AddSequenceDefinitions(effectImage, cloakEffect, effectPalette, isPlayerPalette: isEffectPlayerPalette);

								if (TryGetString(traitNode, out var uncloakEffect, "UncloakEffectSequence"))
									AddSequenceDefinitions(effectImage, uncloakEffect, effectPalette, isPlayerPalette: isEffectPlayerPalette);
							}

							break;
						}

						case "GpsPower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var doorPalette, "DoorPalette", "player"))
							{
								var isDoorPlayerPalette = ParseBool(traitNode, "DoorPaletteIsPlayerPalette", true);
								if (TryGetString(traitNode, out var doorImage, "DoorImage", "atek")
									&& TryGetString(traitNode, out var doorSequence, "DoorSequence", "active"))
									AddSequenceDefinitions(doorImage, doorSequence, doorPalette, isPlayerPalette: isDoorPlayerPalette);
							}

							if (TryGetString(traitNode, out var satellitePalette, "SatellitePalette", "player"))
							{
								var isSatellitePlayerPalette = ParseBool(traitNode, "SatellitePaletteIsPlayerPalette", true);
								if (TryGetString(traitNode, out var satelliteImage, "SatelliteImage", "sputnik")
									&& TryGetString(traitNode, out var satelliteSequence, "SatelliteSequence", "idle"))
									AddSequenceDefinitions(satelliteImage, satelliteSequence, satellitePalette, isPlayerPalette: isSatellitePlayerPalette);
							}

							break;
						}

						case "Buildable":
						{
							if (TryGetString(traitNode, out var iconPalette, "IconPalette", "chrome"))
							{
								var isIconPlayerPalette = ParseBool(traitNode, "IconPaletteIsPlayerPalette", false);
								if (TryGetString(traitNode, out var icon, "Icon", "icon"))
									foreach (var actorImage in handledRenderSprites.Keys)
										AddSequenceDefinitions(actorImage, icon, iconPalette, isPlayerPalette: isIconPlayerPalette);
							}

							break;
						}

						case "RallyPoint":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "player"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "rallypoint"))
								continue;

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", true);
							if (TryGetString(traitNode, out var flagSequence, "FlagSequence", "flag"))
								AddSequenceDefinitions(image, flagSequence, palette, isPlayerPalette: isPlayerPalette);

							if (TryGetString(traitNode, out var circlesSequence, "CirclesSequence", "circles"))
								AddSequenceDefinitions(image, circlesSequence, palette, isPlayerPalette: isPlayerPalette);

							break;
						}

						case "PlaceBeacon":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "player"))
								continue;

							if (!TryGetString(traitNode, out var image, "BeaconImage", "beacon"))
								continue;

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", true);
							if (TryGetString(traitNode, out var beaconSequence, "BeaconSequence"))
								AddSequenceDefinitions(image, beaconSequence, palette, isPlayerPalette: isPlayerPalette);

							if (TryGetString(traitNode, out var arrowSequence, "ArrowSequence", "arrow"))
								AddSequenceDefinitions(image, arrowSequence, palette, isPlayerPalette: isPlayerPalette);

							if (TryGetString(traitNode, out var circleSequence, "CircleSequence", "circles"))
								AddSequenceDefinitions(image, circleSequence, palette, isPlayerPalette: isPlayerPalette);

							break;
						}

						case "SupportPower":
						case "AttackOrderPower":
						case "GrantPrerequisiteChargeDrainPower":
						case "ProduceActorPower":
						{
							HandleSupportPower(traitNode);
							break;
						}

						case "ParatroopersPower":
						case "AirstrikePower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var palette, "DirectionArrowPalette", "chrome")
								&& TryGetString(traitNode, out var image, "DirectionArrowAnimation"))
							{
								var arrowSequences = ParseStringArray(traitNode,
									"Arrows", ["arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr"]);

								foreach (var sequence in arrowSequences)
									AddSequenceDefinitions(image, sequence, palette);
							}

							break;
						}

						case "ChronoshiftPower":
						{
							HandleSupportPower(traitNode);

							if (!TryGetString(traitNode, out var image, "FootprintImage", "overlay"))
								continue;

							var hasSource = TryGetString(traitNode, out var sourceSeq, "SourceFootprintSequence", "target-select");

							if (hasSource && TryGetString(traitNode, out var palette, "TargetOverlayPalette", TerrainPaletteInternalName))
							{
								AddSequenceDefinitions(image, sourceSeq, palette);
							}

							if (TryGetString(traitNode, out var iconPalette, "IconPalette", "chrome"))
							{
								if (TryGetString(traitNode, out var validSeq, "ValidFootprintSequence", "target-valid"))
								{
									AddSequenceDefinitions(image, validSeq, iconPalette);

									foreach (var tileset in modData.DefaultTerrainInfo.Values)
										AddSequenceDefinitions(image, validSeq + '-' + tileset.Id.ToLowerInvariant(), iconPalette);
								}

								if (hasSource)
									AddSequenceDefinitions(image, sourceSeq, iconPalette);

								if (TryGetString(traitNode, out var invalidSeq, "InvalidFootprintSequence", "target-invalid"))
									AddSequenceDefinitions(image, invalidSeq, iconPalette);
							}

							break;
						}

						case "DropPodsPower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var entryEffectPalette, "EntryEffectPalette", "effect")
								&& TryGetString(traitNode, out var entryEffect, "EntryEffect", "podring")
								&& TryGetString(traitNode, out var entryEffectSequence, "EntryEffectSequence", "idle"))
							{
								AddSequenceDefinitions(entryEffect, entryEffectSequence, entryEffectPalette);
							}

							break;
						}

						case "IonCannonPower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var effectPalette, "EffectPalette", "effect")
								&& TryGetString(traitNode, out var effect, "Effect", "ionsfx")
								&& TryGetString(traitNode, out var effectSequence, "EffectSequence", "idle"))
							{
								AddSequenceDefinitions(effect, effectSequence, effectPalette);
							}

							break;
						}

						case "GrantExternalConditionPower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var footprintSequence, "FootprintSequence", "target-select")
								&& TryGetString(traitNode, out var footprintImage, "FootprintImage", "overlay"))
							{
								AddSequenceDefinitions(footprintImage, footprintSequence, TerrainPaletteInternalName);
							}

							if (spriteBodyPalettes.Count > 0 && TryGetString(traitNode, out var sequence, "Sequence", "active"))
							{
								var (palette, isPlayerPalette) = spriteBodyPalettes.First().Value;
								AddRenderSequences(sequence, palette, null, isPlayerPalette);
							}

							break;
						}

						case "NukePower":
						{
							HandleSupportPower(traitNode);

							if (TryGetString(traitNode, out var missilePalette, "MissilePalette", "effect"))
							{
								var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
								if (TryGetString(traitNode, out var missileImage, "MissileImage"))
								{
									if (TryGetString(traitNode, out var missileUp, "MissileUp", "up"))
										AddSequenceDefinitions(missileImage, missileUp, missilePalette, isPlayerPalette: isPlayerPalette);

									if (TryGetString(traitNode, out var missileDown, "MissileDown", "down"))
										AddSequenceDefinitions(missileImage, missileDown, missilePalette, isPlayerPalette: isPlayerPalette);
								}
							}

							if (TryGetString(traitNode, out var trailPalette, "TrailPalette", "effect"))
							{
								var isTrailPlayerPalette = ParseBool(traitNode, "TrailUsePlayerPalette", false);
								if (TryGetString(traitNode, out var trailImage, "TrailImage"))
								{
									var trailSequences = ParseStringArray(traitNode, "TrailSequences", ["particles"]);
									foreach (var sequence in trailSequences)
										AddSequenceDefinitions(trailImage, sequence, trailPalette, isPlayerPalette: isTrailPlayerPalette);
								}
							}

							break;
						}

						case "WithMuzzleOverlay":
						{
							var armaments = globalArmaments[actorNode.Key];
							foreach (var armament in armaments)
							{
								if (TryGetString(armament, out var palette, "MuzzlePalette", "effect")
									 && TryGetString(armament, out var sequence, "MuzzleSequence"))
								{
									foreach (var actorImage in handledRenderSprites.Keys)
										AddSequenceDefinitions(actorImage, sequence, palette);
								}
							}

							break;
						}

						case "AttackGarrisoned":
						{
							if (!TryGetString(traitNode, out var palette, "MuzzlePalette", "effect"))
								continue;

							foreach (var tuple in globalArmaments)
								foreach (var armament in tuple.Value)
									if (TryGetString(armament, out var sequence, "MuzzleSequence"))
										AddSequenceDefinitions(tuple.Key, sequence, palette);

							break;
						}

						case "FloatingSpriteEmitter":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "effect"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "smoke"))
								continue;

							var sequences = ParseStringArray(traitNode, "Sequence", ["particles"]);
							foreach (var sequence in sequences)
								AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "TSVeinsRenderer":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "resources"))
								continue;

							if (!TryGetString(traitNode, out var sequence, "Sequence", "veins"))
								continue;

							AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "Minelayer":
						{
							if (TryGetString(traitNode, out var validSeq, "TileValidName", "build-valid"))
							{
								AddSequenceDefinitions("overlay", validSeq, TerrainPaletteInternalName);
								if (drawLineToTargetPalette != null)
									AddSequenceDefinitions("overlay", validSeq, drawLineToTargetPalette);

								foreach (var tileset in modData.DefaultTerrainInfo.Values)
								{
									AddSequenceDefinitions("overlay", validSeq + '-' + tileset.Id.ToLowerInvariant(), TerrainPaletteInternalName);
									if (drawLineToTargetPalette != null)
										AddSequenceDefinitions("overlay", validSeq + '-' + tileset.Id.ToLowerInvariant(), drawLineToTargetPalette);
								}
							}

							if (TryGetString(traitNode, out var invalidSeq, "TileInvalidName", "build-invalid"))
							{
								AddSequenceDefinitions("overlay", invalidSeq, TerrainPaletteInternalName);

								foreach (var tileset in modData.DefaultTerrainInfo.Values)
									AddSequenceDefinitions("overlay", invalidSeq + '-' + tileset.Id.ToLowerInvariant(), TerrainPaletteInternalName);
							}

							if (TryGetString(traitNode, out var unknownSeq, "TileUnknownName", "build-unknown"))
							{
								AddSequenceDefinitions("overlay", unknownSeq, TerrainPaletteInternalName);

								foreach (var tileset in modData.DefaultTerrainInfo.Values)
									AddSequenceDefinitions("overlay", unknownSeq + '-' + tileset.Id.ToLowerInvariant(), TerrainPaletteInternalName);
							}

							break;
						}

						case "ResourceRenderer":
						case "D2kResourceRenderer":
						{
							var resources = traitNode.LastChildMatching("ResourceTypes");
							if (resources == null)
								continue;

							foreach (var res in resources.Value.Nodes)
							{
								if (!TryGetString(res, out var palette, "Palette", TerrainPaletteInternalName))
									continue;

								if (!TryGetString(res, out var image, "Image", "resources"))
									continue;

								var sequences = ParseStringArray(res, "Sequences");
								foreach (var sequence in sequences)
									AddSequenceDefinitions(image, sequence, palette);
							}

							break;
						}

						case "TSTiberiumRenderer":
						{
							var resources = traitNode.LastChildMatching("ResourceTypes");
							if (resources == null)
								continue;

							var ramp1Dict = ParseDictionary<string[]>(traitNode, "Ramp1Sequences");
							var ramp2Dict = ParseDictionary<string[]>(traitNode, "Ramp2Sequences");
							var ramp3Dict = ParseDictionary<string[]>(traitNode, "Ramp3Sequences");
							var ramp4Dict = ParseDictionary<string[]>(traitNode, "Ramp4Sequences");
							foreach (var res in resources.Value.Nodes)
							{
								if (!TryGetString(res, out var palette, "Palette", TerrainPaletteInternalName))
									continue;

								if (!TryGetString(res, out var image, "Image", "resources"))
									continue;

								var sequences = ParseStringArray(res, "Sequences");
								foreach (var sequence in sequences)
									AddSequenceDefinitions(image, sequence, palette);

								foreach (var ramp in ramp1Dict)
									foreach (var sequence in ramp.Value)
										AddSequenceDefinitions(image, sequence, palette);

								foreach (var ramp in ramp2Dict)
									foreach (var sequence in ramp.Value)
										AddSequenceDefinitions(image, sequence, palette);

								foreach (var ramp in ramp3Dict)
									foreach (var sequence in ramp.Value)
										AddSequenceDefinitions(image, sequence, palette);

								foreach (var ramp in ramp4Dict)
									foreach (var sequence in ramp.Value)
										AddSequenceDefinitions(image, sequence, palette);
							}

							break;
						}

						case "BuildableTerrainOverlay":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "overlay"))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence", "build-invalid"))
								AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "WithDecoration":
						case "WithBuildingRepairDecoration":
						case "InfiltrateForDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);

							if (TryGetString(traitNode, out var image, "Image")
								&& TryGetString(traitNode, out var sequence, "Sequence"))
								AddSequenceDefinitions(image, sequence, palette, isPlayerPalette: isPlayerPalette);

							break;
						}

						case "ShroudRenderer":
						{
							if (!TryGetString(traitNode, out var image, "Sequence", "shroud"))
								continue;

							if (TryGetString(traitNode, out var palette, "ShroudPalette", "shroud"))
							{
								var shroudVariants = ParseStringArray(traitNode, "ShroudVariants", ["shroud"]);
								foreach (var variant in shroudVariants)
									AddSequenceDefinitions(image, variant, palette);

								if (TryGetString(traitNode, out var fullShroud, "OverrideFullShroud"))
									AddSequenceDefinitions(image, fullShroud, palette);
							}

							if (TryGetString(traitNode, out var fogPalette, "FogPalette", "fog"))
							{
								var fogVariants = ParseStringArray(traitNode, "FogVariants", ["fog"]);
								foreach (var variant in fogVariants)
									AddSequenceDefinitions(image, variant, fogPalette);

								if (TryGetString(traitNode, out var fullFog, "OverrideFullFog"))
									AddSequenceDefinitions(image, fullFog, fogPalette);
							}

							break;
						}

						case "ProductionIconOverlayManager":
						{
							if (TryGetString(traitNode, out var palette, "Palette", "chrome")
								&& TryGetString(traitNode, out var image, "Image")
								&& TryGetString(traitNode, out var sequence, "Sequence"))
							{
								AddSequenceDefinitions(image, sequence, palette);
							}

							break;
						}

						case "WithAmmoPipsDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "pips"))
								continue;

							if (TryGetString(traitNode, out var emptySequence, "EmptySequence", "pip-empty"))
								AddSequenceDefinitions(image, emptySequence, palette);

							if (TryGetString(traitNode, out var fullSequence, "FullSequence", "pip-green"))
								AddSequenceDefinitions(image, fullSequence, palette);

							break;
						}

						case "WithCargoPipsDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "pips"))
								continue;

							if (TryGetString(traitNode, out var emptySequence, "EmptySequence", "pip-empty"))
								AddSequenceDefinitions(image, emptySequence, palette);

							if (TryGetString(traitNode, out var fullSequence, "FullSequence", "pip-green"))
								AddSequenceDefinitions(image, fullSequence, palette);

							var customPips = ParseDictionary<string>(traitNode, "CustomPipSequences");
							foreach (var sequence in customPips.Values)
								AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "WithResourceStoragePipsDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "pips"))
								continue;

							if (TryGetString(traitNode, out var emptySequence, "EmptySequence", "pip-empty"))
								AddSequenceDefinitions(image, emptySequence, palette);

							if (TryGetString(traitNode, out var fullSequence, "FullSequence", "pip-green"))
								AddSequenceDefinitions(image, fullSequence, palette);

							break;
						}

						case "WithSpriteControlGroupDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "pips"))
								continue;

							if (TryGetString(traitNode, out var sequence, "GroupSequence", "groups"))
								AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "WithStoresResourcesPipsDecoration":
						{
							if (!TryGetString(traitNode, out var palette, "Palette", "chrome"))
								continue;

							if (!TryGetString(traitNode, out var image, "Image", "pips"))
								continue;

							if (TryGetString(traitNode, out var emptySequence, "EmptySequence", "pip-empty"))
								AddSequenceDefinitions(image, emptySequence, palette);

							if (TryGetString(traitNode, out var fullSequence, "FullSequence", "pip-green"))
								AddSequenceDefinitions(image, fullSequence, palette);

							var sequences = ParseDictionary<string>(traitNode, "ResourceSequences");
							foreach (var sequence in sequences.Values)
								AddSequenceDefinitions(image, sequence, palette);

							break;
						}

						case "WithCrateBody":
						{
							foreach (var image in (string[])[null, .. ParseStringArray(traitNode, "XmasImages")])
							{
								if (TryGetString(traitNode, out var sequence, "IdleSequence", "idle"))
									AddRenderSequences(sequence, null, image: image);

								if (TryGetString(traitNode, out var waterSequence, "WaterSequence"))
									AddRenderSequences(waterSequence, null, image: image);

								if (TryGetString(traitNode, out var landSequence, "LandSequence"))
									AddRenderSequences(landSequence, null, image: image);
							}

							break;
						}

						case "WithGunboatBody":
						{
							if (TryGetString(traitNode, out var leftSequence, "LeftSequence", "left"))
								AddRenderSequences(leftSequence, null);

							if (TryGetString(traitNode, out var rightSequence, "RightSequence", "right"))
								AddRenderSequences(rightSequence, null);

							if (TryGetString(traitNode, out var wakeLeftSequence, "WakeLeftSequence", "wake-left"))
								AddRenderSequences(wakeLeftSequence, null);

							if (TryGetString(traitNode, out var wakeRightSequence, "WakeRightSequence", "wake-right"))
								AddRenderSequences(wakeRightSequence, null);

							break;
						}

						case "ThrowsParticle":
						{
							if (TryGetString(traitNode, out var sequence, "Anim"))
								AddRenderSequences(sequence, null);

							break;
						}

						case "SpiceBloom":
						{
							var growthSequences = ParseStringArray(traitNode, "GrowthSequences", ["grow1", "grow2", "grow3"]);
							foreach (var sequence in growthSequences)
								AddRenderSequences(sequence, null);

							if (TryGetString(traitNode, out var sequence2, "SpurtSequence", "spurt"))
								AddRenderSequences(sequence2, null);

							break;
						}

						case "WithDockingAnimation":
						{
							if (spriteBodyPalettes.Count == 0)
								break;

							var (palette, isPlayerPalette) = spriteBodyPalettes.First().Value;
							if (TryGetString(traitNode, out var sequence, "DockSequence", "dock"))
								AddRenderSequences(sequence, palette, null, isPlayerPalette);

							if (TryGetString(traitNode, out var loopSequence, "DockLoopSequence", "dock-loop"))
								AddRenderSequences(loopSequence, palette, null, isPlayerPalette);

							break;
						}

						case "WithHarvestAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "HarvestSequence", "harvest"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "AttackPopupTurreted":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var openingSequence, "OpeningSequence", "opening"))
								AddRenderSequences(openingSequence, vals.Palette, null, vals.IsPlayerPalette);

							if (TryGetString(traitNode, out var closingSequence, "ClosingSequence", "closing"))
								AddRenderSequences(closingSequence, vals.Palette, null, vals.IsPlayerPalette);

							if (TryGetString(traitNode, out var closedIdleSequence, "ClosedIdleSequence", "closed-idle"))
								AddRenderSequences(closedIdleSequence, vals.Palette, null, vals.IsPlayerPalette);

							break;
						}

						case "Chronoshiftable":
						case "PortableChrono":
						case "ConyardChronoReturn":
						{
							if (spriteBodyPalettes.Count == 0)
								break;

							var (palette, isPlayerPalette) = spriteBodyPalettes.First().Value;
							AddRenderSequences("active", palette, null, isPlayerPalette);
							break;
						}

						case "WithLandingCraftAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var openSequence, "OpenSequence", "open"))
								AddRenderSequences(openSequence, vals.Palette, null, vals.IsPlayerPalette);

							if (TryGetString(traitNode, out var closeSequence, "CloseSequence", "close"))
								AddRenderSequences(closeSequence, vals.Palette, null, vals.IsPlayerPalette);

							if (TryGetString(traitNode, out var unloadSequence, "UnloadSequence", "unload"))
								AddRenderSequences(unloadSequence, vals.Palette, null, vals.IsPlayerPalette);

							break;
						}

						case "WithTeslaChargeAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "ChargeSequence", "active"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithAcceptDeliveredCashAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithDeliveryAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "ActiveSequence", "active"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithBuildingPlacedAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence", "build"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithAttackAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithAimAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithMakeAnimation":
						{
							if (!TryGetString(traitNode, out var sequence, "Sequence", "make"))
								continue;

							var bodies = ParseStringArray(traitNode, "Bodies", ["body"]);
							foreach (var body in bodies)
							{
								if (!spriteBodyPalettes.TryGetValue(body, out var vals))
									continue;

								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							}

							break;
						}

						case "WithResupplyAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							if (TryGetString(traitNode, out var sequence, "Sequence", "active"))
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithIdleAnimation":
						{
							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							var sequences = ParseStringArray(traitNode, "Sequences", ["active"]);
							foreach (var sequence in sequences)
								AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}

						case "WithSupportPowerActivationAnimation":
						{
							if (!TryGetString(traitNode, out var sequence, "Sequence", "active"))
								continue;

							if (!TryGetString(traitNode, out var body, "Body", "body"))
								continue;

							if (!spriteBodyPalettes.TryGetValue(body, out var vals))
								continue;

							AddRenderSequences(sequence, vals.Palette, null, vals.IsPlayerPalette);
							break;
						}
					}
				}
			}

			yield break;
		}

		string HandleFootprintPlaceBuildingPreview(MiniYamlNodeBuilder traitNode, ModData modData)
		{
			if (!TryGetString(traitNode, out var palette, "Palette", TerrainPaletteInternalName))
				return null;

			AddSequenceDefinitions("overlay", "build-valid", palette);
			AddSequenceDefinitions("overlay", "build-invalid", palette);

			foreach (var tileset in modData.DefaultTerrainInfo.Values)
				AddSequenceDefinitions("overlay", $"build-invalid-{tileset.Id.ToLowerInvariant()}", palette);

			return palette;
		}

		static (HashSet<SequenceDefinitions> Sequences, string Name, string Palette, bool IsPlayerPalette) HandleWithSpriteBody(MiniYamlNodeBuilder traitNode)
		{
			TryGetString(traitNode, out var palette, "Palette");
#pragma warning disable CA1507 // Use nameof instead of string literal
			TryGetString(traitNode, out var name, "Name", "body");
#pragma warning restore CA1507 // Use nameof instead of string literal

			var sequences = new HashSet<SequenceDefinitions>();
			var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
			if (TryGetString(traitNode, out var startSequence, "StartSequence"))
				sequences.Add(new SequenceDefinitions
				{
					Sequence = startSequence,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});

			if (TryGetString(traitNode, out var idleSequence, "Sequence", "idle"))
				sequences.Add(new SequenceDefinitions
				{
					Sequence = idleSequence,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});

			return (sequences, name, palette, isPlayerPalette);
		}

		static (HashSet<SequenceDefinitions> Sequences, string Palette, bool IsPlayerPalette) HandleWithInfantryBody(MiniYamlNodeBuilder traitNode)
		{
			// We need the rest of information even if palette is not set.
			TryGetString(traitNode, out var palette, "Palette");

			var isPlayerPalette = ParseBool(traitNode, "IsPlayerPalette", false);
			var sequences = new HashSet<SequenceDefinitions>();

			if (TryGetString(traitNode, out var moveSeq, "MoveSequence", "run"))
			{
				sequences.Add(new SequenceDefinitions
				{
					Sequence = moveSeq,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});
			}

			var idleSeqs = ParseStringArray(traitNode, "IdleSequences");
			foreach (var seq in idleSeqs)
			{
				sequences.Add(new SequenceDefinitions
				{
					Sequence = seq,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});
			}

			var standSeqs = ParseStringArray(traitNode, "StandSequences", ["stand"]);
			foreach (var seq in standSeqs)
			{
				sequences.Add(new SequenceDefinitions
				{
					Sequence = seq,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});
			}

			var attackSeqs = ParseDictionary<string[]>(traitNode, "AttackSequences");
			foreach (var seq in attackSeqs.Values)
			{
				foreach (var s in seq)
				{
					sequences.Add(new SequenceDefinitions
					{
						Sequence = s,
						Palette = palette,
						IsPlayerPalette = isPlayerPalette
					});
				}
			}

			if (TryGetString(traitNode, out var defaultAttackSeq, "DefaultAttackSequence"))
			{
				sequences.Add(new SequenceDefinitions
				{
					Sequence = defaultAttackSeq,
					Palette = palette,
					IsPlayerPalette = isPlayerPalette
				});
			}

			return (sequences, palette, isPlayerPalette);
		}

		void HandleSupportPower(MiniYamlNodeBuilder traitNode)
		{
			if (TryGetString(traitNode, out var iconPalette, "IconPalette", "chrome")
				&& TryGetString(traitNode, out var iconImage, "IconImage", "icon")
				&& TryGetString(traitNode, out var icon, "Icon"))
			{
				AddSequenceDefinitions(iconImage, icon, iconPalette);
			}

			if (!TryGetString(traitNode, out var beaconImage, "BeaconImage", "beacon"))
				return;

			if (TryGetString(traitNode, out var beaconPalette, "BeaconPalette", "player"))
			{
				var isPlayerPalette = ParseBool(traitNode, "BeaconPaletteIsPlayerPalette", true);
				if (TryGetString(traitNode, out var beaconSequence, "BeaconSequence"))
					AddSequenceDefinitions(beaconImage, beaconSequence, beaconPalette, isPlayerPalette: isPlayerPalette);

				if (TryGetString(traitNode, out var circleClockSequence, "CircleSequence"))
					AddSequenceDefinitions(beaconImage, circleClockSequence, beaconPalette, isPlayerPalette: isPlayerPalette);

				if (TryGetString(traitNode, out var arrowSequence, "ArrowSequence", "arrow"))
					AddSequenceDefinitions(beaconImage, arrowSequence, beaconPalette, isPlayerPalette: isPlayerPalette);
			}

			if (TryGetString(traitNode, out var beaconPosterPalette, "BeaconPosterPalette", "chrome"))
			{
				if (TryGetString(traitNode, out var clockSequence, "ClockSequence"))
					AddSequenceDefinitions(beaconImage, clockSequence, beaconPosterPalette);

				if (TryGetString(traitNode, out var beaconPosterSequence, "BeaconPoster"))
					AddSequenceDefinitions(beaconImage, beaconPosterSequence, beaconPosterPalette);
			}
		}

		IEnumerable<string> IBeforeUpdateWeapons.BeforeUpdateWeapons(ModData modData, List<MiniYamlNodeBuilder> resolvedWeapons)
		{
			foreach (var weapon in resolvedWeapons)
			{
				var projectileNode = weapon.LastChildMatching("Projectile");
				if (projectileNode?.Value?.Value != null)
				{
					switch (projectileNode.Value.Value)
					{
						case "TeslaZap":
						{
							if (!TryGetString(projectileNode, out var palette, "Palette", "effect"))
								continue;

							if (!TryGetString(projectileNode, out var image, "Image", "litning"))
								continue;

							if (TryGetString(projectileNode, out var brightSequence, "BrightSequence", "bright"))
								AddSequenceDefinitions(image, brightSequence, palette);

							if (TryGetString(projectileNode, out var dimSequence, "DimSequence", "dim"))
								AddSequenceDefinitions(image, dimSequence, palette);
							break;
						}

						case "Bullet":
						case "Missile":
						case "GravityBomb":
						{
							if (TryGetString(projectileNode, out var image, "Image"))
							{
								if (TryGetString(projectileNode, out var palette, "Palette", "effect"))
								{
									var isPlayerPalette = ParseBool(projectileNode, "IsPlayerPalette", false);
									var sequences = ParseStringArray(projectileNode, "Sequences", ["idle"]);
									foreach (var sequence in sequences)
										AddSequenceDefinitions(image, sequence, palette, isPlayerPalette: isPlayerPalette);

									if (TryGetString(projectileNode, out var openSequence, "OpenSequence"))
										AddSequenceDefinitions(image, openSequence, palette, isPlayerPalette: isPlayerPalette);
								}
							}

							if (TryGetString(projectileNode, out var trailPalette, "TrailPalette", "effect"))
							{
								if (TryGetString(projectileNode, out var trailImage, "TrailImage"))
								{
									var isTrailPlayerPalette = ParseBool(projectileNode, "TrailUsePlayerPalette", false);
									var trailSequences = ParseStringArray(projectileNode, "TrailSequences", ["idle"]);

									foreach (var sequence in trailSequences)
										AddSequenceDefinitions(trailImage, sequence, trailPalette, isPlayerPalette: isTrailPlayerPalette);
								}
							}

							break;
						}

						case "LaserZap":
						{
							if (TryGetString(projectileNode, out var hitAnim, "HitAnim")
								&& TryGetString(projectileNode, out var hitAnimPalette, "HitAnimPalette", "effect")
								&& TryGetString(projectileNode, out var hitAnimSequence, "HitAnimSequence", "idle"))
							{
								AddSequenceDefinitions(hitAnim, hitAnimSequence, hitAnimPalette);
							}

							if (TryGetString(projectileNode, out var launchEffectImage, "LaunchEffectImage")
								&& TryGetString(projectileNode, out var launchEffectPalette, "LaunchEffectPalette", "effect")
								&& TryGetString(projectileNode, out var launchEffectSequence, "LaunchEffectSequence"))
							{
								AddSequenceDefinitions(launchEffectImage, launchEffectSequence, launchEffectPalette);
							}

							break;
						}

						case "Railgun":
						{
							if (!TryGetString(projectileNode, out var palette, "HitAnimPalette", "effect"))
								continue;

							if (TryGetString(projectileNode, out var hitAnim, "HitAnim")
								&& TryGetString(projectileNode, out var hitAnimSequence, "HitAnimSequence", "idle"))
							{
								AddSequenceDefinitions(hitAnim, hitAnimSequence, palette);
							}

							break;
						}
					}
				}

				foreach (var warheadNode in weapon.ChildrenMatching("Warhead"))
				{
					if (warheadNode?.Value?.Value == null)
						continue;

					switch (warheadNode.Value.Value)
					{
						case "CreateEffect":
						{
							if (!TryGetString(warheadNode, out var palette, "ExplosionPalette", "effect"))
								continue;

							if (!TryGetString(warheadNode, out var image, "Image", "explosion"))
								continue;

							var explosions = ParseStringArray(warheadNode, "Explosions", []);
							var usePlayerPalette = ParseBool(warheadNode, "UsePlayerPalette", false);

							foreach (var explosion in explosions)
								AddSequenceDefinitions(image, explosion, palette, isPlayerPalette: usePlayerPalette);

							break;
						}
					}
				}
			}

			yield break;
		}

		IEnumerable<string> IBeforeUpdateSequences.BeforeUpdateSequences(ModData modData, List<MiniYamlNodeBuilder> resolvedSequences)
		{
			foreach (var sequence in resolvedSequences)
			{
				if (queuedSequenceDefinitions.TryGetValue(sequence.Key, out var palette))
				{
					foreach (var seq in sequence.Value.Nodes)
					{
						AddSequenceDefinitions(sequence.Key, seq.Key, palette);
					}
				}
			}

			this.resolvedSequences = resolvedSequences;
			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNodeBuilder weaponNode)
		{
			if (weaponNode?.Value?.Nodes == null)
				yield break;

			var projectileNode = weaponNode.LastChildMatching("Projectile");
			if (projectileNode?.Value?.Value != null && projectileRemovals.TryGetValue(projectileNode.Value.Value, out var fieldsToRemove))
			{
				foreach (var field in fieldsToRemove)
					projectileNode?.RemoveNodes(field);
			}

			foreach (var warheadNode in weaponNode.ChildrenMatching("Warhead"))
			{
				if (warheadNode?.Value?.Value != null && warheadRemovals.TryGetValue(warheadNode.Value.Value, out fieldsToRemove))
				{
					foreach (var field in fieldsToRemove)
						warheadNode?.RemoveNodes(field);
				}
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			if (actorNode?.Value?.Nodes == null)
				yield break;

			foreach (var traitNode in actorNode.Value.Nodes)
			{
				var key = traitNode.GetKey();
				switch (key)
				{
					case "ResourceRenderer":
					case "D2kResourceRenderer":
					case "TSTiberiumRenderer":
						var resources = traitNode.LastChildMatching("ResourceTypes");
						if (resources != null)
							foreach (var res in resources.Value.Nodes)
								res.RemoveNodes("Palette");

						continue;
				}

				if (traitNode.Key == null || !traitRemovals.TryGetValue(key, out var fieldsToRemove))
					continue;

				foreach (var field in fieldsToRemove)
					traitNode.RemoveNodes(field);
			}
		}

		public override IEnumerable<string> UpdateSequenceNode(ModData modData, MiniYamlNodeBuilder sequenceNode)
		{
			if (sequenceNode?.Key == null)
				yield break;

			var fullSequence = resolvedSequences.First(s => s.Key == sequenceNode.Key);

			if (!sequenceDefinitions.TryGetValue(fullSequence.Key.ToLowerInvariant(), out var definitions))
				yield break;

			var addedPalette = false;
			var addedShadowPalette = false;

			foreach (var def in definitions)
			{
				// If the sequence does not exist, skip it.
				if (!fullSequence.ChildrenMatching(def.Sequence).Any())
					continue;

				var seqNode = sequenceNode.LastChildMatching(def.Sequence);
				var wasEmpty = seqNode == null;
				seqNode ??= new MiniYamlNodeBuilder(def.Sequence, string.Empty);

				if (def.Palette != null && !seqNode.ChildrenMatching("Palette").ToArray().Any(p => p.Value.Value == def.Palette))
				{
					var newPaletteNode = new MiniYamlNodeBuilder("Palette", def.Palette);
					seqNode.AddNode(newPaletteNode);
					seqNode.AddNode(new MiniYamlNodeBuilder(FieldSaver.SaveField(def, "IsPlayerPalette")));

					if (addedPalette)
						yield return $"Sequence {sequenceNode.Key}.{def.Sequence} had a duplicated Palette nodes added";

					addedPalette = true;
				}

				if (def.ShadowPalette != null && !seqNode.ChildrenMatching("ShadowPalette").Any(p => p.Value.Value == def.ShadowPalette))
				{
					var newShadowPaletteNode = new MiniYamlNodeBuilder("ShadowPalette", def.ShadowPalette);
					seqNode.AddNode(newShadowPaletteNode);

					if (addedShadowPalette)
						yield return $"Sequence {sequenceNode.Key}.{def.Sequence} had a duplicated ShadowPalette nodes added";

					addedShadowPalette = true;
				}

				if (wasEmpty && (addedPalette || addedShadowPalette))
					sequenceNode.AddNode(seqNode);
			}
		}

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNodeBuilder chromeNode)
		{
			var key = chromeNode.GetKey();
			if (string.IsNullOrEmpty(key))
				yield break;

			switch (key)
			{
				case "SupportPowers":
					if (!TryGetString(chromeNode, out var palette, "ClockPalette", "chrome"))
						yield break;

					if (!TryGetString(chromeNode, out var iconImage, "ClockAnimation", "clock"))
						yield break;

					if (!TryGetString(chromeNode, out var clockSequence, "ClockSequence", "idle"))
						yield break;

					AddSequenceDefinitions(iconImage, clockSequence, palette);
					break;
				case "ProductionPalette":
					if (TryGetString(chromeNode, out var notBuildablePalette, "NotBuildablePalette", "chrome")
						&& TryGetString(chromeNode, out var notBuildableImage, "NotBuildableAnimation", "clock")
						&& TryGetString(chromeNode, out var notBuildableSequence, "NotBuildableSequence", "idle"))
						AddSequenceDefinitions(notBuildableImage, notBuildableSequence, notBuildablePalette);

					if (TryGetString(chromeNode, out var clockPalette, "ClockPalette", "chrome")
						&& TryGetString(chromeNode, out var clockAnimation, "ClockAnimation", "clock")
						&& TryGetString(chromeNode, out clockSequence, "ClockSequence", "idle"))
						AddSequenceDefinitions(clockAnimation, clockSequence, clockPalette);

					break;
			}

			if (widgetRemovals.TryGetValue(key, out var fieldsToRemove))
				foreach (var field in fieldsToRemove)
					chromeNode.RemoveNodes(field);

			var childrenNode = chromeNode.LastChildMatching("Children");
			if (childrenNode != null)
				foreach (var childNode in childrenNode.Value.Nodes)
					foreach (var result in UpdateChromeProviderNode(modData, childNode))
						yield return result;
		}
	}
}
