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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorEditLogic : ChromeLogic
	{
		[FluentReference]
		const string DuplicateActorId = "label-duplicate-actor-id";

		[FluentReference]
		const string EnterActorId = "label-actor-id";

		[FluentReference]
		const string Owner = "label-actor-owner";

		// Error states define overlapping bits to simplify panel reflow logic
		[Flags]
		enum ActorIDStatus { Normal = 0, Duplicate = 1, Empty = 3 }

		readonly WorldRenderer worldRenderer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editor;
		readonly Widget actorEditPanel;
		readonly LabelWidget typeLabel;
		readonly TextFieldWidget actorIDField;
		readonly HashSet<TextFieldWidget> typableFields = [];
		readonly LabelWidget actorIDErrorLabel;

		readonly Widget initContainer;
		readonly Widget buttonContainer;

		readonly Widget checkboxOptionTemplate;
		readonly Widget sliderOptionTemplate;
		readonly Widget dropdownOptionTemplate;

		ActorIDStatus actorIDStatus = ActorIDStatus.Normal;
		ActorIDStatus nextActorIDStatus = ActorIDStatus.Normal;
		string initialActorID;

		EditActorPreview editActorPreview;

		EditorActorPreview SelectedActor => editor.DefaultBrush.Selection.Actor;

		internal bool IsChangingSelection { get; set; }

		[ObjectCreator.UseCtor]
		public ActorEditLogic(Widget widget, World world, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			this.worldRenderer = worldRenderer;

			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editor = widget.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			editor.DefaultBrush.SelectionChanged += HandleSelectionChanged;

			var selectTabContainer = widget.Parent.Parent.Get("SELECT_WIDGETS");
			actorEditPanel = selectTabContainer.Get("ACTOR_EDIT_PANEL");

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
			actorIDErrorLabel.GetText = () =>
				actorIDStatus == ActorIDStatus.Duplicate || nextActorIDStatus == ActorIDStatus.Duplicate
					? FluentProvider.GetMessage(DuplicateActorId)
					: FluentProvider.GetMessage(EnterActorId);

			okButton.IsDisabled = () => !IsValid() || editActorPreview == null || !editActorPreview.IsDirty;
			okButton.OnClick = Save;
			cancelButton.OnClick = Cancel;
			deleteButton.OnClick = Delete;
			actorEditPanel.IsVisible = () => editor.CurrentBrush == editor.DefaultBrush && SelectedActor != null;

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
				if (!SelectedActor.ID.Equals(actorId, StringComparison.OrdinalIgnoreCase) && editorActorLayer[actorId] != null)
				{
					nextActorIDStatus = ActorIDStatus.Duplicate;
					actorIDErrorLabel.Visible = true;
					return;
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

		protected override void Dispose(bool disposing)
		{
			editor.DefaultBrush.SelectionChanged -= HandleSelectionChanged;

			base.Dispose(disposing);
		}

		void HandleSelectionChanged()
		{
			if (SelectedActor != null)
			{
				// Our edit control is updating the selection to account for an actor ID change.
				// Don't try and reset, we're the ones who instigated the selection change!
				if (!IsChangingSelection)
					Reset();

				editActorPreview = new EditActorPreview(this, editor, editorActorLayer, SelectedActor);

				initialActorID = actorIDField.Text = SelectedActor.ID;

				var font = Game.Renderer.Fonts[typeLabel.Font];
				var truncatedType = WidgetUtils.TruncateText(FluentProvider.GetMessage(SelectedActor.DescriptiveName), typeLabel.Bounds.Width, font);
				typeLabel.GetText = () => truncatedType;

				actorIDField.CursorPosition = SelectedActor.ID.Length;
				nextActorIDStatus = ActorIDStatus.Normal;

				// Remove old widgets
				var oldInitHeight = initContainer.Bounds.Height;
				initContainer.Bounds.Height = 0;
				initContainer.RemoveChildren();

				// Add owner dropdown
				var ownerContainer = dropdownOptionTemplate.Clone();
				var owner = FluentProvider.GetMessage(Owner);
				ownerContainer.Get<LabelWidget>("LABEL").GetText = () => owner;
				var ownerDropdown = ownerContainer.Get<DropDownButtonWidget>("OPTION");
				var selectedOwner = SelectedActor.Owner;

				void UpdateOwner(EditorActorPreview preview, PlayerReference reference)
				{
					preview.Owner = reference;
					preview.ReplaceInit(new OwnerInit(reference.Name));
				}

				var ownerHandler = new EditorActorOptionActionHandle<PlayerReference>(UpdateOwner, SelectedActor.Owner);
				editActorPreview.Add(ownerHandler);

				ScrollItemWidget SetupItem(PlayerReference option, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
					{
						selectedOwner = option;
						UpdateOwner(SelectedActor, selectedOwner);
						ownerHandler.OnChange(option);
					});

					item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
					item.GetColor = () => option.Color;
					return item;
				}

				ownerDropdown.GetText = () => selectedOwner.Name;
				ownerDropdown.GetColor = () => selectedOwner.Color;
				ownerDropdown.OnClick = () =>
				{
					var owners = editorActorLayer.Players.Players.Values.OrderBy(p => p.Name);
					ownerDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, SetupItem);
				};

				initContainer.Bounds.Height += ownerContainer.Bounds.Height;
				initContainer.AddChild(ownerContainer);

				// Add new children for inits
				var options = SelectedActor.Info.TraitInfos<IEditorActorOptions>()
					.SelectMany(t => t.ActorOptions(SelectedActor.Info, worldRenderer.World))
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

						var editorActionHandle = new EditorActorOptionActionHandle<bool>(co.OnChange, co.GetValue(SelectedActor));
						editActorPreview.Add(editorActionHandle);

						checkbox.IsChecked = () => co.GetValue(SelectedActor);
						checkbox.OnClick = () =>
						{
							var newValue = co.GetValue(SelectedActor) ^ true;
							co.OnChange(SelectedActor, newValue);
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

						var editorActionHandle = new EditorActorOptionActionHandle<float>(so.OnChange, so.GetValue(SelectedActor));
						editActorPreview.Add(editorActionHandle);

						slider.GetValue = () => so.GetValue(SelectedActor);
						slider.OnChange += value => so.OnChange(SelectedActor, value);
						slider.OnChange += editorActionHandle.OnChange;

						var valueField = sliderContainer.GetOrNull<TextFieldWidget>("VALUE");
						if (valueField != null)
						{
							void UpdateValueField(float f) => valueField.Text = ((int)f).ToString(NumberFormatInfo.CurrentInfo);
							UpdateValueField(so.GetValue(SelectedActor));
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
						dropdownContainer.Bounds.Y = initContainer.Bounds.Height;
						initContainer.Bounds.Height += dropdownContainer.Bounds.Height;
						dropdownContainer.Get<LabelWidget>("LABEL").GetText = () => ddo.Name;

						var labels = ddo.GetLabels(SelectedActor);

						var editorActionHandle = new EditorActorOptionActionHandle<string>(ddo.OnChange, ddo.GetValue(SelectedActor, labels));
						editActorPreview.Add(editorActionHandle);

						var dropdown = dropdownContainer.Get<DropDownButtonWidget>("OPTION");
						ScrollItemWidget DropdownSetup(KeyValuePair<string, string> option, ScrollItemWidget template)
						{
							var item = ScrollItemWidget.Setup(template,
								() => ddo.GetValue(SelectedActor, labels) == option.Key,
								() =>
								{
									ddo.OnChange(SelectedActor, option.Key);
									editorActionHandle.OnChange(option.Key);
								});

							item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
							return item;
						}

						dropdown.GetText = () => labels[ddo.GetValue(SelectedActor, labels)];
						dropdown.OnClick = () => dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, labels, DropdownSetup);

						initContainer.AddChild(dropdownContainer);
					}
				}

				buttonContainer.Bounds.Y += initContainer.Bounds.Height - oldInitHeight;
			}
			else
			{
				// Selected actor is null, hide the border and edit panel.
				Close();
			}
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

					initContainer.Bounds.Y += offset;
					buttonContainer.Bounds.Y += offset;
				}

				actorIDStatus = nextActorIDStatus;
			}
		}

		void Delete()
		{
			YieldFocus();

			if (SelectedActor != null)
				editorActionManager.Add(new RemoveSelectedActorAction(
					editor.DefaultBrush,
					editorActorLayer,
					SelectedActor));
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

		void YieldFocus()
		{
			actorIDField.YieldKeyboardFocus();
			foreach (var f in typableFields)
				f.YieldKeyboardFocus();
		}

		void Close()
		{
			YieldFocus();

			if (SelectedActor != null)
			{
				editor.DefaultBrush.ClearSelection(updateSelectedTab: true);
			}
		}

		void Save()
		{
			editorActionManager.Add(new EditActorEditorAction(SelectedActor, editActorPreview.GetDirtyHandles()));
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

		public void Do(ref EditorActorPreview actor)
		{
			change(actor, value);
		}

		public void Undo(ref EditorActorPreview actor)
		{
			change(actor, initialValue);
		}

		public bool IsDirty { get; private set; }
		public bool ShouldDoOnSave => false;
	}

	public interface IEditActorHandle
	{
		void Do(ref EditorActorPreview actor);
		void Undo(ref EditorActorPreview actor);
		bool IsDirty { get; }
		bool ShouldDoOnSave { get; }
	}

	sealed class EditActorEditorAction : IEditorAction
	{
		[FluentReference("name", "id")]
		const string EditedActor = "notification-edited-actor";

		[FluentReference("name", "old-id", "new-id")]
		const string EditedActorId = "notification-edited-actor-id";

		public string Text { get; private set; }
		public EditorActorPreview Actor;

		readonly IEnumerable<IEditActorHandle> handles;

		public EditActorEditorAction(EditorActorPreview actor, IEnumerable<IEditActorHandle> handles)
		{
			Actor = actor;
			this.handles = handles;
			Text = FluentProvider.GetMessage(EditedActor, "name", actor.Info.Name, "id", actor.ID);
		}

		public void Execute()
		{
			var before = Actor;

			foreach (var editorActionHandle in handles.Where(h => h.ShouldDoOnSave))
				editorActionHandle.Do(ref Actor);

			var after = Actor;
			if (before != after)
				Text = FluentProvider.GetMessage(EditedActorId, "name", after.Info.Name, "old-id", before.ID, "new-id", after.ID);
		}

		public void Do()
		{
			foreach (var editorActionHandle in handles)
				editorActionHandle.Do(ref Actor);
		}

		public void Undo()
		{
			foreach (var editorActionHandle in handles)
				editorActionHandle.Undo(ref Actor);
		}
	}

	sealed class EditActorPreview
	{
		readonly SetActorIdAction setActorIdAction;
		readonly List<IEditActorHandle> handles = [];
		EditorActorPreview actor;

		public EditActorPreview(ActorEditLogic logic, EditorViewportControllerWidget editor, EditorActorLayer editorActorLayer, EditorActorPreview actor)
		{
			this.actor = actor;
			setActorIdAction = new SetActorIdAction(logic, editor, editorActorLayer, actor.ID);
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
				handle.Undo(ref actor);
		}
	}

	public class SetActorIdAction : IEditActorHandle
	{
		readonly ActorEditLogic logic;
		readonly EditorViewportControllerWidget editor;
		readonly EditorActorLayer editorActorLayer;
		readonly string initial;
		string newID;

		public void Set(string actorId)
		{
			IsDirty = initial != actorId;
			newID = actorId;
		}

		public SetActorIdAction(ActorEditLogic logic, EditorViewportControllerWidget editor, EditorActorLayer editorActorLayer, string initial)
		{
			this.logic = logic;
			this.editor = editor;
			this.editorActorLayer = editorActorLayer;
			this.initial = initial;
		}

		public void Do(ref EditorActorPreview actor)
		{
			// We can't update the ID of an EditorActorPreview in place - it's the hash and equality key of a preview.
			// So instead we need to swap in an entirely new preview with the updated ID.
			// This affects the actor layer, and the current selection.
			editorActorLayer.Remove(actor);
			actor = actor.WithId(newID);
			editorActorLayer.Add(actor);
			logic.IsChangingSelection = true;
			editor.DefaultBrush.SetSelection(new EditorSelection { Actor = actor });
			logic.IsChangingSelection = false;
		}

		public void Undo(ref EditorActorPreview actor)
		{
			editorActorLayer.Remove(actor);
			actor = actor.WithId(initial);
			editorActorLayer.Add(actor);
			logic.IsChangingSelection = true;
			editor.DefaultBrush.SetSelection(new EditorSelection { Actor = actor });
			logic.IsChangingSelection = false;
		}

		public bool IsDirty { get; private set; }
		public bool ShouldDoOnSave => true;
	}
}
