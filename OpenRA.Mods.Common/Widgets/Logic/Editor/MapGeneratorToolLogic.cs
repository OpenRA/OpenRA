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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapGeneratorToolLogic : ChromeLogic
	{
		[FluentReference("name")]
		const string StrGenerated = "notification-map-generator-generated";
		[FluentReference]
		const string StrBadOption = "notification-map-generator-bad-option";
		[FluentReference]
		const string StrFailed = "notification-map-generator-failed";
		[FluentReference]
		const string StrFailedCancel = "label-map-generator-failed-cancel";

		readonly EditorActionManager editorActionManager;
		readonly ButtonWidget generateButtonWidget;
		readonly ButtonWidget generateRandomButtonWidget;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ModData modData;

		// nullable
		IMapGenerator selectedGenerator;

		readonly Dictionary<IMapGenerator, MapGeneratorSettings> generatorsToSettings;
		readonly Dictionary<IMapGenerator, Dictionary<MapGeneratorSettings.Option, MapGeneratorSettings.Choice>> generatorsToSettingsChoices;

		readonly ScrollPanelWidget settingsPanel;
		readonly Widget checkboxSettingTemplate;
		readonly Widget textSettingTemplate;
		readonly Widget dropDownSettingTemplate;

		[ObjectCreator.UseCtor]
		public MapGeneratorToolLogic(Widget widget, World world, WorldRenderer worldRenderer, ModData modData)
		{
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			this.world = world;
			this.worldRenderer = worldRenderer;
			this.modData = modData;

			selectedGenerator = null;
			generatorsToSettings = new Dictionary<IMapGenerator, MapGeneratorSettings>();
			generatorsToSettingsChoices = new Dictionary<IMapGenerator, Dictionary<MapGeneratorSettings.Option, MapGeneratorSettings.Choice>>();

			var mapGenerators = new List<IMapGenerator>();
			foreach (var generator in world.WorldActor.TraitsImplementing<IMapGenerator>())
			{
				var settings = generator.GetSettings(world.Map);
				if (settings == null)
					continue;
				var choices = settings.DefaultChoices();
				mapGenerators.Add(generator);
				generatorsToSettingsChoices.Add(generator, choices);
				generatorsToSettings.Add(generator, settings);
			}

			generateButtonWidget = widget.Get<ButtonWidget>("GENERATE_BUTTON");
			generateRandomButtonWidget = widget.Get<ButtonWidget>("GENERATE_RANDOM_BUTTON");

			settingsPanel = widget.Get<ScrollPanelWidget>("SETTINGS_PANEL");
			checkboxSettingTemplate = settingsPanel.Get<Widget>("CHECKBOX_TEMPLATE");
			textSettingTemplate = settingsPanel.Get<Widget>("TEXT_TEMPLATE");
			dropDownSettingTemplate = settingsPanel.Get<Widget>("DROPDOWN_TEMPLATE");

			generateButtonWidget.OnClick = GenerateMap;
			generateRandomButtonWidget.OnClick = () =>
			{
				Randomize();
				GenerateMap();
			};

			var generatorDropDown = widget.Get<DropDownButtonWidget>("GENERATOR");
			ChangeGenerator(mapGenerators.FirstOrDefault((IMapGenerator)null));
			if (selectedGenerator != null)
			{
				generatorDropDown.GetText = () => FluentProvider.GetMessage(selectedGenerator.Info.Name);
				generatorDropDown.OnMouseDown = _ =>
				{
					ScrollItemWidget SetupItem(IMapGenerator g, ScrollItemWidget template)
					{
						bool IsSelected() => g.Info.Type == selectedGenerator.Info.Type;
						void OnClick() => ChangeGenerator(mapGenerators.First(generator => generator.Info.Type == g.Info.Type));
						var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => FluentProvider.GetMessage(g.Info.Name);
						return item;
					}

					generatorDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", mapGenerators.Count * 30, mapGenerators, SetupItem);
				};
			}
			else
			{
				generateButtonWidget.IsDisabled = () => true;
				generateRandomButtonWidget.IsDisabled = () => true;
				generatorDropDown.IsDisabled = () => true;
			}
		}

		sealed class RandomMapEditorAction : IEditorAction
		{
			public string Text { get; }

			readonly EditorBlit editorBlit;

			public RandomMapEditorAction(EditorBlit editorBlit, string description)
			{
				this.editorBlit = editorBlit;

				Text = description;
			}

			public void Execute()
			{
				Do();
			}

			public void Do()
			{
				editorBlit.Commit();
			}

			public void Undo()
			{
				editorBlit.Revert();
			}
		}

		// newGenerator may be null.
		void ChangeGenerator(IMapGenerator newGenerator)
		{
			selectedGenerator = newGenerator;

			UpdateSettingsUi();
		}

		void UpdateSettingsUi()
		{
			settingsPanel.RemoveChildren();
			settingsPanel.ContentHeight = 0;
			if (selectedGenerator == null)
				return;

			var settings = generatorsToSettings[selectedGenerator];
			var choices = generatorsToSettingsChoices[selectedGenerator];
			foreach (var option in settings.Options)
			{
				Widget settingWidget;

				switch (option.Ui)
				{
					case MapGeneratorSettings.UiType.Hidden:
						continue;

					case MapGeneratorSettings.UiType.DropDown:
					{
						settingWidget = dropDownSettingTemplate.Clone();
						var label = settingWidget.Get<LabelWidget>("LABEL");
						label.GetText = () => option.Label;
						var dropDown = settingWidget.Get<DropDownButtonWidget>("DROPDOWN");
						dropDown.GetText = () => choices[option].Label;
						dropDown.OnMouseDown = _ =>
						{
							ScrollItemWidget SetupItem(MapGeneratorSettings.Choice choice, ScrollItemWidget template)
							{
								bool IsSelected() => choice == choices[option];
								void OnClick() => choices[option] = choice;
								var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
								item.Get<LabelWidget>("LABEL").GetText = () => choice.Label;
								return item;
							}

							dropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", option.Choices.Count * 30, option.Choices, SetupItem);
						};
						break;
					}

					case MapGeneratorSettings.UiType.Checkbox:
					{
						if (option.Choices.Count != 2)
							throw new InvalidOperationException("Checkbox option that does not have two choices");
						settingWidget = checkboxSettingTemplate.Clone();
						var checkbox = settingWidget.Get<CheckboxWidget>("CHECKBOX");
						checkbox.GetText = () => option.Label;
						checkbox.IsChecked = () => choices[option] == option.Choices[1];
						checkbox.OnClick = () =>
							choices[option] =
								choices[option] == option.Choices[1]
									? option.Choices[0]
									: option.Choices[1];
						break;
					}

					case MapGeneratorSettings.UiType.Integer:
					case MapGeneratorSettings.UiType.Float:
					case MapGeneratorSettings.UiType.String:
					{
						if (option.Choices.Count != 1)
							throw new InvalidOperationException("Text option that does not have one choice");
						settingWidget = textSettingTemplate.Clone();
						var label = settingWidget.Get<LabelWidget>("LABEL");
						label.GetText = () => option.Label;
						var input = settingWidget.Get<TextFieldWidget>("INPUT");
						input.Text = choices[option].Id;
						input.OnTextEdited = () =>
						{
							var choice = choices[option].NewValue(input.Text);
							choices[option] = choice;
							var valid = option.ValidateChoice(choice);
							input.IsValid = () => valid;
						};

						break;
					}

					default:
						throw new NotSupportedException("Unsupported MapGeneratorSettings.UiType");
				}

				settingWidget.IsVisible = () => true;
				settingsPanel.AddChild(settingWidget);
			}
		}

		void DisplayError(Exception e)
		{
			// For any non-MapGenerationException, include more information for debugging purposes.
			var message = e is MapGenerationException ? e.Message : e.ToString();
			Log.Write("debug", e);
			ConfirmationDialogs.ButtonPrompt(modData,
				title: StrFailed,
				text: message,
				onCancel: () => { },
				cancelText: StrFailedCancel);
		}

		void Randomize()
		{
			var choices = generatorsToSettingsChoices[selectedGenerator];
			foreach (var option in choices.Keys)
			{
				if (option.Random)
					choices[option] = option.RandomChoice();
			}

			UpdateSettingsUi();
		}

		void GenerateMap()
		{
			try
			{
				GenerateMapMayThrow();
			}
			catch (Exception e) when (e is MapGenerationException || e is YamlException)
			{
				DisplayError(e);
			}
		}

		void GenerateMapMayThrow()
		{
			var map = world.Map;
			var tileset = modData.DefaultTerrainInfo[map.Tileset];
			var generatedMap = new Map(modData, tileset, map.MapSize.X, map.MapSize.Y);
			var bounds = map.Bounds;
			generatedMap.SetBounds(new PPos(bounds.Left, bounds.Top), new PPos(bounds.Right - 1, bounds.Bottom - 1));
			var choices = generatorsToSettingsChoices[selectedGenerator];

			foreach (var optionChoice in choices)
			{
				var option = optionChoice.Key;
				var choice = optionChoice.Value;
				if (!option.ValidateChoice(choice))
					throw new MapGenerationException(
						FluentProvider.GetMessage(StrBadOption, "option", option.Label));
			}

			var settings = generatorsToSettings[selectedGenerator].Compile(choices);

			// Run main generator logic. May throw.
			var generateStopwatch = Stopwatch.StartNew();
			Log.Write("debug", $"Running '{selectedGenerator.Info.Type}' map generator with settings:\n{MiniYamlExts.WriteToString(settings.Nodes)}\n\n");
			selectedGenerator.Generate(generatedMap, settings);
			Log.Write("debug", $"Generator finished, taking {generateStopwatch.ElapsedMilliseconds}ms");

			var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			var resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();

			// Hack, hack, hack.
			var resourceTypesByIndex = (resourceLayer.Info as EditorResourceLayerInfo).ResourceTypes.ToDictionary(
				kv => kv.Value.ResourceIndex,
				kv => kv.Key);

			var tiles = new Dictionary<CPos, BlitTile>();
			foreach (var cell in generatedMap.AllCells)
			{
				var mpos = cell.ToMPos(map);
				var resourceTile = generatedMap.Resources[mpos];
				resourceTypesByIndex.TryGetValue(resourceTile.Type, out var resourceType);
				var resourceLayerContents = new ResourceLayerContents(resourceType, resourceTile.Index);
				tiles.Add(cell, new BlitTile(generatedMap.Tiles[mpos], resourceTile, resourceLayerContents, generatedMap.Height[mpos]));
			}

			var previews = new Dictionary<string, EditorActorPreview>();
			var players = generatedMap.PlayerDefinitions.Select(pr => new PlayerReference(new MiniYaml(pr.Key, pr.Value.Nodes)))
				.ToDictionary(player => player.Name);
			foreach (var kv in generatedMap.ActorDefinitions)
			{
				var actorReference = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());
				var ownerInit = actorReference.Get<OwnerInit>();
				if (!players.TryGetValue(ownerInit.InternalName, out var owner))
					throw new MapGenerationException("Generator produced mismatching player and actor definitions.");

				var preview = new EditorActorPreview(worldRenderer, kv.Key, actorReference, owner);
				previews.Add(kv.Key, preview);
			}

			var blitSource = new EditorBlitSource(generatedMap.AllCells, previews, tiles);
			var editorBlit = new EditorBlit(
				MapBlitFilters.All,
				resourceLayer,
				new CPos(0, 0),
				map,
				blitSource,
				editorActorLayer,
				false);

			var description = FluentProvider.GetMessage(StrGenerated,
				"name", FluentProvider.GetMessage(selectedGenerator.Info.Name));
			var action = new RandomMapEditorAction(editorBlit, description);
			editorActionManager.Add(action);
		}
	}
}
