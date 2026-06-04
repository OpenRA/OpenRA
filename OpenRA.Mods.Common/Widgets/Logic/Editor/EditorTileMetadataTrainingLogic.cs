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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class EditorTileMetadataTrainingLogic : ChromeLogic
	{
		readonly Widget editorRoot;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly ModData modData;
		readonly EditorTileMetadataTraining training;
		readonly Widget orientationPanel;
		readonly EditorOrientationGridWidget orientationGrid;

		[ObjectCreator.UseCtor]
		public EditorTileMetadataTrainingLogic(Widget widget, World world, WorldRenderer worldRenderer, ModData modData)
		{
			editorRoot = widget;
			this.world = world;
			this.worldRenderer = worldRenderer;
			this.modData = modData;

			var terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			training = new EditorTileMetadataTraining(modData, terrainInfo, world.Map.Rules);

			orientationPanel = widget.GetOrNull("EDITOR_ORIENTATION_TRAINING_PANEL");
			if (orientationPanel != null)
			{
				orientationPanel.IsVisible = () => training.ShowOrientationTraining;
				orientationGrid = orientationPanel.Get<EditorOrientationGridWidget>("ORIENTATION_GRID");
				orientationGrid.GetSelectedSlot = () => training.PendingOrientationSlot;
				orientationGrid.OnSelectSlot = training.SelectOrientationSlot;
				training.Changed += UpdateOrientationGridMode;
				UpdateOrientationGridMode();

				var saveButton = orientationPanel.Get<ButtonWidget>("ORIENTATION_SAVE_BUTTON");
				saveButton.OnClick = training.SaveOrientation;
				saveButton.IsDisabled = () => !training.ShowOrientationSave || !training.PendingOrientationSlot.HasValue;

				orientationPanel.Get<ButtonWidget>("ORIENTATION_CANCEL_BUTTON").OnClick = training.Cancel;
			}

			var trainButton = widget.GetOrNull<ButtonWidget>("OPEN_TILE_METADATA_TRAIN_BUTTON");
			if (trainButton != null)
				trainButton.OnClick = () => EditorTileMetadataDatabaseLogic.Toggle(editorRoot, world, worldRenderer, modData, training);
		}

		void UpdateOrientationGridMode()
		{
			if (orientationGrid == null)
				return;

			orientationGrid.RingCenterSlots = training.Mode == EditorMetadataTrainingKind.OrientationRing;
		}

		protected override void Dispose(bool disposing)
		{
			if (orientationGrid != null)
				training.Changed -= UpdateOrientationGridMode;

			if (EditorTileMetadataTraining.Instance == training)
				EditorTileMetadataTraining.Instance = null;

			base.Dispose(disposing);
		}
	}
}
