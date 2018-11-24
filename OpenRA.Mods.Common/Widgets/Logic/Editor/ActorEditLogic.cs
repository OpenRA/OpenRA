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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ActorEditLogic : ChromeLogic
	{
		// Error states define overlapping bits to simplify panel reflow logic
		[Flags] enum ActorIDStatus { Normal = 0, Duplicate = 1, Empty = 3 }

		readonly WorldRenderer worldRenderer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorViewportControllerWidget editor;
		readonly ContainerWidget actorSelectBorder;
		readonly BackgroundWidget actorEditPanel;
		readonly LabelWidget typeLabel;
		readonly TextFieldWidget actorIDField;
		readonly LabelWidget actorIDErrorLabel;
		readonly DropDownButtonWidget ownersDropDown;
		readonly Widget initContainer;
		readonly Widget buttonContainer;

		readonly int editPanelPadding; // Padding between right edge of actor and the edit panel.
		readonly long scrollVisibleTimeout = 100; // Delay after scrolling map before edit widget becomes visible again.
		long lastScrollTime = 0;
		PlayerReference selectedOwner;

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
			actorSelectBorder = editor.Get<ContainerWidget>("ACTOR_SELECT_BORDER");
			actorEditPanel = editor.Get<BackgroundWidget>("ACTOR_EDIT_PANEL");

			typeLabel = actorEditPanel.Get<LabelWidget>("ACTOR_TYPE_LABEL");
			actorIDField = actorEditPanel.Get<TextFieldWidget>("ACTOR_ID");

			ownersDropDown = actorEditPanel.Get<DropDownButtonWidget>("OWNERS_DROPDOWN");

			initContainer = actorEditPanel.Get("ACTOR_INIT_CONTAINER");
			buttonContainer = actorEditPanel.Get("BUTTON_CONTAINER");

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
			actorSelectBorder.IsVisible = () => CurrentActor != null
				&& editor.CurrentBrush == editor.DefaultBrush
				&& Game.RunTime > lastScrollTime + scrollVisibleTimeout;
			actorEditPanel.IsVisible = actorSelectBorder.IsVisible;

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
					var found = world.Map.ActorDefinitions.Any(x => x.Key.ToLowerInvariant() == actorId);
					if (found)
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

			// Setup owners drop down
			selectedOwner = editorActorLayer.Players.Players.Values.First();
			Func<PlayerReference, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, () => selectedOwner == option, () =>
				{
					ownersDropDown.Text = option.Name;
					ownersDropDown.TextColor = option.Color.RGB;
					selectedOwner = option;

					CurrentActor.Owner = selectedOwner;
					CurrentActor.ReplaceInit(new OwnerInit(selectedOwner.Name));
				});

				item.Get<LabelWidget>("LABEL").GetText = () => option.Name;
				item.GetColor = () => option.Color.RGB;
				return item;
			};

			ownersDropDown.OnClick = () =>
			{
				var owners = editorActorLayer.Players.Players.Values.OrderBy(p => p.Name);
				ownersDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 270, owners, setupItem);
			};

			ownersDropDown.Text = selectedOwner.Name;
			ownersDropDown.TextColor = selectedOwner.Color.RGB;
		}

		void SetActorID(World world, string actorId)
		{
			var actorDef = world.Map.ActorDefinitions.First(x => x.Key == CurrentActor.ID);
			actorDef.Key = actorId;
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
				var origin = worldRenderer.Viewport.WorldToViewPx(new int2(actor.Bounds.X, actor.Bounds.Y));

				// If we scrolled, hide the edit box for a moment
				if (actorSelectBorder.Bounds.X != origin.X || actorSelectBorder.Bounds.Y != origin.Y)
					lastScrollTime = Game.RunTime;

				// If we changed actor, move widgets
				if (CurrentActor != actor)
				{
					lastScrollTime = 0; // Ensure visible
					selectedOwner = actor.Owner;
					ownersDropDown.Text = selectedOwner.Name;
					ownersDropDown.TextColor = selectedOwner.Color.RGB;

					CurrentActor = actor;

					initialActorID = actorIDField.Text = actor.ID;

					var font = Game.Renderer.Fonts[typeLabel.Font];
					var truncatedType = WidgetUtils.TruncateText(actor.DescriptiveName, typeLabel.Bounds.Width, font);
					typeLabel.Text = truncatedType;

					actorIDField.CursorPosition = actor.ID.Length;
					actorSelectBorder.Bounds.Width = actor.Bounds.Width * 2;
					actorSelectBorder.Bounds.Height = actor.Bounds.Height * 2;
					nextActorIDStatus = ActorIDStatus.Normal;
				}

				actorSelectBorder.Bounds.X = origin.X;
				actorSelectBorder.Bounds.Y = origin.Y;

				// Set the edit panel to the right of the selection border.
				actorEditPanel.Bounds.X = origin.X + actorSelectBorder.Bounds.Width / 2 + editPanelPadding;
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
			actorSelectBorder.Visible = false;
			CurrentActor = null;
		}
	}
}
