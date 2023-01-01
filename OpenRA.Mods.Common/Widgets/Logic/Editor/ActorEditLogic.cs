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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorEditLogic : ChromeLogic
	{
		[TranslationReference]
		const string DuplicateActorId = "label-duplicate-actor-id";

		[TranslationReference]
		const string EnterActorId = "label-actor-id";

		[TranslationReference]
		const string Owner = "label-actor-owner";

		// Error states define overlapping bits to simplify panel reflow logic
		[Flags]
		enum ActorIDStatus { Normal = 0, Duplicate = 1, Empty = 3 }

		readonly WorldRenderer worldRenderer;
		readonly ModData modData;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editor;
		readonly BackgroundWidget actorEditPanel;
		readonly LabelWidget typeLabel;
		readonly TextFieldWidget actorIDField;
		readonly HashSet<TextFieldWidget> typableFields = new HashSet<TextFieldWidget>();
		readonly LabelWidget actorIDErrorLabel;

		readonly Widget initContainer;
		readonly Widget buttonContainer;

		readonly Widget checkboxOptionTemplate;
		readonly Widget sliderOptionTemplate;
		readonly Widget dropdownOptionTemplate;

		readonly int editPanelPadding; // Padding between right edge of actor and the edit panel.
		readonly long scrollVisibleTimeout = 100; // Delay after scrolling map before edit widget becomes visible again.

		long lastScrollTime = 0;
		int2 lastScrollPosition = int2.Zero;

		ActorIDStatus actorIDStatus = ActorIDStatus.Normal;
		ActorIDStatus nextActorIDStatus = ActorIDStatus.Normal;
		string initialActorID;

		EditorActorPreview currentActorInner;
		EditActorPreview editActorPreview;

		EditorActorPreview CurrentActor
		{
			get => currentActorInner;

			set
			{
				if (currentActorInner == value)
					return;

				if (currentActorInner != null)
				{
					Reset();
					currentActorInner.Selected = false;
				}

				currentActorInner = value;
				if (currentActorInner != null)
					currentActorInner.Selected = true;
			}
		}

		[ObjectCreator.UseCtor]
		public ActorEditLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			this.modData = modData;
			this.worldRenderer = worldRenderer;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			actorEditPanel = editor.Get<BackgroundWidget>("ACTOR_EDIT_PANEL");

			typeLabel = actorEditPanel.Get<LabelWidget>("ACTOR_TYPE_LABEL");
			actorIDField = actorEditPanel.Get<TextFieldWidget>("ACTOR_ID");

			initContainer = actorEditPanel.Get("ACTOR_INIT_CONTAINER");
			buttonContainer = actorEditPanel.Get("BUTTON_CONTAINER");

			checkboxOptionTemplate = initContainer.Get("CHECKBOX_OPTION_TEMPLATE");
			sliderOptionTemplate = initContainer.Get("SLIDER_OPTION_TEMPLATE");
			dropdownOptionTemplate = initContainer.Get("DROPDOWN_OPTION_TEMPLATE");
			initContainer.RemoveChildren();

			var deleteButton = actorEditPanel.Get<ButtonWidget>("DELETE_BUTTON");
			var cancelButton = actorEditPanel.Get<ButtonWidget>("CANCEL_BUTTON");
			var okButton = actorEditPanel.Get<ButtonWidget>("OK_BUTTON");

			actorIDErrorLabel = actorEditPanel.Get<LabelWidget>("ACTOR_ID_ERROR_LABEL");
			actorIDErrorLabel.IsVisible = () => actorIDStatus != ActorIDStatus.Normal;
			actorIDErrorLabel.GetText = () => actorIDStatus == ActorIDStatus.Duplicate ?
				modData.Translation.GetString(DuplicateActorId)
					: modData.Translation.GetString(EnterActorId);

			if (logicArgs.TryGetValue("EditPanelPadding", out var yaml))
				editPanelPadding = FieldLoader.GetValue<int>("EditPanelPadding", yaml.Value);

			okButton.IsDisabled = () => !IsValid() || !editActorPreview.IsDirty;
			okButton.OnClick = Save;
			cancelButton.OnClick = Cancel;
			deleteButton.OnClick = Delete;
			actorEditPanel.IsVisible = () => CurrentActor != null
				&& editor.CurrentBrush == editor.DefaultBrush
				&& Game.RunTime > lastScrollTime + scrollVisibleTimeout;

			actorIDField.OnEscKey = _ => actorIDField.YieldKeyboardFocus();

			actorIDField.OnTextEdited = () =>
			{
				var actorId = actorIDField.Text.Trim();
				if (string.IsNullOrWhiteSpace(actorId))
				{
					nextActorIDStatus = ActorIDStatus.Empty;
					return;
				}

				// Check for duplicate actor ID
				if (!CurrentActor.ID.Equals(actorId, StringComparison.OrdinalIgnoreCase))
				{
					if (editorActorLayer[actorId] != null)
					{
						nextActorIDStatus = ActorIDStatus.Duplicate;
						actorIDErrorLabel.Text = modData.Translation.GetString(DuplicateActorId);
						actorIDErrorLabel.Visible = true;
						return;
					}
				}

				SetActorID(actorId);
				nextActorIDStatus = ActorIDStatus.Normal;
			};

			actorIDField.OnLoseFocus = () =>
			{
				// Reset invalid IDs back to their starting value
				if (actorIDStatus != ActorIDStatus.Normal)
					SetActorID(initialActorID);
			};
		}

		void SetActorID(string actorId)
		{
			editActorPreview.SetActorID(actorId);

			nextActorIDStatus = ActorIDStatus.Normal;
		}

		bool IsValid()
		{
			return nextActorIDStatus == ActorIDStatus.Normal;
		}

		public override void Tick()
		{
			if (actorIDStatus != nextActorIDStatus)
			{
				if ((actorIDStatus & nextActorIDStatus) == 0)
				{
					var offset = actorIDErrorLabel.Bounds.Height;
					if (nextActorIDStatus == ActorIDStatus.Normal)
						offset *= -1;

					actorEditPanel.Bounds.Height += offset;
					initContainer.Bounds.Y += offset;
					buttonContainer.Bounds.Y += offset;
				}

				actorIDStatus = nextActorIDStatus;
			}

			var actor = editor.DefaultBrush.SelectedActor;
			if (actor != null)
			{
				var origin = worldRenderer.Viewport.WorldToViewPx(new int2(actor.Bounds.Right, actor.Bounds.Top));

				// If we scrolled, hide the edit box for a moment
				if (lastScrollPosition.X != origin.X || lastScrollPosition.Y != origin.Y)
				{
					lastScrollTime = Game.RunTime;
					lastScrollPosition = origin;
				}

				// If we changed actor, move widgets
				if (CurrentActor != actor)
				{
					lastScrollTime = 0; // Ensure visible
					CurrentActor = actor;

					editActorPreview = new EditActorPreview(CurrentActor);

					initialActorID = actorIDField.Text = actor.ID;

					var font = Game.Renderer.Fonts[typeLabel.Font];
					var truncatedType = WidgetUtils.TruncateText(actor.DescriptiveName, typeLabel.Bounds.Width, font);
					typeLabel.Text = truncatedType;

					actorIDField.CursorPosition = actor.ID.Length;
					nextActorIDStatus = ActorIDStatus.Normal;

					// Remove old widgets
					var oldInitHeight = initContainer.Bounds.Height;
					initContainer.Bounds.Height = 0;
					initContainer.RemoveChildren();

					// Add owner dropdown
					var ownerContainer = dropdownOptionTemplate.Clone();
					var owner = modData.Translation.GetString(Owner);
					ownerContainer.Get<LabelWidget>("LABEL").GetText = () => owner;
					var ownerDropdown = ownerContainer.Get<DropDownButtonWidget>("OPTION");
					var selectedOwner = actor.Owner;

					Action<EditorActorPreview, PlayerReference> updateOwner = (preview, reference) =>
					{
						preview.Owner = reference;
						preview.ReplaceInit(new OwnerInit(reference.Name));
					};

					var ownerHandler = new EditorActorOptionActionHandle<PlayerReference>(updateOwner, actor.Owner);
					editActorPreview.Add(ownerHandler);

					Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
						{
							selectedOwner = option;
							updateOwner(CurrentActor, selectedOwner);
							ownerHandler.OnChange(option);
						});

						item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
						item.GetColor = () => option.Color;
						return item;
					};

					ownerDropdown.GetText = () => selectedOwner.Name;
					ownerDropdown.GetColor = () => selectedOwner.Color;
					ownerDropdown.OnClick = () =>
					{
						var owners = editorActorLayer.Players.Players.Values.OrderBy(p => p.Name);
						ownerDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, setupItem);
					};

					initContainer.Bounds.Height += ownerContainer.Bounds.Height;
					initContainer.AddChild(ownerContainer);

					// Add new children for inits
					var options = actor.Info.TraitInfos<IEditorActorOptions>()
						.SelectMany(t => t.ActorOptions(actor.Info, worldRenderer.World))
						.OrderBy(o => o.DisplayOrder);

					foreach (var o in options)
					{
						if (o is EditorActorCheckbox co)
						{
							var checkboxContainer = checkboxOptionTemplate.Clone();
							checkboxContainer.Bounds.Y = initContainer.Bounds.Height;
							initContainer.Bounds.Height += checkboxContainer.Bounds.Height;

							var checkbox = checkboxContainer.Get<CheckboxWidget>("OPTION");
							checkbox.GetText = () => co.Name;

							var editorActionHandle = new EditorActorOptionActionHandle<bool>(co.OnChange, co.GetValue(actor));
							editActorPreview.Add(editorActionHandle);

							checkbox.IsChecked = () => co.GetValue(actor);
							checkbox.OnClick = () =>
							{
								var newValue = co.GetValue(actor) ^ true;
								co.OnChange(actor, newValue);
								editorActionHandle.OnChange(newValue);
							};

							initContainer.AddChild(checkboxContainer);
						}
						else if (o is EditorActorSlider so)
						{
							var sliderContainer = sliderOptionTemplate.Clone();
							sliderContainer.Bounds.Y = initContainer.Bounds.Height;
							initContainer.Bounds.Height += sliderContainer.Bounds.Height;
							sliderContainer.Get<LabelWidget>("LABEL").GetText = () => so.Name;

							var slider = sliderContainer.Get<SliderWidget>("OPTION");
							slider.MinimumValue = so.MinValue;
							slider.MaximumValue = so.MaxValue;
							slider.Ticks = so.Ticks;

							var editorActionHandle = new EditorActorOptionActionHandle<float>(so.OnChange, so.GetValue(actor));
							editActorPreview.Add(editorActionHandle);

							slider.GetValue = () => so.GetValue(actor);
							slider.OnChange += value => so.OnChange(actor, value);
							slider.OnChange += value => editorActionHandle.OnChange(value);

							var valueField = sliderContainer.GetOrNull<TextFieldWidget>("VALUE");
							if (valueField != null)
							{
								Action<float> updateValueField = f => valueField.Text = ((int)f).ToString();
								updateValueField(so.GetValue(actor));
								slider.OnChange += updateValueField;

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
							dropdownContainer.Bounds.Y = initContainer.Bounds.Height;
							initContainer.Bounds.Height += dropdownContainer.Bounds.Height;
							dropdownContainer.Get<LabelWidget>("LABEL").GetText = () => ddo.Name;

							var editorActionHandle = new EditorActorOptionActionHandle<string>(ddo.OnChange, ddo.GetValue(actor));
							editActorPreview.Add(editorActionHandle);

							var dropdown = dropdownContainer.Get<DropDownButtonWidget>("OPTION");
							Func<KeyValuePair<string, string>, ScrollItemWidget, ScrollItemWidget> dropdownSetup = (option, template) =>
							{
								var item = ScrollItemWidget.Setup(template,
									() => ddo.GetValue(actor) == option.Key,
									() =>
									{
										ddo.OnChange(actor, option.Key);
										editorActionHandle.OnChange(option.Key);
									});

								item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
								return item;
							};

							dropdown.GetText = () => ddo.Labels[ddo.GetValue(actor)];
							dropdown.OnClick = () => dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, ddo.Labels, dropdownSetup);

							initContainer.AddChild(dropdownContainer);
						}
					}

					actorEditPanel.Bounds.Height += initContainer.Bounds.Height - oldInitHeight;
					buttonContainer.Bounds.Y += initContainer.Bounds.Height - oldInitHeight;
				}

				// Set the edit panel to the right of the selection border.
				actorEditPanel.Bounds.X = origin.X + editPanelPadding;
				actorEditPanel.Bounds.Y = origin.Y;
			}
			else if (CurrentActor != null)
			{
				// Selected actor is null, hide the border and edit panel.
				Close();
			}
		}

		void Delete()
		{
			if (CurrentActor != null)
				editorActionManager.Add(new RemoveActorAction(editorActorLayer, CurrentActor));

			Close();
		}

		void Cancel()
		{
			Reset();
			Close();
		}

		void Reset()
		{
			editActorPreview?.Reset();
		}

		void Close()
		{
			actorIDField.YieldKeyboardFocus();
			foreach (var f in typableFields)
				f.YieldKeyboardFocus();

			editor.DefaultBrush.SelectedActor = null;
			CurrentActor = null;
		}

		void Save()
		{
			editorActionManager.Add(new EditActorEditorAction(editorActorLayer, CurrentActor, editActorPreview.GetDirtyHandles()));
			editActorPreview = null;
			Close();
		}
	}

	public class EditorActorOptionActionHandle<T> : IEditActorHandle
	{
		readonly Action<EditorActorPreview, T> change;
		T value;
		readonly T initialValue;

		public EditorActorOptionActionHandle(Action<EditorActorPreview, T> change, T value)
		{
			this.change = change;
			this.value = value;
			initialValue = value;
		}

		public void OnChange(T value)
		{
			IsDirty = !EqualityComparer<T>.Default.Equals(initialValue, value);

			this.value = value;
		}

		public void Do(EditorActorPreview actor)
		{
			change(actor, value);
		}

		public void Undo(EditorActorPreview actor)
		{
			change(actor, initialValue);
		}

		public bool IsDirty { get; private set; }
		public bool ShouldDoOnSave => false;
	}

	public interface IEditActorHandle
	{
		void Do(EditorActorPreview actor);
		void Undo(EditorActorPreview actor);
		bool IsDirty { get; }
		bool ShouldDoOnSave { get; }
	}

	class EditActorEditorAction : IEditorAction
	{
		public string Text { get; }

		readonly IEnumerable<IEditActorHandle> handles;
		readonly EditorActorLayer editorActorLayer;
		EditorActorPreview actor;
		readonly string actorId;

		public EditActorEditorAction(EditorActorLayer editorActorLayer, EditorActorPreview actor, IEnumerable<IEditActorHandle> handles)
		{
			this.editorActorLayer = editorActorLayer;
			actorId = actor.ID;
			this.actor = actor;
			this.handles = handles;
			Text = $"Edited {actor.Info.Name} ({actor.ID})";
		}

		public void Execute()
		{
			foreach (var editorActionHandle in handles.Where(h => h.ShouldDoOnSave))
				editorActionHandle.Do(actor);
		}

		public void Do()
		{
			actor = editorActorLayer[actorId.ToLowerInvariant()];
			foreach (var editorActionHandle in handles)
				editorActionHandle.Do(actor);
		}

		public void Undo()
		{
			foreach (var editorActionHandle in handles)
				editorActionHandle.Undo(actor);
		}
	}

	class EditActorPreview
	{
		readonly EditorActorPreview actor;
		readonly SetActorIdAction setActorIdAction;
		readonly List<IEditActorHandle> handles = new List<IEditActorHandle>();

		public EditActorPreview(EditorActorPreview actor)
		{
			this.actor = actor;
			setActorIdAction = new SetActorIdAction(actor.ID);
			handles.Add(setActorIdAction);
		}

		public bool IsDirty
		{
			get { return handles.Any(h => h.IsDirty); }
		}

		public void SetActorID(string actorID)
		{
			setActorIdAction.Set(actorID);
		}

		public void Add(IEditActorHandle editActor)
		{
			handles.Add(editActor);
		}

		public IEnumerable<IEditActorHandle> GetDirtyHandles()
		{
			return handles.Where(h => h.IsDirty);
		}

		public void Reset()
		{
			foreach (var handle in handles.Where(h => h.IsDirty))
				handle.Undo(actor);
		}
	}

	public class SetActorIdAction : IEditActorHandle
	{
		readonly string initial;
		string newID;

		public void Set(string actorId)
		{
			IsDirty = initial != actorId;
			newID = actorId;
		}

		public SetActorIdAction(string initial)
		{
			this.initial = initial;
		}

		public void Do(EditorActorPreview actor)
		{
			actor.ID = newID;
		}

		public void Undo(EditorActorPreview actor)
		{
			actor.ID = initial;
		}

		public bool IsDirty { get; private set; }
		public bool ShouldDoOnSave => true;
	}
}
