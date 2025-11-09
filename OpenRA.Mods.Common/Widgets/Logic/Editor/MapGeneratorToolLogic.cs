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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapGeneratorToolLogic : ChromeLogic
	{
		[FluentReference("name")]
		const string StrGenerated = "notification-map-generator-generated";
		[FluentReference]
		const string StrFailed = "notification-map-generator-failed";
		[FluentReference]
		const string StrFailedCancel = "label-map-generator-failed-cancel";

		readonly EditorActionManager editorActionManager;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ModData modData;
		readonly IEditorMapGeneratorInfo generator;
		readonly IMapGeneratorSettings settings;

		readonly ScrollPanelWidget settingsPanel;
		readonly Widget checkboxSettingTemplate;
		readonly Widget textSettingTemplate;
		readonly Widget dropDownSettingTemplate;

		[ObjectCreator.UseCtor]
		public MapGeneratorToolLogic(Widget widget, World world, WorldRenderer worldRenderer, ModData modData, IEditorTool tool)
		{
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			this.world = world;
			this.worldRenderer = worldRenderer;
			this.modData = modData;
			generator = tool.TraitInfo as IEditorMapGeneratorInfo;
			settings = generator.GetSettings();

			settingsPanel = widget.Get<ScrollPanelWidget>("SETTINGS_PANEL");
			checkboxSettingTemplate = settingsPanel.Get<Widget>("CHECKBOX_TEMPLATE");
			textSettingTemplate = settingsPanel.Get<Widget>("TEXT_TEMPLATE");
			dropDownSettingTemplate = settingsPanel.Get<Widget>("DROPDOWN_TEMPLATE");

			var generateButtonWidget = widget.Get<ButtonWidget>("GENERATE_BUTTON");
			generateButtonWidget.OnClick = GenerateMap;

			var generateRandomButtonWidget = widget.Get<ButtonWidget>("GENERATE_RANDOM_BUTTON");
			generateRandomButtonWidget.OnClick = () =>
			{
				settings?.Randomize(world.LocalRandom);
				UpdateSettingsUi();
				GenerateMap();
			};

			UpdateSettingsUi();
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

		void UpdateSettingsUi()
		{
			settingsPanel.RemoveChildren();
			settingsPanel.ContentHeight = 0;
			if (generator == null)
				return;

			var playerCount = settings.PlayerCount;
			foreach (var o in settings.Options)
			{
				Widget settingWidget = null;
				switch (o)
				{
					case MapGeneratorBooleanOption bo:
					{
						settingWidget = checkboxSettingTemplate.Clone();
						var checkboxWidget = settingWidget.Get<CheckboxWidget>("CHECKBOX");
						var label = FluentProvider.GetMessage(bo.Label);
						checkboxWidget.GetText = () => label;
						checkboxWidget.IsChecked = () => bo.Value;
						checkboxWidget.OnClick = () => bo.Value ^= true;
						break;
					}

					case MapGeneratorIntegerOption io:
					{
						settingWidget = textSettingTemplate.Clone();
						var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
						var label = FluentProvider.GetMessage(io.Label);
						labelWidget.GetText = () => label;
						var textFieldWidget = settingWidget.Get<TextFieldWidget>("INPUT");
						textFieldWidget.Type = TextFieldType.Integer;
						textFieldWidget.Text = FieldSaver.FormatValue(io.Value);
						textFieldWidget.OnTextEdited = () =>
						{
							var valid = int.TryParse(textFieldWidget.Text, out io.Value);
							textFieldWidget.IsValid = () => valid;
						};

						textFieldWidget.OnEscKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						textFieldWidget.OnEnterKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						break;
					}

					case MapGeneratorMultiIntegerChoiceOption mio:
					{
						settingWidget = dropDownSettingTemplate.Clone();
						var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
						var label = FluentProvider.GetMessage(mio.Label);
						labelWidget.GetText = () => label;

						var labelCache = new CachedTransform<int, string>(v => FieldSaver.FormatValue(v));
						var dropDownWidget = settingWidget.Get<DropDownButtonWidget>("DROPDOWN");
						dropDownWidget.GetText = () => labelCache.Update(mio.Value);
						dropDownWidget.OnMouseDown = _ =>
						{
							ScrollItemWidget SetupItem(int choice, ScrollItemWidget template)
							{
								bool IsSelected() => choice == mio.Value;
								void OnClick()
								{
									mio.Value = choice;
									if (o.Id == "Players")
										UpdateSettingsUi();
								}

								var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
								var itemLabel = FieldSaver.FormatValue(choice);
								item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
								item.GetTooltipText = null;
								return item;
							}

							dropDownWidget.ShowDropDown("LABEL_DROPDOWN_WITH_TOOLTIP_TEMPLATE", mio.Choices.Length * 30, mio.Choices, SetupItem);
						};
						break;
					}

					case MapGeneratorMultiChoiceOption mo:
					{
						var validChoices = mo.ValidChoices(world.Map.Rules.TerrainInfo, playerCount);
						if (!validChoices.Contains(mo.Value))
							mo.Value = mo.Default?.FirstOrDefault(validChoices.Contains) ?? validChoices.FirstOrDefault();

						if (mo.Value != null && mo.Label != null && validChoices.Count > 0)
						{
							settingWidget = dropDownSettingTemplate.Clone();
							var labelWidget = settingWidget.Get<LabelWidget>("LABEL");
							var label = FluentProvider.GetMessage(mo.Label);
							labelWidget.GetText = () => label;

							var labelCache = new CachedTransform<string, string>(v => FluentProvider.GetMessage(mo.Choices[v].Label + ".label"));
							var dropDownWidget = settingWidget.Get<DropDownButtonWidget>("DROPDOWN");
							dropDownWidget.GetText = () => labelCache.Update(mo.Value);
							dropDownWidget.OnMouseDown = _ =>
							{
								ScrollItemWidget SetupItem(string choice, ScrollItemWidget template)
								{
									bool IsSelected() => choice == mo.Value;
									void OnClick() => mo.Value = choice;
									var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);

									var itemLabel = FluentProvider.GetMessage(mo.Choices[choice].Label + ".label");
									item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
									if (FluentProvider.TryGetMessage(mo.Choices[choice].Label + ".description", out var desc))
										item.GetTooltipText = () => desc;
									else
										item.GetTooltipText = null;

									return item;
								}

								dropDownWidget.ShowDropDown("LABEL_DROPDOWN_WITH_TOOLTIP_TEMPLATE", validChoices.Count * 30, validChoices, SetupItem);
							};
						}

						break;
					}

					default:
						throw new NotImplementedException($"Unhandled MapGeneratorOption type {o.GetType().Name}");
				}

				if (settingWidget == null)
					continue;

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
			var terrainInfo = modData.DefaultTerrainInfo[map.Tileset];
			var args = settings.Compile(terrainInfo, map.MapSize);

			// Run main generator logic. May throw.
			var generateStopwatch = Stopwatch.StartNew();
			Log.Write("debug", $"Running '{generator.Type}' map generator with settings:\n{MiniYamlExts.WriteToString(args.Settings.Nodes)}\n\n");
			var generatedMap = generator.Generate(modData, args);
			Log.Write("debug", $"Generator finished, taking {generateStopwatch.ElapsedMilliseconds}ms");

			var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			var resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();

			// Hack, hack, hack.
			var resourceTypesByIndex = (resourceLayer.Info as EditorResourceLayerInfo).ResourceTypes.ToDictionary(
				kv => kv.Value.ResourceIndex,
				kv => kv.Key);

			var tiles = new Dictionary<CPos, BlitTile>();
			foreach (var uv in generatedMap.AllCells.MapCoords)
			{
				var resourceTile = generatedMap.Resources[uv];
				resourceTypesByIndex.TryGetValue(resourceTile.Type, out var resourceType);
				var resourceLayerContents = new ResourceLayerContents(resourceType, resourceTile.Index);
				tiles.Add(uv.ToCPos(generatedMap), new BlitTile(generatedMap.Tiles[uv], resourceTile, resourceLayerContents, generatedMap.Height[uv]));
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

			var cellBounds = CellLayerUtils.CellBounds(map);
			var topLeft = new CPos(cellBounds.TopLeft.X, cellBounds.TopLeft.Y);
			var bottomRight = new CPos(cellBounds.BottomRight.X, cellBounds.BottomRight.Y);
			var cellRegion = new CellRegion(map.Grid.Type, topLeft, bottomRight);
			var blitSource = new EditorBlitSource(cellRegion, previews, tiles);
			var editorBlit = new EditorBlit(
				MapBlitFilters.All,
				resourceLayer,
				topLeft,
				map,
				blitSource,
				editorActorLayer,
				false);

			var description = FluentProvider.GetMessage(StrGenerated,
				"name", FluentProvider.GetMessage(generator.Name));
			var action = new RandomMapEditorAction(editorBlit, description);
			editorActionManager.Add(action);
		}
	}
}
