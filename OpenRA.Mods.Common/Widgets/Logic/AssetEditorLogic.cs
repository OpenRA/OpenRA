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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AssetEditorLogic : ChromeLogic
	{
		[TranslationReference]
		const string ExitEditorTitle = "dialog-asseteditor-exit-editor.title";

		[TranslationReference]
		const string ExitEditorPrompt = "dialog-asseteditor-exit-editor.prompt";

		[TranslationReference]
		const string ExitEditorConfirm = "dialog-asseteditor-exit-editor.confirm";

		enum Panel { Editor, Options, Inits }

		readonly World world;

		readonly Widget panel;
		readonly ScrollPanelWidget actorList;
		readonly ScrollItemWidget template;

		WRot modelOrientation;

		ActorInfo selectedActor;

		readonly Ruleset rules;
		readonly ActorSelectorActor[] allActors;
		readonly List<ActorSelectorActor> filteredActors = new();
		readonly PlayerReference selectedOwner;

		readonly Widget initContainer;
		readonly Widget checkboxOptionTemplate;
		readonly Widget sliderOptionTemplate;
		readonly Widget dropdownOptionTemplate;

		readonly Widget editorContainer;
		readonly List<AssetFieldSelector> allEditorFields = new();
		readonly List<AssetFieldSelector> filteredEditorFields = new();
		readonly Widget optionTemplate;
		readonly Widget intOptionTemplate;
		readonly Widget wVecOptionTemplate;
		readonly Widget float3OptionTemplate;

		readonly ScrollPanelWidget optionsContainer;

		readonly HashSet<TextFieldWidget> typableFields = new();

		readonly ActorPreviewWidget preview;
		readonly TextFieldWidget searchTextField;
		string searchFilter;
		readonly SequenceSet customSequences;

		readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> actorEdits = new();
		readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> sequenceEdits = new();
		bool edited;

		IActorPreview[] previewCache;
		Panel currentPanel;

		AssetType assetTypesToDisplay = AssetType.Sprites | AssetType.Models | AssetType.Traits;

		[Flags]
		enum AssetType
		{
			Sprites = 1,
			Models = 2,
			Traits = 4,
		}

		readonly struct ActorSelectorActor
		{
			public readonly ActorInfo Actor;
			public readonly Dictionary<TraitInfo, List<FieldInfo>> Fields;
			public readonly string[] SearchTerms;
			public readonly string Name;

			public ActorSelectorActor(ActorInfo actor, Dictionary<TraitInfo, List<FieldInfo>> properties,
				string[] searchTerms, string name)
			{
				Actor = actor;
				Fields = properties;
				SearchTerms = searchTerms;
				Name = name;
			}
		}

		readonly struct AssetFieldSelector
		{
			public readonly AssetType Type;
			public readonly Widget Widget;

			public AssetFieldSelector(AssetType type, Widget widget)
			{
				Type = type;
				Widget = widget;
			}
		}

		[ObjectCreator.UseCtor]
		public AssetEditorLogic(Widget widget, Action onExit, ModData modData, WorldRenderer worldRenderer)
		{
			world = worldRenderer.World;
			selectedOwner = worldRenderer.World.WorldActor.Owner.PlayerReference;
			panel = widget;

			var colorPickerPalettes = world.WorldActor.TraitsImplementing<IProvidesAssetBrowserColorPickerPalettes>()
				.SelectMany(p => p.ColorPickerPaletteNames)
				.ToArray();

			var colorManager = modData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();

			var colorDropdown = panel.GetOrNull<DropDownButtonWidget>("COLOR");
			if (colorDropdown != null)
			{
				var color = Game.Settings.Player.Color;

				// colorDropdown.IsDisabled = () => !colorPickerPalettes.Contains(currentPalette);
				colorDropdown.IsDisabled = () => true;
				colorDropdown.OnMouseDown = _ => colorManager.ShowColorDropDown(colorDropdown, color, null, worldRenderer, c => color = c);
				colorDropdown.IsVisible = () => selectedActor != null;

				panel.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => color;
			}

			var spriteScaleSlider = panel.GetOrNull<SliderWidget>("SCALE_SLIDER");
			if (spriteScaleSlider != null)
			{
				spriteScaleSlider.OnChange += x => preview.SetScale(x);
				spriteScaleSlider.GetValue = () => preview.Scale;
			}

			var rollSlider = panel.GetOrNull<SliderWidget>("ROLL_SLIDER");
			if (rollSlider != null)
			{
				rollSlider.OnChange += x =>
				{
					var roll = (int)x;
					modelOrientation = modelOrientation.WithRoll(new WAngle(roll));
				};

				rollSlider.GetValue = () => modelOrientation.Roll.Angle;
			}

			var pitchSlider = panel.GetOrNull<SliderWidget>("PITCH_SLIDER");
			if (pitchSlider != null)
			{
				pitchSlider.OnChange += x =>
				{
					var pitch = (int)x;
					modelOrientation = modelOrientation.WithPitch(new WAngle(pitch));
				};

				pitchSlider.GetValue = () => modelOrientation.Pitch.Angle;
			}

			var yawSlider = panel.GetOrNull<SliderWidget>("YAW_SLIDER");
			if (yawSlider != null)
			{
				yawSlider.OnChange += x =>
				{
					var yaw = (int)x;
					modelOrientation = modelOrientation.WithYaw(new WAngle(yaw));
				};

				yawSlider.GetValue = () => modelOrientation.Yaw.Angle;
			}

			var assetTypeDropdown = panel.GetOrNull<DropDownButtonWidget>("TYPES_DROPDOWN");
			if (assetTypeDropdown != null)
			{
				var assetTypesPanel = CreateAssetTypesPanel();
				assetTypeDropdown.OnMouseDown = _ =>
				{
					assetTypeDropdown.RemovePanel();
					assetTypeDropdown.AttachPanel(assetTypesPanel);
				};
			}

			actorList = panel.Get<ScrollPanelWidget>("ASSET_LIST");
			template = panel.Get<ScrollItemWidget>("ASSET_TEMPLATE");

			rules = world.Map.Rules;
			var allActorsTemp = new List<ActorSelectorActor>();
			foreach (var a in rules.Actors.Values)
			{
				// Partial templates are not allowed.
				if (a.Name.Contains(ActorInfo.AbstractActorPrefix))
					continue;

				// Actor must have a preview associated with it.
				if (!a.HasTraitInfo<IRenderActorPreviewInfo>())
					continue;

				var (actor, properties) = Clone(a);

				var editorData = actor.TraitInfoOrDefault<MapEditorDataInfo>();

				// Actor must be included in at least one category.
				if (editorData == null || editorData.Categories == null)
					continue;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(ti => ti.EnabledByDefault);
				var searchTerms = new List<string>() { actor.Name };
				if (tooltip != null)
				{
					var actorName = TranslationProvider.GetString(tooltip.Name);
					searchTerms.Add(actorName);
					allActorsTemp.Add(new ActorSelectorActor(actor, properties, searchTerms.ToArray(), $"{actorName} ({actor.Name})"));
				}
				else
					allActorsTemp.Add(new ActorSelectorActor(actor, properties, searchTerms.ToArray(), actor.Name));
			}

			customSequences = Clone(world.Map.Sequences);
			preview = panel.Get<ActorPreviewWidget>("ACTOR_PREVIEW");
			preview.IsVisible = () => selectedActor != null;
			preview.Sequences = customSequences;

			initContainer = panel.Get("INITS_SCROLLPANEL");
			checkboxOptionTemplate = initContainer.Get("CHECKBOX_OPTION_TEMPLATE");
			sliderOptionTemplate = initContainer.Get("SLIDER_OPTION_TEMPLATE");
			dropdownOptionTemplate = initContainer.Get("DROPDOWN_OPTION_TEMPLATE");

			editorContainer = panel.Get("EDITOR_SCROLLPANEL");
			optionTemplate = editorContainer.Get("OPTION_TEMPLATE");
			intOptionTemplate = optionTemplate.Get("INT_OPTION_TEMPLATE");
			optionTemplate.RemoveChild(intOptionTemplate);
			wVecOptionTemplate = optionTemplate.Get("WVEC_OPTION_TEMPLATE");
			optionTemplate.RemoveChild(wVecOptionTemplate);
			float3OptionTemplate = optionTemplate.Get("FLOAT3_OPTION_TEMPLATE");
			optionTemplate.RemoveChild(float3OptionTemplate);

			optionsContainer = panel.Get<ScrollPanelWidget>("OPTIONS_SCROLLPANEL");
			SettingsUtils.AdjustSettingsScrollPanelLayout(optionsContainer);

			allActors = allActorsTemp.ToArray();
			filteredActors = allActors.ToList();

			InitializeActorList();

			searchTextField = widget.Get<TextFieldWidget>("SEARCH_TEXTFIELD");
			searchTextField.OnEscKey = _ =>
			{
				if (string.IsNullOrEmpty(searchTextField.Text))
					searchTextField.YieldKeyboardFocus();
				else
				{
					searchTextField.Text = "";
					searchTextField.OnTextEdited();
				}

				return true;
			};

			searchTextField.OnTextEdited = () =>
			{
				searchFilter = searchTextField.Text.Trim();
				filteredActors.Clear();

				if (!string.IsNullOrEmpty(searchFilter))
					filteredActors.AddRange(allActors.Where(t => t.SearchTerms.Any(
							s => s.Contains(searchFilter, StringComparison.CurrentCultureIgnoreCase))));
				else
					filteredActors.AddRange(allActors);

				InitializeActorList();
			};

			var saveButton = panel.GetOrNull<ButtonWidget>("EXPORT_BUTTON");
			if (saveButton != null)
			{
				saveButton.OnClick = Export;
				saveButton.IsDisabled = () => !edited;
			}

			var editorButton = panel.GetOrNull<ButtonWidget>("EDITOR_BUTTON");
			if (editorButton != null)
			{
				editorButton.OnClick = () => currentPanel = Panel.Editor;
				editorButton.IsHighlighted = () => currentPanel == Panel.Editor;
				editorContainer.IsVisible = () => currentPanel == Panel.Editor;
			}

			var optionsButton = panel.GetOrNull<ButtonWidget>("OPTIONS_BUTTON");
			if (optionsButton != null)
			{
				optionsButton.OnClick = () => currentPanel = Panel.Options;
				optionsButton.IsHighlighted = () => currentPanel == Panel.Options;
				initContainer.IsVisible = () => currentPanel == Panel.Options;
			}

			var initsButton = panel.GetOrNull<ButtonWidget>("INITS_BUTTON");
			if (initsButton != null)
			{
				initsButton.OnClick = () => currentPanel = Panel.Inits;
				initsButton.IsHighlighted = () => currentPanel == Panel.Inits;
				optionsContainer.IsVisible = () => currentPanel == Panel.Inits;
			}

			var closeButton = panel.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			if (closeButton != null)
			{
				closeButton.OnClick = () =>
				{
					if (edited)
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: ExitEditorTitle,
							text: ExitEditorPrompt,
							onConfirm: () => { Ui.CloseWindow(); onExit(); },
							confirmText: ExitEditorConfirm,
							onCancel: () => { });
					}
					else
					{
						Ui.CloseWindow();
						onExit();
					}
				};
			}
		}

		void SetUpTextField(TextFieldWidget textField, string initialValue, Action<string> onTextEdited)
		{
			textField.Text = initialValue;
			textField.OnTextEdited = () => onTextEdited(textField.Text);

			textField.OnEscKey = _ => { textField.YieldKeyboardFocus(); return true; };
			textField.OnEnterKey = _ => { textField.YieldKeyboardFocus(); return true; };
			typableFields.Add(textField);
		}

		Widget CreateAssetTypesPanel()
		{
			var assetTypesPanel = Ui.LoadWidget("ASSET_TYPES_PANEL", null, new WidgetArgs());
			var assetTypeTemplate = assetTypesPanel.Get<CheckboxWidget>("ASSET_TYPE_TEMPLATE");

			foreach (var type in new[] { AssetType.Sprites, AssetType.Models, AssetType.Traits })
			{
				var assetType = (CheckboxWidget)assetTypeTemplate.Clone();
				var text = type.ToString();
				assetType.GetText = () => text;
				assetType.IsChecked = () => assetTypesToDisplay.HasFlag(type);
				assetType.IsVisible = () => true;
				assetType.OnClick = () =>
				{
					assetTypesToDisplay ^= type;
					UpdateEditorFields();
				};

				assetTypesPanel.AddChild(assetType);
			}

			return assetTypesPanel;
		}

		Widget SetEditorFieldsInner(Type fieldType, string fieldName, object initialValue, Action<object> setValue)
		{
			if (fieldType == typeof(int))
			{
				var template = intOptionTemplate.Clone();
				template.Get<LabelWidget>("LABEL").GetText = () => fieldName;

				SetUpTextField(template.Get<TextFieldWidget>("VALUE"),
					initialValue.ToString(),
					text =>
					{
						if (int.TryParse(text, out var result))
							setValue(result);
					});

				return template;
			}
			else if (fieldType == typeof(WVec))
			{
				var template = wVecOptionTemplate.Clone();
				template.Get<LabelWidget>("LABEL").GetText = () => fieldName;
				var val = (WVec)initialValue;

				SetUpTextField(template.Get<TextFieldWidget>("VALUEX"),
					val.X.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (int.TryParse(text, out var result))
						{
							val = new WVec(result, val.Y, val.Z);
							setValue(val);
						}
					});

				SetUpTextField(template.Get<TextFieldWidget>("VALUEY"),
					val.Y.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (int.TryParse(text, out var result))
						{
							val = new WVec(val.X, result, val.Z);
							setValue(val);
						}
					});

				SetUpTextField(template.Get<TextFieldWidget>("VALUEZ"),
					val.Z.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (int.TryParse(text, out var result))
						{
							val = new WVec(val.X, val.Y, result);
							setValue(val);
						}
					});

				return template;
			}
			else if (fieldType == typeof(float3))
			{
				var template = float3OptionTemplate.Clone();
				template.Get<LabelWidget>("LABEL").GetText = () => fieldName;
				var val = (float3)initialValue;

				SetUpTextField(template.Get<TextFieldWidget>("VALUEX"),
					val.X.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (float.TryParse(text, out var result))
						{
							val = new float3(result, val.Y, val.Z);
							setValue(val);
						}
					});

				SetUpTextField(template.Get<TextFieldWidget>("VALUEY"),
					val.Y.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (float.TryParse(text, out var result))
						{
							val = new float3(val.X, result, val.Z);
							setValue(val);
						}
					});

				SetUpTextField(template.Get<TextFieldWidget>("VALUEZ"),
					val.Z.ToString(CultureInfo.InvariantCulture),
					text =>
					{
						if (float.TryParse(text, out var result))
						{
							val = new float3(val.X, val.Y, result);
							setValue(val);
						}
					});

				return template;
			}

			return null;
		}

		Widget SetEditorFields(FieldInfo field, object obj, Action<string, object> editAction)
		{
			var attribute = field.GetCustomAttribute<AssetEditorAttribute>();
			if (attribute == null)
				return null;

			if (attribute.EditInsideMembers != null)
			{
				if (field.FieldType != typeof(float3))
					return null;

				List<(object Obj, FieldInfo Field)> fields = new();
				foreach (var member in attribute.EditInsideMembers)
				{
					if (obj.GetType()
						.GetField(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.GetValue(obj) is not IEnumerable collection)
						break;

					foreach (var item in collection)
					{
						if (item != null)
						{
							foreach (var innerField in item.GetType()
								.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
							{
								var innerAttribute = innerField.GetCustomAttribute<AssetEditorAttribute>();
								if (innerAttribute != null && innerField.FieldType == typeof(float3))
									fields.Add((item, innerField));
							}
						}
					}
				}

				if (fields.Count > 0)
				{
					var structVal = (float3)field.GetValue(obj);
					return SetEditorFieldsInner(typeof(float3), field.Name, structVal, val =>
					{
						var diff = (float3)val - structVal;
						structVal = (float3)val;
						foreach (var (o, f) in fields)
						{
							var fVal = f.GetValue(o);
							f.SetValue(o, (float3)fVal + diff);
						}

						field.SetValue(obj, val);
						editAction(field.Name, val);
					});
				}
			}
			else
				return SetEditorFieldsInner(field.FieldType, field.Name, field.GetValue(obj), val =>
				{
					field.SetValue(obj, val);
					editAction(field.Name, val);
				});

			return null;
		}

		Widget SetEditorTemplate(string name, ICollection<Widget> widgets)
		{
			if (widgets.Count > 0)
			{
				var template = optionTemplate.Clone();
				template.Get<LabelWidget>("TITLE").GetText = () => name;

				var height = 0;
				foreach (var w in widgets)
				{
					template.AddChild(w);
					if (height == 0)
						height = w.Bounds.Y + w.Bounds.Height;
					else
					{
						w.Bounds.Y = height;
						height += w.Bounds.Height;
						template.Bounds.Height += w.Bounds.Height;
					}
				}

				return template;
			}

			return null;
		}

		void UpdateEditorFields()
		{
			filteredEditorFields.Clear();
			foreach (var f in allEditorFields)
				if (assetTypesToDisplay.HasFlag(f.Type))
					filteredEditorFields.Add(f);

			editorContainer.RemoveChildren();
			foreach (var f in filteredEditorFields)
				editorContainer.AddChild(f.Widget);
		}

		void SetupSequenceWidgets()
		{
			// This can be an expensive opperation, so we only do it when the preview changes.
			if (previewCache == preview.Preview)
				return;

			previewCache = preview.Preview;

			allEditorFields.RemoveAll(f => f.Type == AssetType.Sprites);

			var usedSequences = new HashSet<string>();
			foreach (var p in previewCache)
			{
				if (p is SpriteActorPreview sap && sap.Animation.CurrentSequence != null)
				{
					var sequence = sap.Animation.CurrentSequence;
					var image = "undefined-sequence";
					var sequenceName = sequence.Name;
					if (sequence is DefaultSpriteSequence dss)
					{
						image = dss.Image;
						sequenceName = $"{dss.Image}.{sequenceName}";

						if (!usedSequences.Add(sequenceName))
							continue;
					}

					var widget = SetEditorTemplate(sequenceName, sequence.GetType()
						.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.Select(f => SetEditorFields(f, sequence, (field, value) => EditSequence(image, sequence.Name, field, value)))
						.Where(w => w != null)
						.ToList());

					if (widget != null)
						allEditorFields.Add(new AssetFieldSelector(AssetType.Sprites, widget));
				}
			}

			UpdateEditorFields();
		}

		void SetPreview(ActorSelectorActor a)
		{
			allEditorFields.Clear();
			initContainer.RemoveChildren();
			foreach (var f in typableFields)
				f.YieldKeyboardFocus();

			var actor = a.Actor;

			selectedActor = actor;
			var td = new TypeDictionary
			{
				new OwnerInit(selectedOwner.Name),
				new FactionInit(selectedOwner.Faction)
			};
			foreach (var api in actor.TraitInfos<IActorPreviewInitInfo>())
				foreach (var o in api.ActorPreviewInits(actor, ActorPreviewType.ColorPicker))
					td.Add(o);

			preview.SetPreview(actor, td);

			foreach (var editableProperties in a.Fields)
			{
				var trait = editableProperties.Key;
				var traitName = string.IsNullOrEmpty(trait.InstanceName)
					? trait.GetType().Name[..^4]
					: trait.GetType().Name[..^4] + '@' + trait.InstanceName;

				var widget = SetEditorTemplate(traitName,
					editableProperties.Value
					.Select(f => SetEditorFields(f, trait, (field, value) => EditActor(actor.Name, traitName, field, value)))
					.Where(w => w != null)
					.ToList());

				if (widget != null)
					allEditorFields.Add(new AssetFieldSelector(AssetType.Traits, widget));
			}

			// Add new children for inits
			var options = actor.TraitInfos<IEditorActorOptions>()
				.SelectMany(t => t.ActorOptions(actor, world))
				.OrderBy(o => o.DisplayOrder);

			foreach (var o in options)
			{
				if (o.DisplayMapEditorOnly)
					continue;

				if (o is EditorActorCheckbox co)
				{
					var checkboxContainer = checkboxOptionTemplate.Clone();
					var checkbox = checkboxContainer.Get<CheckboxWidget>("OPTION");
					checkbox.GetText = () => co.Name;

					checkbox.IsChecked = () => co.GetValue(preview);
					checkbox.OnClick = () =>
					{
						co.OnChange(preview, co.GetValue(preview) ^ true);
						SetupSequenceWidgets();
					};

					initContainer.AddChild(checkboxContainer);
				}
				else if (o is EditorActorSlider so)
				{
					var sliderContainer = sliderOptionTemplate.Clone();
					sliderContainer.Get<LabelWidget>("LABEL").GetText = () => so.Name;

					var slider = sliderContainer.Get<SliderWidget>("OPTION");
					slider.MinimumValue = so.MinValue;
					slider.MaximumValue = so.MaxValue;
					slider.Ticks = so.Ticks;

					so.OnChange(preview, so.GetValue(preview));
					slider.GetValue = () => so.GetValue(preview);
					slider.OnChange += value =>
					{
						so.OnChange(preview, value);
						SetupSequenceWidgets();
					};

					var valueField = sliderContainer.GetOrNull<TextFieldWidget>("VALUE");
					if (valueField != null)
					{
						void UpdateValueField(float f) => valueField.Text = ((int)f).ToString(NumberFormatInfo.CurrentInfo);
						UpdateValueField(so.GetValue(preview));
						slider.OnChange += UpdateValueField;

						valueField.OnTextEdited = () =>
						{
							if (float.TryParse(valueField.Text, out var result))
								slider.UpdateValue(result);
						};

						valueField.OnEscKey = _ => { valueField.YieldKeyboardFocus(); return true; };
						valueField.OnEnterKey = _ => { valueField.YieldKeyboardFocus(); return true; };
						typableFields.Add(valueField);
					}

					initContainer.AddChild(sliderContainer);
				}
				else if (o is EditorActorDropdown ddo)
				{
					var dropdownContainer = dropdownOptionTemplate.Clone();
					dropdownContainer.Get<LabelWidget>("LABEL").GetText = () => ddo.Name;

					var dropdown = dropdownContainer.Get<DropDownButtonWidget>("OPTION");
					ScrollItemWidget DropdownSetup(KeyValuePair<string, string> option, ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(template,
							() => ddo.GetValue(preview) == option.Key,
							() =>
							{
								ddo.OnChange(preview, option.Key);
								SetupSequenceWidgets();
							});

						item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
						return item;
					}

					dropdown.GetText = () => ddo.Labels[ddo.GetValue(preview)];
					dropdown.OnClick = () => dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, ddo.Labels, DropdownSetup);

					initContainer.AddChild(dropdownContainer);
				}
			}

			SetupSequenceWidgets();
		}

		public void Export()
		{
			var actorNodes = new List<MiniYamlNode>();
			var sequenceNodes = new List<MiniYamlNode>();
			foreach (var actor in actorEdits)
				actorNodes.Add(new MiniYamlNode(actor.Key, null,
					actor.Value.Select(t => new MiniYamlNode(t.Key,
						null, t.Value.Select(f => new MiniYamlNode(f.Key, FieldSaver.FormatValue(f.Value))).ToList())).ToList()));

			foreach (var actor in sequenceEdits)
				sequenceNodes.Add(new MiniYamlNode(actor.Key, null,
					actor.Value.Select(t => new MiniYamlNode(t.Key,
						null, t.Value.Select(f => new MiniYamlNode(f.Key, FieldSaver.FormatValue(f.Value))).ToList())).ToList()));

			if (actorNodes.Count != 0)
				actorNodes.WriteToFile(Path.Combine(Platform.SupportDir, "AssetEditorActors.yaml"));

			if (sequenceNodes.Count != 0)
				sequenceNodes.WriteToFile(Path.Combine(Platform.SupportDir, "AssetEditorSequences.yaml"));

			edited = false;
		}

		void EditActor(string name, string parentName, string fieldName, object value)
		{
			edited = true;
			edited = true;
			if (actorEdits.TryGetValue(name, out var actor))
			{
				if (actor.TryGetValue(parentName, out var fields))
					fields[fieldName] = value;
				else
					actor.Add(parentName, new Dictionary<string, object>() { { fieldName, value } });
			}
			else
				actorEdits.Add(name, new Dictionary<string, Dictionary<string, object>>
				{
					{
						parentName,
						new Dictionary<string, object>() { { fieldName, value } }
					}
				});
		}

		void EditSequence(string name, string parentName, string fieldName, object value)
		{
			edited = true;
			if (sequenceEdits.TryGetValue(name, out var actor))
			{
				if (actor.TryGetValue(parentName, out var fields))
					fields[fieldName] = value;
				else
					actor.Add(parentName, new Dictionary<string, object>() { { fieldName, value } });
			}
			else
				sequenceEdits.Add(name, new Dictionary<string, Dictionary<string, object>>
				{
					{
						parentName,
						new Dictionary<string, object>() { { fieldName, value } }
					}
				});
		}

		public static (ActorInfo Actor, Dictionary<TraitInfo, List<FieldInfo>> Traits) Clone(ActorInfo actor)
		{
			var clonedTraits = new List<TraitInfo>();
			foreach (var trait in actor.TraitInfos<TraitInfo>())
				clonedTraits.Add((TraitInfo)trait.GetType()
					.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(trait, null));

			return (new ActorInfo(actor.Name, clonedTraits.ToArray()), GetEditableFields(clonedTraits));
		}

		public static SequenceSet Clone(SequenceSet sequenceSet)
		{
			var fieldInfo = sequenceSet.GetType().GetField("images", BindingFlags.NonPublic | BindingFlags.Instance);
			var images = (IReadOnlyDictionary<string, IReadOnlyDictionary<string, ISpriteSequence>>)fieldInfo.GetValue(sequenceSet);
			var clonedImages = new Dictionary<string, IReadOnlyDictionary<string, ISpriteSequence>>();
			foreach (var outerEntry in images)
			{
				var outerKey = outerEntry.Key;
				var innerDictionary = outerEntry.Value;

				var clonedInnerDictionary = new Dictionary<string, ISpriteSequence>();
				foreach (var innerEntry in innerDictionary)
				{
					var spriteSequence = innerEntry.Value;
					var type = spriteSequence.GetType();
					var clonedSpriteSequence = (ISpriteSequence)type
						.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
						.Invoke(spriteSequence, null);

					// Deep clone necesary fields.
					foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
					{
						var attribute = field.GetCustomAttribute<AssetEditorAttribute>();
						if (attribute == null || attribute.EditInsideMembers == null)
							continue;

						var value = field.GetValue(spriteSequence);
						if (value == null)
							continue;

						if (value is Array arr)
						{
							var arrType = arr.GetType().GetElementType();
							var clonedArray = Array.CreateInstance(arrType, arr.Length);
							for (var i = 0; i < arr.Length; i++)
								clonedArray.SetValue(arrType.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
									.Invoke(arr.GetValue(i), null), i);

							value = clonedArray;
						}

						field.SetValue(clonedSpriteSequence, value);
					}

					clonedInnerDictionary.Add(innerEntry.Key, clonedSpriteSequence);
				}

				clonedImages.Add(outerKey, clonedInnerDictionary);
			}

			var clonedSequenceSet = (SequenceSet)sequenceSet.GetType()
				.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(sequenceSet, null);

			clonedSequenceSet.GetType()
				.GetField("images", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(clonedSequenceSet, clonedImages);

			return clonedSequenceSet;
		}

		public static Dictionary<TraitInfo, List<FieldInfo>> GetEditableFields(IEnumerable<TraitInfo> traitInfos)
		{
			var editableProperties = new Dictionary<TraitInfo, List<FieldInfo>>();
			foreach (var traitInfo in traitInfos)
			{
				var fields = traitInfo.GetType()
					.GetFields(BindingFlags.Public | BindingFlags.Instance);

				foreach (var field in fields)
				{
					if (field.GetCustomAttributes(typeof(AssetEditorAttribute), true).Length > 0)
					{
						if (editableProperties.TryGetValue(traitInfo, out var list))
							list.Add(field);
						else
							editableProperties.Add(traitInfo, new List<FieldInfo> { field });
					}
				}
			}

			editableProperties.TrimExcess();
			return editableProperties;
		}

		void InitializeActorList()
		{
			actorList.RemoveChildren();

			foreach (var a in filteredActors)
			{
				var actor = a.Actor;
				var item = ScrollItemWidget.Setup(template,
					() => actor == selectedActor,
					() => SetPreview(a));

				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				WidgetUtils.TruncateLabelToTooltip(label, a.Name);
				item.IsVisible = () => true;
				actorList.AddChild(item);
			}

			if (filteredActors.Count > 0 && (selectedActor == null || !filteredActors.Any(a => a.Actor == selectedActor)))
				SetPreview(filteredActors[0]);
		}
	}
}
