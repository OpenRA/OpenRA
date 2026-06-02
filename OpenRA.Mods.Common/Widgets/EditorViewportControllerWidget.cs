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
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.Common.Widgets
{
	public enum EditorAssetMixMode { Random, Sequential }
	public enum EditorFillMode { Overlap, Delete }

	[IncludeStaticFluentReferences(
		typeof(ChangeSelectionAction),
		typeof(DeleteAreaAction),
		typeof(RemoveActorAction),
		typeof(RemoveResourceAction),
		typeof(MoveActorAction))]
	public class EditorViewportControllerWidget : Widget
	{
		[Desc("Main color of the selection grid.")]
		public readonly Color SelectionMainColor = Color.White;

		[Desc("Alternate color of the selection grid.")]
		public readonly Color SelectionAltColor = Color.Black;

		[Desc("Main color of the copy / paste grid.")]
		public readonly Color PasteColor = Color.FromArgb(0xFF4CFF00);

		[Desc("Glow color shown around a copied area selection.")]
		public readonly Color CopyColor = Color.FromArgb(0xFF0088FF);

		public EditorBlitSource? Clipboard { get; private set; }
		public CellCoordsRegion? CopySourceRegion { get; private set; }
		public bool HasClipboard => Clipboard.HasValue &&
			(Clipboard.Value.Actors.Count > 0 || Clipboard.Value.Tiles.Count > 0);

		public IEditorBrush CurrentBrush { get; private set; }

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;
		public readonly EditorDefaultBrush DefaultBrush;
		public EditorAssetMixMode AssetMixMode { get; private set; } = EditorAssetMixMode.Random;
		public int AssetFillDensity { get; private set; } = 100;
		public EditorFillMode AssetFillMode { get; private set; } = EditorFillMode.Overlap;

		public event Action BrushChanged;
		public event Action<EditorLocateAssetRequest> LocateAssetRequested;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly WorldRenderer worldRenderer;
		readonly EditorCursorLayer editorCursor;
		public int2 SelectionAltOffset { get; }

		bool enableTooltips;

		[ObjectCreator.UseCtor]
		public EditorViewportControllerWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			CurrentBrush = DefaultBrush = new EditorDefaultBrush(this, worldRenderer);

			editorCursor = worldRenderer.World.WorldActor.Trait<EditorCursorLayer>();
			editorCursor.SetSelectionBrush(DefaultBrush);
			editorCursor.SetBrush(CurrentBrush);

			// Allow zooming out to full map size
			worldRenderer.Viewport.UnlockMinimumZoom(0.25f);

			SelectionAltOffset = worldRenderer.World.Map.Grid.Type == MapGridType.Rectangular
				? new int2(1, 1)
				: new int2(0, 1);
		}

		public void ClearBrush() { SetBrush(null); }

		public void SetClipboard(EditorBlitSource clipboard, CellCoordsRegion sourceRegion)
		{
			Clipboard = clipboard;
			CopySourceRegion = sourceRegion;
			BrushChanged?.Invoke();
		}

		public void ClearClipboard()
		{
			Clipboard = null;
			CopySourceRegion = null;
			BrushChanged?.Invoke();
		}

		public void SetAssetMixMode(EditorAssetMixMode mixMode)
		{
			if (AssetMixMode == mixMode)
				return;

			AssetMixMode = mixMode;
			BrushChanged?.Invoke();
		}

		public void SetAssetFillDensity(int fillDensity)
		{
			fillDensity = ((fillDensity + 5) / 10 * 10).Clamp(10, 100);
			if (AssetFillDensity == fillDensity)
				return;

			AssetFillDensity = fillDensity;
			BrushChanged?.Invoke();
		}

		public void SetAssetFillMode(EditorFillMode fillMode)
		{
			if (AssetFillMode == fillMode)
				return;

			AssetFillMode = fillMode;
			BrushChanged?.Invoke();
		}

		public void RequestLocateAsset(EditorLocateAssetRequest request) =>
			LocateAssetRequested?.Invoke(request);

		public void SetBrush(IEditorBrush brush)
		{
			if (CurrentBrush != DefaultBrush)
				CurrentBrush?.Dispose();

			CurrentBrush = brush ?? DefaultBrush;

			if (IsEditorAssetSelection(CurrentBrush))
				DefaultBrush.ShowAreaPanel();

			BrushChanged?.Invoke();
			editorCursor.SetBrush(CurrentBrush);
		}

		static bool IsEditorAssetSelection(IEditorBrush brush)
		{
			return brush is EditorTileBrush or EditorActorBrush or EditorResourceBrush;
		}

		public override void MouseEntered()
		{
			enableTooltips = true;
		}

		public override void MouseExited()
		{
			tooltipContainer.Value.RemoveTooltip();
			enableTooltips = false;
		}

		public void SetTooltip(string tooltip)
		{
			if (!enableTooltips)
				return;

			if (tooltip != null)
			{
				Func<string> getTooltip = () => tooltip;
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getTooltip } });
			}
			else
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll && mi.Modifiers.HasModifier(Game.Settings.Game.ZoomModifier))
			{
				worldRenderer.Viewport.AdjustZoom(mi.Delta.Y * Game.Settings.Game.ZoomSpeed, mi.Location);
				return true;
			}

			if (CurrentBrush.HandleMouseInput(mi))
				return true;

			return base.HandleMouseInput(mi);
		}

		WPos cachedViewportPosition;
		public override void Tick()
		{
			// Clear any tooltips when the viewport is scrolled using the keyboard
			if (worldRenderer.Viewport.CenterPosition != cachedViewportPosition)
				SetTooltip(null);

			cachedViewportPosition = worldRenderer.Viewport.CenterPosition;
			CurrentBrush.Tick();
		}
	}
}
