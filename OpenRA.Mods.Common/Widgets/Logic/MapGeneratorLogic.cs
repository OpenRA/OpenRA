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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapGeneratorLogic : ChromeLogic
	{
		[FluentReference]
		const string Tileset = "label-mapchooser-random-map-tileset";

		[FluentReference]
		const string MapSize = "label-mapchooser-random-map-size";

		[FluentReference]
		const string RandomMap = "label-mapchooser-random-map-title";

		[FluentReference]
		const string Generating = "label-mapchooser-random-map-generating";

		[FluentReference]
		const string GenerationFailed = "label-mapchooser-random-map-error";

		[FluentReference("players")]
		const string Players = "label-player-count";

		[FluentReference("author")]
		const string CreatedBy = "label-created-by";

		[FluentReference]
		const string MapSizeSmall = "label-map-size-small";

		[FluentReference]
		const string MapSizeMedium = "label-map-size-medium";

		[FluentReference]
		const string MapSizeLarge = "label-map-size-large";

		[FluentReference]
		const string MapSizeHuge = "label-map-size-huge";

		public static readonly IReadOnlyDictionary<string, int2> MapSizes = new Dictionary<string, int2>()
		{
			{ MapSizeSmall, new int2(48, 60) },
			{ MapSizeMedium, new int2(60, 90) },
			{ MapSizeLarge, new int2(90, 120) },
			{ MapSizeHuge, new int2(120, 160) },
		};

		readonly ModData modData;
		readonly IEditorMapGeneratorInfo generator;
		readonly MapGenerationArgs generationArgs;
		readonly Action<MapGenerationArgs, IReadWritePackage> onGenerate;

		readonly GeneratedMapPreviewWidget preview;
		readonly ScrollPanelWidget optionsPanel;
		readonly Widget checkboxOptionTemplate;
		readonly Widget textOptionTemplate;
		readonly Widget dropdownOptionTemplate;
		readonly Widget tilesetOption;
		readonly Widget sizeOption;
		readonly Widget parentWidget;

		ITerrainInfo selectedTerrain;
		string selectedSize;
		bool initialGenerationDone;

		volatile bool failed;
		volatile uint generationCounter = 0;
		volatile uint lastGeneration = 0;

		bool IsGenerating => lastGeneration != generationCounter;

		[ObjectCreator.UseCtor]
		internal MapGeneratorLogic(Widget widget, ModData modData, MapGenerationArgs initialGeneratedMap, Action<MapGenerationArgs, IReadWritePackage> onGenerate)
		{
			this.modData = modData;
			this.onGenerate = onGenerate;
			parentWidget = widget.Parent;

			generator = modData.DefaultRules.Actors[SystemActors.EditorWorld].TraitInfos<IEditorMapGeneratorInfo>().First();
			preview = widget.Get<GeneratedMapPreviewWidget>("PREVIEW");

			widget.Get("ERROR").IsVisible = () => failed;

			var title = new CachedTransform<string, string>(id => FluentProvider.GetMessage(id));
			var previewTitleLabel = widget.Get<LabelWidget>("TITLE");
			previewTitleLabel.GetText = () => title.Update(IsGenerating ? Generating : failed ? GenerationFailed : RandomMap);

			var previewDetailsLabel = widget.GetOrNull<LabelWidget>("DETAILS");
			if (previewDetailsLabel != null)
			{
				// The default "Conquest" label is hardcoded in Map.cs
				var desc = new CachedTransform<int, string>(p => "Conquest " + FluentProvider.GetMessage(Players, "players", p));
				previewDetailsLabel.GetText = () => desc.Update(generator.GetPlayerCount(generationArgs));
				previewDetailsLabel.IsVisible = () => !failed;
			}

			var previewAuthorLabel = widget.GetOrNull<LabelWithTooltipWidget>("AUTHOR");
			if (previewAuthorLabel != null)
			{
				var desc = FluentProvider.GetMessage(CreatedBy, "author", FluentProvider.GetMessage(generator.Name));
				previewAuthorLabel.GetText = () => desc;
				previewAuthorLabel.IsVisible = () => !failed;
			}

			var previewSizeLabel = widget.GetOrNull<LabelWidget>("SIZE");
			if (previewSizeLabel != null)
			{
				var desc = new CachedTransform<Size, string>(MapChooserLogic.MapSizeLabel);
				previewSizeLabel.IsVisible = () => !failed;
				previewSizeLabel.GetText = () => desc.Update(generationArgs.Size);
			}

			optionsPanel = widget.Get<ScrollPanelWidget>("OPTIONS_PANEL");
			checkboxOptionTemplate = optionsPanel.Get<Widget>("CHECKBOX_TEMPLATE");
			textOptionTemplate = optionsPanel.Get<Widget>("TEXT_TEMPLATE");
			dropdownOptionTemplate = optionsPanel.Get<Widget>("DROPDOWN_TEMPLATE");
			optionsPanel.Layout = new GridLayout(optionsPanel);

			// Tileset and map size are handled outside the generator logic so must be created manually
			var validTerrainInfos = generator.Tilesets.Select(t => modData.DefaultTerrainInfo[t]).ToList();
			var tilesetLabel = FluentProvider.GetMessage(Tileset);
			tilesetOption = dropdownOptionTemplate.Clone();
			tilesetOption.Get<LabelWidget>("LABEL").GetText = () => tilesetLabel;

			var label = new CachedTransform<ITerrainInfo, string>(ti => FluentProvider.GetMessage(ti.Name));
			var tilesetDropdown = tilesetOption.Get<DropDownButtonWidget>("DROPDOWN");
			tilesetDropdown.GetText = () => label.Update(selectedTerrain);
			tilesetDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(ITerrainInfo terrainInfo, ScrollItemWidget template)
				{
					bool IsSelected() => terrainInfo == selectedTerrain;
					void OnClick()
					{
						selectedTerrain = terrainInfo;
						generationArgs.Tileset = terrainInfo.Id;
						RefreshOptions();
						GenerateMap();
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var itemLabel = FluentProvider.GetMessage(terrainInfo.Name);
					item.Get<LabelWidget>("LABEL").GetText = () => itemLabel;
					return item;
				}

				tilesetDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", validTerrainInfos.Count * 30, validTerrainInfos, SetupItem);
			};

			var sizeLabel = FluentProvider.GetMessage(MapSize);
			sizeOption = dropdownOptionTemplate.Clone();
			sizeOption.Get<LabelWidget>("LABEL").GetText = () => sizeLabel;

			var sizeDropdown = sizeOption.Get<DropDownButtonWidget>("DROPDOWN");
			var sizeDropdownLabel = new CachedTransform<string, string>(s => FluentProvider.GetMessage(s));
			sizeDropdown.GetText = () => sizeDropdownLabel.Update(selectedSize);
			sizeDropdown.OnMouseDown = _ =>
			{
				ScrollItemWidget SetupItem(string size, ScrollItemWidget template)
				{
					bool IsSelected() => size == selectedSize;
					void OnClick()
					{
						selectedSize = size;
						RandomizeSize();
						GenerateMap();
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					var label = FluentProvider.GetMessage(size);
					item.Get<LabelWidget>("LABEL").GetText = () => label;
					return item;
				}

				sizeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", MapSizes.Count * 30, MapSizes.Keys, SetupItem);
			};

			var generateButton = widget.Get<ButtonWidget>("BUTTON_GENERATE");
			generateButton.IsDisabled = () => IsGenerating;
			generateButton.OnClick = () =>
			{
				generationArgs.Options["Seed"] = FieldSaver.FormatValue(Game.CosmeticRandom.Next());
				RandomizeSize();
				GenerateMap();
			};

			selectedSize = MapSizes.Keys.Skip(1).First();
			if (initialGeneratedMap != null)
			{
				// Make our own copy to prevent external mutation
				generationArgs = new MapGenerationArgs()
				{
					Uid = initialGeneratedMap.Uid,
					Generator = initialGeneratedMap.Generator,
					Tileset = initialGeneratedMap.Tileset,
					Size = initialGeneratedMap.Size,
					Options = initialGeneratedMap.Options.ToDictionary(),
					Title = FluentProvider.GetMessage(generator.MapTitle),
					Author = FluentProvider.GetMessage(generator.Name)
				};

				selectedTerrain = modData.DefaultTerrainInfo[generationArgs.Tileset];
				foreach (var kv in MapSizes)
					if (kv.Value.X > generationArgs.Size.Width && kv.Value.Y <= generationArgs.Size.Width)
						selectedSize = kv.Key;

				RefreshOptions();

				var map = modData.MapCache[generationArgs.Uid];
				if (map.Status == MapStatus.Available)
				{
					preview.Update(map);
					initialGenerationDone = true;
					onGenerate(generationArgs, null);
				}
			}
			else
			{
				selectedTerrain = validTerrainInfos[0];
				generationArgs = new MapGenerationArgs
				{
					Generator = generator.Type,
					Tileset = selectedTerrain.Id,
					Title = FluentProvider.GetMessage(generator.MapTitle),
					Author = FluentProvider.GetMessage(generator.Name),
				};

				generationArgs.Options["Seed"] = FieldSaver.FormatValue(Game.CosmeticRandom.Next());
				RandomizeSize();
				RefreshOptions();
			}
		}

		public override void Tick()
		{
			if (!initialGenerationDone && !IsGenerating && parentWidget.IsVisible())
			{
				initialGenerationDone = true;
				GenerateMap();
			}
		}

		void RandomizeSize()
		{
			var mapGrid = modData.GetOrCreate<MapGrid>();
			var sizeRange = MapSizes[selectedSize];
			var width = Game.CosmeticRandom.Next(sizeRange.X, sizeRange.Y);
			var height = mapGrid.Type == MapGridType.RectangularIsometric ? width * 2 : width;

			generationArgs.Size = new Size(width + 2, height + mapGrid.MaximumTerrainHeight * 2 + 2);
		}

		void RefreshOptions()
		{
			optionsPanel.RemoveChildren();
			tilesetOption.Bounds = sizeOption.Bounds = dropdownOptionTemplate.Bounds;
			optionsPanel.AddChild(tilesetOption);
			optionsPanel.AddChild(sizeOption);

			var trueString = FieldSaver.FormatValue(true);
			var falseString = FieldSaver.FormatValue(false);
			foreach (var o in generator.Options)
			{
				if (o.Id == "Seed")
					continue;

				Widget optionWidget = null;
				switch (o)
				{
					case MapGeneratorBooleanOption bo:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = bo.Default ? falseString : trueString;

						optionWidget = checkboxOptionTemplate.Clone();
						var checkboxWidget = optionWidget.Get<CheckboxWidget>("CHECKBOX");
						var label = FluentProvider.GetMessage(bo.Label);
						checkboxWidget.GetText = () => label;
						checkboxWidget.IsChecked = () => generationArgs.Options[o.Id] == trueString;
						checkboxWidget.OnClick = () =>
						{
							generationArgs.Options[o.Id] = generationArgs.Options[o.Id] == trueString ? falseString : trueString;
							GenerateMap();
						};
						break;
					}

					case MapGeneratorIntegerOption io:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = FieldSaver.FormatValue(io.Default);

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
						textFieldWidget.OnLoseFocus = GenerateMap;
						break;
					}

					case MapGeneratorMultiIntegerChoiceOption mio:
					{
						if (!generationArgs.Options.ContainsKey(o.Id))
							generationArgs.Options[o.Id] = FieldSaver.FormatValue(mio.Default);

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
									GenerateMap();
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
						var validChoices = mo.ValidChoices(selectedTerrain, playerCount);
						if (!generationArgs.Options.TryGetValue(o.Id, out var option) || !validChoices.Contains(option))
							generationArgs.Options[o.Id] = mo.DefaultFor(selectedTerrain, playerCount);

						if (mo.Label != null && validChoices.Count > 0)
						{
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
									void OnClick()
									{
										generationArgs.Options[o.Id] = choice;
										GenerateMap();
									}

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
						}

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

		void GenerateMap()
		{
			var currentGeneration = Interlocked.Increment(ref generationCounter);

			failed = false;
			onGenerate(null, null);
			preview.Clear();

			Task.Run(() =>
			{
				// Tasks don't run in parallel, so we may be able to cancel some outdated requests here.
				if (currentGeneration != generationCounter)
					return;

				Map map;
				try
				{
					map = generator.Generate(modData, generationArgs);
				}
				catch
				{
					// We are the lastest generation request, mark as failed.
					if (currentGeneration == generationCounter)
					{
						lastGeneration = currentGeneration;
						failed = true;
					}

					return;
				}

				// Need to invoke widgets from the main thread.
				Game.RunAfterTick(() =>
				{
					// A newer generation will be set after us, discard.
					if (currentGeneration == generationCounter)
					{
						var package = new ZipFileLoader.ReadWriteZipFile();
						map.Save(package);

						generationArgs.Uid = map.Uid;

						preview.Update(map);
						lastGeneration = currentGeneration;

						// `onGenerate` assumed to take ownership of package here.
						onGenerate(generationArgs, package);
					}
				});
			});
		}
	}
}
