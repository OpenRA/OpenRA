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
		const string MapGenerated = "notification-map-generator-generated";

		[FluentReference]
		const string MapGeneratorFailedTitle = "dialog-notification-map-generator-failed.title";

		[FluentReference]
		const string MapGeneratorFailedPrompt = "dialog-notification-map-generator-failed.prompt";

		[FluentReference]
		const string MapGeneratorFailedCancel = "dialog-notification-map-generator-failed.cancel";

		readonly EditorActionManager editorActionManager;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ModData modData;
		readonly IEditorMapGeneratorInfo generator;
		readonly MapGenerationArgs generationArgs;

		readonly ScrollPanelWidget optionsPanel;
		readonly Widget checkboxOptionTemplate;
		readonly Widget textOptionTemplate;
		readonly Widget dropdownOptionTemplate;

		[ObjectCreator.UseCtor]
		public MapGeneratorToolLogic(Widget widget, World world, WorldRenderer worldRenderer, ModData modData, IEditorTool tool)
		{
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			this.world = world;
			this.worldRenderer = worldRenderer;
			this.modData = modData;

			generator = tool.TraitInfo as IEditorMapGeneratorInfo;
			generationArgs = new MapGenerationArgs
			{
				Tileset = world.Map.Tileset,
				Size = world.Map.MapSize,
			};

			optionsPanel = widget.Get<ScrollPanelWidget>("OPTIONS_PANEL");
			checkboxOptionTemplate = optionsPanel.Get<Widget>("CHECKBOX_TEMPLATE");
			textOptionTemplate = optionsPanel.Get<Widget>("TEXT_TEMPLATE");
			dropdownOptionTemplate = optionsPanel.Get<Widget>("DROPDOWN_TEMPLATE");

			var generateButtonWidget = widget.Get<ButtonWidget>("GENERATE_BUTTON");
			generateButtonWidget.OnClick = GenerateMap;

			var generateRandomButtonWidget = widget.Get<ButtonWidget>("GENERATE_RANDOM_BUTTON");
			generateRandomButtonWidget.OnClick = () =>
			{
				generationArgs.Options["Seed"] = FieldSaver.FormatValue(world.LocalRandom.Next());
				RefreshOptions();
				GenerateMap();
			};

			RefreshOptions();
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

		void RefreshOptions()
		{
			optionsPanel.RemoveChildren();
			if (generator == null)
				return;

			var trueString = FieldSaver.FormatValue(true);
			var falseString = FieldSaver.FormatValue(false);

			foreach (var o in generator.Options)
			{
				Widget optionWidget = null;
				var hidden = !o.Visibility.HasFlag(MapGeneratorOption.VisibilityFlags.Editor);
				switch (o)
				{
					case MapGeneratorBooleanOption bo:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = bo.Default ? trueString : falseString;

						if (hidden)
							break;

						optionWidget = checkboxOptionTemplate.Clone();
						var checkboxWidget = optionWidget.Get<CheckboxWidget>("CHECKBOX");
						var label = FluentProvider.GetMessage(bo.Label);
						checkboxWidget.GetText = () => label;
						checkboxWidget.IsChecked = () => generationArgs.Options[o.Id] == trueString;
						checkboxWidget.OnClick = () =>
							generationArgs.Options[o.Id] = generationArgs.Options[o.Id] == trueString ? falseString : trueString;
						break;
					}

					case MapGeneratorIntegerOption io:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = FieldSaver.FormatValue(io.Default);

						if (hidden)
							break;

						optionWidget = textOptionTemplate.Clone();
						var labelWidget = optionWidget.Get<LabelWidget>("LABEL");
						var label = FluentProvider.GetMessage(io.Label);
						labelWidget.GetText = () => label;
						var textFieldWidget = optionWidget.Get<TextFieldWidget>("INPUT");
						textFieldWidget.Type = TextFieldType.Integer;
						textFieldWidget.Text = generationArgs.Options[o.Id];
						textFieldWidget.OnTextEdited = () =>
						{
							var valid = int.TryParse(textFieldWidget.Text, out _);
							if (valid)
								generationArgs.Options[o.Id] = textFieldWidget.Text;
							textFieldWidget.IsValid = () => valid;
						};

						textFieldWidget.OnEscKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						textFieldWidget.OnEnterKey = _ => { textFieldWidget.YieldKeyboardFocus(); return true; };
						break;
					}

					case MapGeneratorMultiIntegerChoiceOption mio:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = FieldSaver.FormatValue(mio.Default);

						if (hidden)
							break;

						optionWidget = dropdownOptionTemplate.Clone();
						var labelWidget = optionWidget.Get<LabelWidget>("LABEL");
						var label = FluentProvider.GetMessage(mio.Label);
						labelWidget.GetText = () => label;

						var dropDownWidget = optionWidget.Get<DropDownButtonWidget>("DROPDOWN");
						dropDownWidget.GetText = () => generationArgs.Options[o.Id];
						dropDownWidget.OnMouseDown = _ =>
						{
							ScrollItemWidget SetupItem(int choice, ScrollItemWidget template)
							{
								var choiceString = FieldSaver.FormatValue(choice);
								bool IsSelected() => choiceString == generationArgs.Options[o.Id];
								void OnClick()
								{
									generationArgs.Options[o.Id] = choiceString;
									if (o.Id == "Players")
										RefreshOptions();
								}

								var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
								var itemLabel = FieldSaver.FormatValue(choice);
								item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
								item.GetTooltipText = null;
								return item;
							}

							dropDownWidget.ShowDropDown("LABEL_DROPDOWN_WITH_TOOLTIP_TEMPLATE", 250, mio.Choices, SetupItem);
						};
						break;
					}

					case MapGeneratorMultiChoiceOption mo:
					{
						var playerCount = generator.GetPlayerCount(generationArgs);
						var validChoices = mo.ValidChoices(world.Map.Rules.TerrainInfo, playerCount);
						if (!generationArgs.Options.TryGetValue(o.Id, out var option) || !validChoices.Contains(option))
							generationArgs.Options[o.Id] = mo.DefaultFor(world.Map.Rules.TerrainInfo, playerCount);

						if (hidden || mo.Label == null || validChoices.Count == 0)
							break;

						optionWidget = dropdownOptionTemplate.Clone();
						var labelWidget = optionWidget.Get<LabelWidget>("LABEL");
						var label = FluentProvider.GetMessage(mo.Label);
						labelWidget.GetText = () => label;

						var labelCache = new CachedTransform<string, string>(v => FluentProvider.GetMessage(mo.Choices[v].Label + ".label"));
						var dropDownWidget = optionWidget.Get<DropDownButtonWidget>("DROPDOWN");
						dropDownWidget.GetText = () => labelCache.Update(generationArgs.Options[o.Id]);
						dropDownWidget.OnMouseDown = _ =>
						{
							ScrollItemWidget SetupItem(string choice, ScrollItemWidget template)
							{
								bool IsSelected() => choice == generationArgs.Options[o.Id];
								void OnClick() => generationArgs.Options[o.Id] = choice;
								var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);

								var itemLabel = FluentProvider.GetMessage(mo.Choices[choice].Label + ".label");
								item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
								if (FluentProvider.TryGetMessage(mo.Choices[choice].Label + ".description", out var desc))
									item.GetTooltipText = () => desc;
								else
									item.GetTooltipText = null;

								return item;
							}

							dropDownWidget.ShowDropDown("LABEL_DROPDOWN_WITH_TOOLTIP_TEMPLATE", 250, validChoices, SetupItem);
						};

						break;
					}

					default:
						throw new NotImplementedException($"Unhandled MapGeneratorOption type {o.GetType().Name}");
				}

				if (optionWidget == null)
					continue;

				optionWidget.IsVisible = () => true;
				optionsPanel.AddChild(optionWidget);
			}
		}

		void DisplayError(Exception e)
		{
			var message = e is MapGenerationException ? e.Message : MapGeneratorFailedPrompt;
			Log.Write("debug", e);
			ConfirmationDialogs.ButtonPrompt(modData,
				title: MapGeneratorFailedTitle,
				text: message,
				onCancel: () => { },
				cancelText: MapGeneratorFailedCancel);
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
			// Run main generator logic. May throw.
			var generateStopwatch = Stopwatch.StartNew();
			Log.Write("debug", $"Running '{generator.Type}' map generator with options:\n{generationArgs.Options
				.Select(kv => $"{kv.Key}: {kv.Value}").JoinWith("\n")}\n\n");
			var generatedMap = generator.Generate(modData, generationArgs);
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
				var actorReference = new ActorReference(kv.Value.Value, kv.Value);
				var ownerInit = actorReference.Get<OwnerInit>();
				if (!players.TryGetValue(ownerInit.InternalName, out var owner))
					throw new MapGenerationException("Generator produced mismatching player and actor definitions.");

				var preview = new EditorActorPreview(worldRenderer, kv.Key, actorReference, owner);
				previews.Add(kv.Key, preview);
			}

			var cellBounds = CellLayerUtils.CellBounds(world.Map);
			var topLeft = new CPos(cellBounds.TopLeft.X, cellBounds.TopLeft.Y);
			var bottomRight = new CPos(cellBounds.BottomRight.X, cellBounds.BottomRight.Y);
			var cellRegion = new CellCoordsRegion(topLeft, bottomRight);
			var blitSource = new EditorBlitSource(cellRegion, previews, tiles);
			var editorBlit = new EditorBlit(
				MapBlitFilters.All,
				resourceLayer,
				topLeft,
				world.Map,
				blitSource,
				editorActorLayer,
				false);

			var description = FluentProvider.GetMessage(MapGenerated, "name", FluentProvider.GetMessage(generator.Name));
			var action = new RandomMapEditorAction(editorBlit, description);
			editorActionManager.Add(action);
		}
	}
}
