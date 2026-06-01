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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[IncludeStaticFluentReferences(
		typeof(MoveRegionEditorAction),
		typeof(MoveToolInfo))]
	public sealed class MoveToolLogic : ChromeLogic
	{
		readonly Widget widget;
		readonly EditorViewportControllerWidget editor;
		readonly WorldRenderer worldRenderer;
		EditorMoveBrush moveBrush;
		bool moveEnabled;

		[ObjectCreator.UseCtor]
		public MoveToolLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			this.widget = widget;
			editor = widget.Parent.Parent.Parent.Parent.Get<EditorViewportControllerWidget>("MAP_EDITOR");
			this.worldRenderer = worldRenderer;

			var enableCheckbox = widget.Get<CheckboxWidget>("ENABLE_CHECKBOX");
			var placeButton = widget.Get<ButtonWidget>("PLACE_BUTTON");
			var resetButton = widget.Get<ButtonWidget>("RESET_BUTTON");
			var statusLabel = widget.Get<LabelWidget>("STATUS_LABEL");

			enableCheckbox.IsChecked = () => moveEnabled;
			enableCheckbox.OnClick = () =>
			{
				moveEnabled ^= true;
				UpdateMoveBrushState();
			};

			placeButton.OnClick = () => moveBrush?.Place();
			resetButton.OnClick = () => moveBrush?.ResetOffset();

			placeButton.IsDisabled = () => !moveEnabled || moveBrush == null || !moveBrush.CanPlace();
			resetButton.IsDisabled = () => !moveEnabled || moveBrush == null || moveBrush.Offset == CVec.Zero || !moveBrush.HasMoveContent;

			statusLabel.GetText = () =>
			{
				if (!moveEnabled)
					return FluentProvider.GetMessage(MoveToolStatusDisabled);

				if (moveBrush == null || !moveBrush.HasMoveContent)
					return FluentProvider.GetMessage(MoveToolStatusNoSelection);

				if (moveBrush.Offset == CVec.Zero)
					return FluentProvider.GetMessage(MoveToolStatusReady);

				return FluentProvider.GetMessage(
					MoveToolStatusOffset,
					"x", moveBrush.Offset.X,
					"y", moveBrush.Offset.Y);
			};

			MapToolsLogic.OnSelected += TabSelected;

			var root = widget;
			while (root != null && root.Id != "EDITOR_WORLD_ROOT")
				root = root.Parent;

			if (root != null)
			{
				var keyHandler = root.Get<LogicKeyListenerWidget>("MOVE_KEYHANDLER");
				keyHandler.AddHandler(HandleMoveHotkey);
			}
		}

		[FluentReference]
		const string MoveToolStatusDisabled = "label-move-tool-disabled";

		[FluentReference]
		const string MoveToolStatusNoSelection = "label-move-tool-no-selection";

		[FluentReference]
		const string MoveToolStatusReady = "label-move-tool-ready";

		[FluentReference("x", "y")]
		const string MoveToolStatusOffset = "label-move-tool-offset";

		void TabSelected(bool isVisible)
		{
			if (isVisible && widget.IsVisible())
				UpdateMoveBrushState();
			else
				DeactivateMoveBrush();
		}

		void UpdateMoveBrushState()
		{
			if (moveEnabled && widget.IsVisible())
			{
				if (editor.CurrentBrush is not EditorMoveBrush)
				{
					moveBrush = new EditorMoveBrush(editor, worldRenderer);
					editor.SetBrush(moveBrush);
				}
			}
			else
				DeactivateMoveBrush();
		}

		void DeactivateMoveBrush()
		{
			if (editor.CurrentBrush is EditorMoveBrush)
			{
				moveBrush?.Dispose();
				moveBrush = null;
				editor.ClearBrush();
			}
		}

		bool HandleMoveHotkey(KeyInput e)
		{
			if (!moveEnabled || !widget.IsVisible() || moveBrush == null || !moveBrush.HasMoveContent)
				return false;

			if (e.Event != KeyInputEvent.Down || e.Modifiers != Modifiers.None)
				return false;

			var delta = e.Key switch
			{
				Keycode.W => new CVec(0, -1),
				Keycode.A => new CVec(-1, 0),
				Keycode.S => new CVec(0, 1),
				Keycode.D => new CVec(1, 0),
				_ => CVec.Zero
			};

			if (delta != CVec.Zero)
			{
				moveBrush.Nudge(delta);
				return true;
			}

			if (e.Key is Keycode.RETURN or Keycode.KP_ENTER && moveBrush.CanPlace())
			{
				moveBrush.Place();
				return true;
			}

			return false;
		}

		protected override void Dispose(bool disposing)
		{
			MapToolsLogic.OnSelected -= TabSelected;
			moveBrush?.Dispose();
			base.Dispose(disposing);
		}
	}
}
