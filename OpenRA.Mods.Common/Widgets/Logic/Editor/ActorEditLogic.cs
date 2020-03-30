#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		// Error states define overlapping bits to simplify panel reflow logic
		[Flags]
		enum ActorIDStatus { Normal = 0, Duplicate = 1, Empty = 3 }

		readonly WorldRenderer worldRenderer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorViewportControllerWidget editor;
		readonly BackgroundWidget actorEditPanel;
		readonly LabelWidget typeLabel;
		readonly TextFieldWidget actorIDField;
		readonly LabelWidget actorIDErrorLabel;

		readonly Widget initContainer;
		readonly Widget buttonContainer;

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
		EditorActorPreview CurrentActor
		{
			get
			{
				return currentActorInner;
			}

			set
			{
				if (currentActorInner == value)
					return;

				if (currentActorInner != null)
					currentActorInner.Selected = false;

				currentActorInner = value;
				if (currentActorInner != null)
					currentActorInner.Selected = true;
			}
		}

		[ObjectCreator.UseCtor]
		public ActorEditLogic(Widget widget, World world, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			this.worldRenderer = worldRenderer;
			editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editor = widget.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			actorEditPanel = editor.Get<BackgroundWidget>("ACTOR_EDIT_PANEL");

			typeLabel = actorEditPanel.Get<LabelWidget>("ACTOR_TYPE_LABEL");
			actorIDField = actorEditPanel.Get<TextFieldWidget>("ACTOR_ID");

			initContainer = actorEditPanel.Get("ACTOR_INIT_CONTAINER");
			buttonContainer = actorEditPanel.Get("BUTTON_CONTAINER");

			sliderOptionTemplate = initContainer.Get("SLIDER_OPTION_TEMPLATE");
			dropdownOptionTemplate = initContainer.Get("DROPDOWN_OPTION_TEMPLATE");
			initContainer.RemoveChildren();

			var deleteButton = actorEditPanel.Get<ButtonWidget>("DELETE_BUTTON");
			var closeButton = actorEditPanel.Get<ButtonWidget>("CLOSE_BUTTON");

			actorIDErrorLabel = actorEditPanel.Get<LabelWidget>("ACTOR_ID_ERROR_LABEL");
			actorIDErrorLabel.IsVisible = () => actorIDStatus != ActorIDStatus.Normal;
			actorIDErrorLabel.GetText = () => actorIDStatus == ActorIDStatus.Duplicate ?
				"Duplicate Actor ID" : "Enter an Actor ID";

			MiniYaml yaml;
			if (logicArgs.TryGetValue("EditPanelPadding", out yaml))
				editPanelPadding = FieldLoader.GetValue<int>("EditPanelPadding", yaml.Value);

			closeButton.OnClick = Close;
			deleteButton.OnClick = Delete;
			actorEditPanel.IsVisible = () => CurrentActor != null
				&& editor.CurrentBrush == editor.DefaultBrush
				&& Game.RunTime > lastScrollTime + scrollVisibleTimeout;

			actorIDField.OnEscKey = () =>
			{
				actorIDField.YieldKeyboardFocus();
				return true;
			};

			actorIDField.OnTextEdited = () =>
			{
				if (string.IsNullOrWhiteSpace(actorIDField.Text))
				{
					nextActorIDStatus = ActorIDStatus.Empty;
					return;
				}

				// Check for duplicate actor ID
				var actorId = actorIDField.Text.ToLowerInvariant();
				if (CurrentActor.ID.ToLowerInvariant() != actorId)
				{
					if (editorActorLayer[actorId] != null)
					{
						nextActorIDStatus = ActorIDStatus.Duplicate;
						return;
					}
				}

				SetActorID(world, actorId);
			};

			actorIDField.OnLoseFocus = () =>
			{
				// Reset invalid IDs back to their starting value
				if (actorIDStatus != ActorIDStatus.Normal)
					SetActorID(world, initialActorID);
			};
		}

		void SetActorID(World world, string actorId)
		{
			CurrentActor.ID = actorId;
			nextActorIDStatus = ActorIDStatus.Normal;
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
					ownerContainer.Get<LabelWidget>("LABEL").GetText = () => "Owner";
					var ownerDropdown = ownerContainer.Get<DropDownButtonWidget>("OPTION");
					var selectedOwner = actor.Owner;

					Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
						{
							selectedOwner = option;
							CurrentActor.Owner = selectedOwner;
							CurrentActor.ReplaceInit(new OwnerInit(selectedOwner.Name));
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
						if (o is EditorActorSlider)
						{
							var so = (EditorActorSlider)o;
							var sliderContainer = sliderOptionTemplate.Clone();
							sliderContainer.Bounds.Y = initContainer.Bounds.Height;
							initContainer.Bounds.Height += sliderContainer.Bounds.Height;
							sliderContainer.Get<LabelWidget>("LABEL").GetText = () => so.Name;

							var slider = sliderContainer.Get<SliderWidget>("OPTION");
							slider.MinimumValue = so.MinValue;
							slider.MaximumValue = so.MaxValue;
							slider.Ticks = so.Ticks;

							slider.GetValue = () => so.GetValue(actor);
							slider.OnChange += value => so.OnChange(actor, value);

							initContainer.AddChild(sliderContainer);
						}
						else if (o is EditorActorDropdown)
						{
							var ddo = (EditorActorDropdown)o;
							var dropdownContainer = dropdownOptionTemplate.Clone();
							dropdownContainer.Bounds.Y = initContainer.Bounds.Height;
							initContainer.Bounds.Height += dropdownContainer.Bounds.Height;
							dropdownContainer.Get<LabelWidget>("LABEL").GetText = () => ddo.Name;

							var dropdown = dropdownContainer.Get<DropDownButtonWidget>("OPTION");
							Func<KeyValuePair<string, string>, ScrollItemWidget, ScrollItemWidget> dropdownSetup = (option, template) =>
							{
								var item = ScrollItemWidget.Setup(template,
									() => ddo.GetValue(actor) == option.Key,
									() => ddo.OnChange(actor, option.Key));

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
			else
			{
				// Selected actor is null, hide the border and edit panel.
				actorIDField.YieldKeyboardFocus();
				CurrentActor = null;
			}
		}

		void Delete()
		{
			if (CurrentActor != null)
				editorActorLayer.Remove(CurrentActor);

			Close();
		}

		void Close()
		{
			actorIDField.YieldKeyboardFocus();
			editor.DefaultBrush.SelectedActor = null;
			CurrentActor = null;
		}
	}
}
