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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorLogic : ChromeLogic
	{
		MapCopyFilters copyFilters = MapCopyFilters.All;

		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var editorViewport = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");

			var copypasteButton = widget.GetOrNull<ButtonWidget>("COPYPASTE_BUTTON");
			if (copypasteButton != null)
			{
				var copyPasteKey = copypasteButton.Key.GetValue();

				copypasteButton.OnClick = () => editorViewport.SetBrush(new EditorCopyPasteBrush(editorViewport, worldRenderer, () => copyFilters));
				copypasteButton.IsHighlighted = () => editorViewport.CurrentBrush is EditorCopyPasteBrush;
			}

			var copyFilterDropdown = widget.Get<DropDownButtonWidget>("COPYFILTER_BUTTON");
			copyFilterDropdown.OnMouseDown = _ =>
			{
				copyFilterDropdown.RemovePanel();
				copyFilterDropdown.AttachPanel(CreateCategoriesPanel());
			};

			var coordinateLabel = widget.GetOrNull<LabelWidget>("COORDINATE_LABEL");
			if (coordinateLabel != null)
			{
				coordinateLabel.GetText = () =>
				{
					var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
					var map = worldRenderer.World.Map;
					return map.Height.Contains(cell) ? $"{cell},{map.Height[cell]} ({map.Tiles[cell].Type})" : "";
				};
			}

			var cashLabel = widget.GetOrNull<LabelWidget>("CASH_LABEL");
			if (cashLabel != null)
			{
				var reslayer = worldRenderer.World.WorldActor.TraitsImplementing<EditorResourceLayer>().FirstOrDefault();
				if (reslayer != null)
					cashLabel.GetText = () => $"$ {reslayer.NetWorth}";
			}

			var undoButton = widget.GetOrNull<ButtonWidget>("UNDO_BUTTON");
			var redoButton = widget.GetOrNull<ButtonWidget>("REDO_BUTTON");
			if (undoButton != null && redoButton != null)
			{
				var actionManager = world.WorldActor.Trait<EditorActionManager>();
				undoButton.IsDisabled = () => !actionManager.HasUndos();
				undoButton.OnClick = () => actionManager.Undo();
				redoButton.IsDisabled = () => !actionManager.HasRedos();
				redoButton.OnClick = () => actionManager.Redo();
			}
		}

		Widget CreateCategoriesPanel()
		{
			var categoriesPanel = Ui.LoadWidget("COPY_FILTER_PANEL", null, new WidgetArgs());
			var categoryTemplate = categoriesPanel.Get<CheckboxWidget>("CATEGORY_TEMPLATE");

			MapCopyFilters[] allCategories = { MapCopyFilters.Terrain, MapCopyFilters.Resources, MapCopyFilters.Actors };
			foreach (var cat in allCategories)
			{
				var category = (CheckboxWidget)categoryTemplate.Clone();
				category.GetText = () => cat.ToString();
				category.IsChecked = () => copyFilters.HasFlag(cat);
				category.IsVisible = () => true;
				category.OnClick = () => copyFilters ^= cat;

				categoriesPanel.AddChild(category);
			}

			return categoriesPanel;
		}
	}
}
