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
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[IncludeStaticFluentReferences(typeof(MultiBrushTool))]
	public sealed class MultiBrushToolInfo : TraitInfo
	{
		[Desc("The default size for the multi-brush tool.")]
		public readonly int DefaultSize = 1;

		[Desc("The default multi-brush.")]
		public readonly string DefaultMultiBrush = "";

		[Desc("A list of multi-brushes to ignore.")]
		public readonly string[] IgnoredMultiBrushes = [];

		public override object Create(ActorInitializer init)
		{
			return new MultiBrushTool(init.Self, this);
		}
	}

	public sealed class MultiBrushTool : IEditorTool, IWorldLoaded
	{
		public const int MaxBrushSize = 15;
		public const int MaxFloodLimit = 1000;

		public enum ShapeType
		{
			Single,
			Square,
			Circle,
			Flood,
		}

		[FluentReference]
		const string Label = "label-tool-paint-brush";

		[Desc("The widget tree to open when the tool is selected.")]
		const string PanelWidget = "MULTI_BRUSH_TOOL_PANEL";

		[FluentReference]
		const string SingleShape = "label-shape-single";

		[FluentReference]
		const string SquareShape = "label-shape-square";

		[FluentReference]
		const string CircleShape = "label-shape-circle";

		[FluentReference]
		const string FloodFillShape = "label-shape-flood-fill";

		public string GetBrushTypeFluentKey(ShapeType shape)
		{
			return shape switch
			{
				ShapeType.Single => SingleShape,
				ShapeType.Square => SquareShape,
				ShapeType.Circle => CircleShape,
				ShapeType.Flood => FloodFillShape,
				_ => throw new NotImplementedException()
			};
		}

		public enum FloodTerrainBehavior
		{
			AllTerrain,
			TemplateTypes,
			TerrainTypes,
			TargetTypes,
		}

		[FluentReference]
		const string FloodBehaviorAllTerrain = "label-flood-behavior-all-terrain";

		[FluentReference]
		const string FloodBehaviorTemplateTypes = "label-flood-behavior-template-types";

		[FluentReference]
		const string FloodBehaviorTerrainTypes = "label-flood-behavior-terrain-types";

		[FluentReference]
		const string FloodBehaviorTargetTypes = "label-flood-behavior-terrain-target-types";

		public string GetFloodBehaviorFluentKey(FloodTerrainBehavior behavior)
		{
			return behavior switch
			{
				FloodTerrainBehavior.AllTerrain => FloodBehaviorAllTerrain,
				FloodTerrainBehavior.TemplateTypes => FloodBehaviorTemplateTypes,
				FloodTerrainBehavior.TerrainTypes => FloodBehaviorTerrainTypes,
				FloodTerrainBehavior.TargetTypes => FloodBehaviorTargetTypes,
				_ => throw new NotImplementedException()
			};
		}

		readonly CachedTransform<string, string> currentTypeLabel = new(s => FluentProvider.GetMessage(s));
		public string GetCurrentTypeLabel() => currentTypeLabel.Update(GetBrushTypeFluentKey(CurrentFunctionalBrushType));
		readonly CachedTransform<string, string> currentFloodBehaviorLabel = new(s => FluentProvider.GetMessage(s));
		public string GetCurrentFloodBehaviorLabel() => currentFloodBehaviorLabel.Update(GetFloodBehaviorFluentKey(CurrentFloodBehavior));

		public bool IsEnabled { get; }

		string IEditorTool.Label => Label;
		string IEditorTool.PanelWidget => PanelWidget;
		public TraitInfo TraitInfo { get; }
		public MultiBrushToolInfo Info;

		readonly World world;
		WorldRenderer worldRenderer;
		EditorActorLayer editorActorLayer;
		IResourceLayer resourceLayer;

		public event Action BlitRefreshed;

		public ImmutableArray<string> MultiBrushCollectionNames;
		public ImmutableDictionary<string, ImmutableArray<MultiBrush>> MultiBrushCollections;
		public ImmutableDictionary<string, MultiBrush.Replaceability> MultiBrushReplaceabilities;

		public (string Name, ImmutableArray<MultiBrush> Brushes) CurrentMultiBrushCollection;
		public PlayerReference CurrentActorOwner { get; private set; }
		public ShapeType CurrentShape { get; private set; } = ShapeType.Square;
		public ShapeType CurrentFunctionalBrushType => IsFloodFill ? ShapeType.Flood : CurrentShape;
		public Size CurrentSize { get; private set; }
		public int CurrentSparsity { get; private set; } = 0;
		public int CurrentFloodLimit { get; private set; } = 500;
		public EditorBlitSource CurrentCursorBlitSource { get; private set; }
		public bool CurrentAdaptHeight { get; private set; }
		public FloodTerrainBehavior CurrentFloodBehavior { get; private set; } = FloodTerrainBehavior.TemplateTypes;
		public bool CurrentFloodIgnoreActors { get; private set; }
		public bool CurrentFloodIgnoreHeight { get; private set; } = true;
		public MultiBrush.Replaceability CurrentAvailableReplaceability { get; private set; } = MultiBrush.Replaceability.Any;
		public MultiBrush.Replaceability CurrentReplaceability { get; private set; } = MultiBrush.Replaceability.Any;
		MapBlitFilters currentFilters = MapBlitFilters.Terrain | MapBlitFilters.Actors;
		MultiBrush.Replaceability selectedReplaceability = MultiBrush.Replaceability.Any;

		CellLayer<bool> brushMask;

		readonly HashSet<CPos> mousePositions = [];
		readonly HashSet<CPos> newMousePositions = [];
		CPos lastMousePosition;

		CVec lastMaskOffset;
		MultiBrush lastMultiBrush;

		CPos targetMousePos;
		CVec firstTargetBlitCoordOffset;

		public bool IsFloodFill { get; private set; }
		bool isFloodFillEmpty;
		public EditorBlitSource CurrentFloodFillBlitSource { get; private set; }
		CellLayer<bool> floodBrushMask;
		CPos targetFloodTopLeft;

		public EditorBlitSource CurrentSingleBlitSource { get; private set; }
		CellCoordsRegion targetSingleCellCoords;
		MultiBrush lastSingleBrush;

		// Perf
		CellLayer<MultiBrush.Replaceability> replaceabilityLayer;

		public MultiBrushTool(Actor self, MultiBrushToolInfo info)
		{
			world = self.World;
			TraitInfo = info;
			Info = info;
			CurrentSize = new Size(info.DefaultSize, info.DefaultSize);
			CurrentAdaptHeight = self.World.Map.Grid.Type == MapGridType.RectangularIsometric;

			var templatedTerrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			var multiBrushCollectionNames = new List<string>(templatedTerrainInfo.MultiBrushCollections.Count);
			var multiBrushCollections = new Dictionary<string, ImmutableArray<MultiBrush>>();
			var multiBrushReplaceabilities = new Dictionary<string, MultiBrush.Replaceability>();
			foreach (var (name, _) in templatedTerrainInfo.MultiBrushCollections)
			{
				if (info.IgnoredMultiBrushes.Contains(name, StringComparer.InvariantCultureIgnoreCase))
					continue;

				var brushes = MultiBrush.LoadCollection(world.Map, name)
					.RemoveAll(b => !b.HasActors && !b.HasTiles);

				if (brushes.Length == 0)
					continue;

				var replaceability = brushes
					.Select(info => info.Contract())
					.Aggregate((a, b) => a | b);

				if (replaceability == MultiBrush.Replaceability.None)
					continue;

				foreach (var brush in brushes)
					brush.RemoveOffset();

				multiBrushCollections[name] = brushes;
				multiBrushReplaceabilities[name] = replaceability;
				multiBrushCollectionNames.Add(name);
			}

			MultiBrushCollections = multiBrushCollections.ToImmutableDictionary();
			MultiBrushReplaceabilities = multiBrushReplaceabilities.ToImmutableDictionary();
			MultiBrushCollectionNames = multiBrushCollectionNames.ToImmutableArray();

			IsEnabled = MultiBrushCollections.Count > 0;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (IsEnabled)
			{
				worldRenderer = wr;
				editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
				resourceLayer = world.WorldActor.Trait<IResourceLayer>();

				CurrentActorOwner = editorActorLayer.Players.Players.Values.First();

				var firstBrush = MultiBrushCollectionNames.Contains(Info.DefaultMultiBrush)
					? Info.DefaultMultiBrush
					: MultiBrushCollectionNames.FirstOrDefault();

				SelectMultiBrush(firstBrush);
			}
		}

		public bool CanPaint()
		{
			if (IsFloodFill)
				return !isFloodFillEmpty;

			if (CurrentShape == ShapeType.Single)
				return CurrentSingleBlitSource.Tiles != null && (CurrentSingleBlitSource.Tiles.Count > 0 || CurrentSingleBlitSource.Actors.Count > 0);

			return CurrentCursorBlitSource.Tiles != null && (CurrentCursorBlitSource.Tiles.Count > 0 || CurrentCursorBlitSource.Actors.Count > 0);
		}

		bool needToRefreshCursorBlit;
		bool needToRefreshFloodFillBlit;
		bool needToRefreshSingleBlit;

		public void RefreshBlit(bool forceRefresh)
		{
			needToRefreshCursorBlit |= forceRefresh;
			needToRefreshSingleBlit |= forceRefresh;
			needToRefreshFloodFillBlit |= forceRefresh;

			// Nothing to paint with
			if ((MultiBrushReplaceabilities[CurrentMultiBrushCollection.Name] & CurrentReplaceability) == MultiBrush.Replaceability.None)
			{
				isFloodFillEmpty = true;
				CurrentCursorBlitSource = default;
				CurrentSingleBlitSource = default;
				return;
			}

			var filteredBrushes = CurrentMultiBrushCollection.Brushes
				.Where(b => (b.Contract() & CurrentReplaceability) != MultiBrush.Replaceability.None)
				.ToImmutableArray();

			// We use the lazy evaluation for refreshing the blit.
			if (IsFloodFill)
			{
				if (needToRefreshFloodFillBlit)
					BlitRefreshed?.Invoke();

				RefreshFloodFillBlit(filteredBrushes, needToRefreshFloodFillBlit);
				needToRefreshFloodFillBlit = needToRefreshFloodFillBlit && !forceRefresh;
			}
			else if (CurrentShape == ShapeType.Single)
			{
				if (needToRefreshSingleBlit)
					BlitRefreshed?.Invoke();

				RefreshSingleBlit(filteredBrushes);
				needToRefreshSingleBlit = needToRefreshSingleBlit && !forceRefresh;
			}
			else
			{
				if (needToRefreshCursorBlit)
					BlitRefreshed?.Invoke();

				RefreshCursorBlit(filteredBrushes, needToRefreshCursorBlit);
				needToRefreshCursorBlit = needToRefreshCursorBlit && !forceRefresh;
			}
		}

		void RefreshSingleBlit(ImmutableArray<MultiBrush> brushes)
		{
			var random = Game.CosmeticRandom;
			var chosenBrush = MultiBrush.PickRandomBrush(brushes, random);

			// While this makes selection biased, it makes for a better user experience.
			if (brushes.Length > 1)
				while (lastSingleBrush == chosenBrush)
					chosenBrush = MultiBrush.PickRandomBrush(brushes, random);

			lastSingleBrush = chosenBrush;

			var brush = chosenBrush.Clone();
			if (brush.Area == 0)
			{
				CurrentSingleBlitSource = default;
				return;
			}

			brush.UpdateOwner(CurrentActorOwner?.Name);

			var targetCellCoords = brush.GetCellCoordsRegion();
			targetSingleCellCoords = targetCellCoords;

			CurrentSingleBlitSource = brush.ToEditorBlitSource(worldRenderer, random, targetCellCoords, CurrentActorOwner);
		}

		void RefreshCursorBlit(ImmutableArray<MultiBrush> brushes, bool forceRefresh)
		{
			var grid = world.Map.Grid.Type;
			var positions = mousePositions.ToList();

			// We are not drawing, but we still need to render the brush.
			if (positions.Count == 0)
				positions.Add(CPos.Zero);

			if (forceRefresh)
				foreach (var pos in positions)
					newMousePositions.Add(pos);

			if (!forceRefresh && newMousePositions.Count == 0)
				return;

			if (forceRefresh)
				lastMultiBrush = null;

			var brushSizeVec = new CVec(CurrentSize.Width - 1, CurrentSize.Height - 1);

			// We don't center the brush here, centering is done from first mouse position.
			var targetRegion = CellCoordsRegion.Union(positions.Select(pos => new CellCoordsRegion(pos, pos + brushSizeVec)));
			var mouseOffset = CPos.Zero - targetRegion.TopLeft;

			var targetRegionSize = new CellCoordsRegion(CPos.Zero, targetRegion.BottomRight + mouseOffset);

			// In isometric maps, map region does not match cell regions, so we need
			// to create a map region which would encapsulate the target cell region.
			var newBrushMask = CellLayerUtils.BoundingCellLayer<bool>(grid, targetRegionSize, out var offset);
			lastMaskOffset = offset;

			// Offset the brush cell coords so it accurately represents the map region in isometric mods.
			var brushCellCoords = new CellCoordsRegion(CPos.Zero + offset, targetRegionSize.BottomRight + offset);

			// When drawing, we stop centering the brush on mouse position. Now we need to adjust the blit position manually.
			var cellRegionDelta = CVec.Zero;
			if (mousePositions.Count != 0)
			{
				var newTarget = targetRegion.TopLeft - offset - firstTargetBlitCoordOffset;
				cellRegionDelta = newTarget - targetMousePos;
				targetMousePos = newTarget;
			}

			// Only paint new mouse positions.
			foreach (var pos in newMousePositions)
			{
				var brushPos = pos + mouseOffset + lastMaskOffset;
				var brushRegion = new CellCoordsRegion(brushPos, brushPos + brushSizeVec);

				switch (CurrentShape)
				{
					case ShapeType.Square:
						CellLayerUtils.OverCellRegion(
							newBrushMask,
							brushRegion,
							(mpos, cpos) => newBrushMask[mpos] = true);
						break;
					case ShapeType.Circle:
						var center = CellLayerUtils.Center(grid, brushRegion);
						var radius = CellLayerUtils.Radius(grid, brushRegion);
						CellLayerUtils.OverCircle(
							cellLayer: newBrushMask,
							region: brushRegion,
							wCenter: center,
							wRadius: radius,
							outside: false,
							action: (mpos, _, _, _) => newBrushMask[mpos] = true);
						break;
				}
			}

			newMousePositions.Clear();

			if (replaceabilityLayer == null || newBrushMask.Size != replaceabilityLayer.Size)
				replaceabilityLayer = new CellLayer<MultiBrush.Replaceability>(grid, newBrushMask.Size);

			MultiBrush brush;

			// For smoother drawing, we reuse the already painted features.
			// This biases the brush towards smaller actors, which is an unintended consequence.
			if (lastMultiBrush != null)
			{
				brush = lastMultiBrush;

				if (cellRegionDelta != CVec.Zero)
					brush.AddOffset(-cellRegionDelta);

				CellLayerUtils.ApplyMaskDifference(replaceabilityLayer, newBrushMask, brushMask, cellRegionDelta, CurrentReplaceability, MultiBrush.Replaceability.None);
			}
			else
			{
				lastMultiBrush = brush = new MultiBrush();
				CellLayerUtils.Map(replaceabilityLayer, newBrushMask, val => val ? CurrentReplaceability : MultiBrush.Replaceability.None);
			}

			brushMask = newBrushMask;

			var random = Game.CosmeticRandom;
			MultiBrush.PaintAreaBrush(
				brush,
				worldRenderer.World.Map,
				replaceabilityLayer,
				brushes,
				CurrentSparsity,
				random,
				false,
				CurrentActorOwner?.Name);

			CurrentCursorBlitSource = brush.ToEditorBlitSource(worldRenderer, random, brushCellCoords, CurrentActorOwner);
		}

		void RefreshFloodFillBlit(ImmutableArray<MultiBrush> brushes, bool forceRefresh)
		{
			var map = world.Map;
			(CPos Cell, bool Val)[] seeds;
			if (mousePositions.Count != 0)
			{
				seeds = mousePositions.Where(map.Contains).Select(c => (c, true)).ToArray();
				if (seeds.Length == 0)
				{
					isFloodFillEmpty = true;
					return;
				}
			}
			else
			{
				if (map.Contains(lastMousePosition))
					seeds = [(lastMousePosition, true)];
				else
				{
					isFloodFillEmpty = true;
					return;
				}
			}

			if (!forceRefresh && CurrentShape != ShapeType.Flood && floodBrushMask != null && seeds.All(s => floodBrushMask[s.Cell]))
			{
				isFloodFillEmpty = false;
				return;
			}

			var newBrushMask = new CellLayer<bool>(map);
			var count = seeds.Length;

			var filler = GetFiller(newBrushMask, seeds);
			CellLayerUtils.FloodFill(newBrushMask, seeds, filler, DirectionExts.Spread4CVec);
			if (count == 0)
			{
				isFloodFillEmpty = true;
				return;
			}

			if (replaceabilityLayer == null || newBrushMask.Size != replaceabilityLayer.Size)
				replaceabilityLayer = new CellLayer<MultiBrush.Replaceability>(map.Grid.Type, newBrushMask.Size);

			CellLayerUtils.Map(replaceabilityLayer, newBrushMask, val => val ? CurrentReplaceability : MultiBrush.Replaceability.None);

			floodBrushMask = newBrushMask;

			var newMultibrush = new MultiBrush();
			var random = Game.CosmeticRandom;
			MultiBrush.PaintAreaBrush(
				newMultibrush,
				map,
				replaceabilityLayer,
				brushes,
				CurrentSparsity,
				random,
				false,
				CurrentActorOwner?.Name);

			var cellCoords = newMultibrush.GetCellCoordsRegion();
			targetFloodTopLeft = cellCoords.TopLeft;
			CurrentFloodFillBlitSource = newMultibrush.ToEditorBlitSource(worldRenderer, random, cellCoords, CurrentActorOwner);

			isFloodFillEmpty = CurrentFloodFillBlitSource.Tiles.Count == 0 && CurrentFloodFillBlitSource.Actors.Count == 0;
		}

		public void SetFloodFill(bool set)
		{
			if (CurrentShape == ShapeType.Flood)
				return;

			if (IsFloodFill == set)
				return;

			IsFloodFill = set;
			RefreshBlit(false);
		}

		public void SetFloodBehavior(FloodTerrainBehavior behavior)
		{
			CurrentFloodBehavior = behavior;
			RefreshBlit(true);
		}

		public void ToggleFloodIgnoreActors()
		{
			CurrentFloodIgnoreActors = !CurrentFloodIgnoreActors;
			RefreshBlit(true);
		}

		public void ToggleFloodIgnoreHeight()
		{
			CurrentFloodIgnoreHeight = !CurrentFloodIgnoreHeight;
			RefreshBlit(true);
		}

		public void SelectMultiBrush(string name)
		{
			if (MultiBrushCollections.TryGetValue(name, out var brushes))
			{
				CurrentMultiBrushCollection = (name, brushes);

				var replaceability = MultiBrushReplaceabilities[name];
				CurrentReplaceability = selectedReplaceability & replaceability;

				// When selecting a brush we want to make sure we don't filter out all of its MultiBrushes
				if (CurrentReplaceability == MultiBrush.Replaceability.None)
				{
					selectedReplaceability = replaceability;
					CurrentReplaceability = replaceability;
				}

				var filters = MapBlitFilters.None;
				if (CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Actor) || CurrentReplaceability.HasFlag(MultiBrush.Replaceability.SubCellActor))
					filters |= MapBlitFilters.Actors;
				if (CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Tile))
					filters |= MapBlitFilters.Terrain;

				// Ensure we always have at least one filter enabled.
				currentFilters &= filters;
				if (currentFilters == MapBlitFilters.None)
					currentFilters = filters;

				CurrentAvailableReplaceability = replaceability;

				RefreshBlit(true);
			}
		}

		public void SetActorOwner(PlayerReference owner)
		{
			if (CurrentActorOwner == owner)
				return;

			CurrentActorOwner = owner;
			RefreshBlit(true);
		}

		public void SetSize(Size size)
		{
			CurrentSize = size;
			RefreshBlit(true);
		}

		public void SetFloodLimit(int limit)
		{
			CurrentFloodLimit = limit;
			RefreshBlit(true);
		}

		public void SetSparsity(int sparsity)
		{
			CurrentSparsity = sparsity;
			RefreshBlit(true);
		}

		public void ToggleAdaptHeight()
		{
			CurrentAdaptHeight = !CurrentAdaptHeight;
		}

		public void SetShape(ShapeType shape)
		{
			CurrentShape = shape;
			IsFloodFill = shape == ShapeType.Flood;

			RefreshBlit(true);
		}

		public void ToggleReplacability(MultiBrush.Replaceability filter)
		{
			selectedReplaceability ^= filter;

			currentFilters = MapBlitFilters.None;
			if (selectedReplaceability.HasFlag(MultiBrush.Replaceability.Actor) || selectedReplaceability.HasFlag(MultiBrush.Replaceability.SubCellActor))
				currentFilters |= MapBlitFilters.Actors;
			if (selectedReplaceability.HasFlag(MultiBrush.Replaceability.Tile))
				currentFilters |= MapBlitFilters.Terrain;

			CurrentReplaceability = selectedReplaceability & MultiBrushReplaceabilities[CurrentMultiBrushCollection.Name];
			RefreshBlit(true);
		}

		CVec GetCursorBlitCellCoordsOffset()
		{
			return CellLayerUtils.WPosToCPos(
				CellLayerUtils.Center(world.Map.Grid.Type, CurrentCursorBlitSource.CellCoords),
				world.Map.Grid.Type) - CPos.Zero;
		}

		public CPos GetCursorBlitCellCoordsPos(CPos mousePos)
		{
			if (mousePositions.Count != 0)
				return targetMousePos;

			return mousePos - GetCursorBlitCellCoordsOffset();
		}

		public CPos GetSingleBlitCellCoordsPos(CPos mousePos)
		{
			var offset = CellLayerUtils.WPosToCPos(CellLayerUtils.Center(world.Map.Grid.Type, targetSingleCellCoords), world.Map.Grid.Type) - CPos.Zero;
			return mousePos - offset;
		}

		public EditorBlit CreateBlit()
		{
			if (mousePositions.Count == 0)
				return null;

			var useGroundHeight = !CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Tile) || CurrentAdaptHeight;

			if (IsFloodFill)
			{
				if (CurrentFloodFillBlitSource.Tiles.Count == 0 && CurrentFloodFillBlitSource.Actors.Count == 0)
					return null;

				var editorBlit = new EditorBlit(
					currentFilters,
					resourceLayer,
					targetFloodTopLeft,
					world.Map,
					CurrentFloodFillBlitSource,
					editorActorLayer,
					true,
					useGroundHeight);

				return editorBlit;
			}
			else if (CurrentShape == ShapeType.Single)
			{
				if (CurrentSingleBlitSource.Tiles.Count == 0 && CurrentSingleBlitSource.Actors.Count == 0)
					return null;

				var pos = GetSingleBlitCellCoordsPos(lastMousePosition) + (CurrentSingleBlitSource.CellCoords.TopLeft - CPos.Zero);
				var editorBlit = new EditorBlit(
					currentFilters,
					resourceLayer,
					pos,
					world.Map,
					CurrentSingleBlitSource,
					editorActorLayer,
					true,
					useGroundHeight);

				return editorBlit;
			}
			else
			{
				if (CurrentCursorBlitSource.Tiles.Count == 0 && CurrentCursorBlitSource.Actors.Count == 0)
					return null;

				// EditorBlit constructor resets target TopLeft position, fight against it.
				var pos = targetMousePos + (CurrentCursorBlitSource.CellCoords.TopLeft - CPos.Zero);
				var editorBlit = new EditorBlit(
					currentFilters,
					resourceLayer,
					pos,
					world.Map,
					CurrentCursorBlitSource,
					editorActorLayer,
					true,
					useGroundHeight);

				return editorBlit;
			}
		}

		public void SetMousePosition(CPos pos, bool painting)
		{
			if (painting)
			{
				AddMousePosition(pos);
			}
			else if (IsFloodFill)
			{
				if (mousePositions.Count == 0 && lastMousePosition != pos)
				{
					lastMousePosition = pos;
					RefreshBlit(false);
				}
			}

			lastMousePosition = pos;
		}

		public void AddMousePosition(CPos pos)
		{
			if (mousePositions.Count == 0)
			{
				targetMousePos = GetCursorBlitCellCoordsPos(pos);
				firstTargetBlitCoordOffset = GetCursorBlitCellCoordsOffset() - lastMaskOffset;
				mousePositions.Add(pos);
			}
			else if (CurrentShape == ShapeType.Single)
			{
				mousePositions.Clear();
				AddMousePosition(pos);
			}
			else if (mousePositions.Add(pos))
			{
				newMousePositions.Add(pos);
				RefreshBlit(false);
			}
		}

		public void ClearMousePositions()
		{
			mousePositions.Clear();
			newMousePositions.Clear();
			lastMultiBrush = null;
			IsFloodFill = CurrentShape == ShapeType.Flood;

			RefreshBlit(true);
		}

		public Func<CPos, bool, bool?> GetFiller(CellLayer<bool> mask, (CPos Cell, bool Val)[] seeds)
		{
			var map = world.Map;
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var terrainInfo = map.Rules.TerrainInfo;

			var replaceTemplates = seeds.Select(s => mapTiles[s.Cell].Type).ToImmutableHashSet();
			var replaceTerrain = seeds.Select(s => map.GetTerrainInfo(s.Cell).Type).ToImmutableHashSet();
			var replaceTargetTypes = new BitSet<TargetableType>(seeds.SelectMany(s => map.GetTerrainInfo(s.Cell).TargetTypes).ToArray());
			var replaceHeights = seeds.Select(s => mapHeight[s.Cell]).ToImmutableHashSet();

			var replaceActors = seeds.Select(s => editorActorLayer.PreviewsAtCell(s.Cell).Select(a => a.Type)).SelectMany(t => t).ToImmutableHashSet();
			var hasEmptyActors = seeds.Any(s => !editorActorLayer.PreviewsAtCell(s.Cell).Any());

			var count = (uint)seeds.Length;

			bool? EverythingFiller(CPos cpos, bool _)
			{
				var mpos = cpos.ToMPos(map);
				if (mask[mpos])
					return null;

				if (!CurrentFloodIgnoreHeight && !replaceHeights.Contains(mapHeight[cpos]))
					return null;

				switch (CurrentFloodBehavior)
				{
					case FloodTerrainBehavior.AllTerrain:
						break;

					case FloodTerrainBehavior.TemplateTypes:
						if (!replaceTemplates.Contains(mapTiles[mpos].Type))
							return null;
						break;

					case FloodTerrainBehavior.TerrainTypes:
						if (!replaceTerrain.Contains(map.GetTerrainInfo(cpos).Type))
							return null;

						break;

					case FloodTerrainBehavior.TargetTypes:
						if (!map.GetTerrainInfo(cpos).TargetTypes.Overlaps(replaceTargetTypes))
							return null;

						break;
				}

				if (!CurrentFloodIgnoreActors)
				{
					var actors = editorActorLayer.PreviewsAtCell(cpos).ToArray();
					if ((actors.Length != 0 || !hasEmptyActors) && !actors.Any(a => replaceActors.Contains(a.Type)))
						return null;
				}

				if (++count > CurrentFloodLimit)
					return null;

				mask[mpos] = true;
				return true;
			}

			return EverythingFiller;
		}

		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, CPos mousePosition)
		{
			if (!CanPaint())
				yield break;

			var useGroundHeight = !CurrentReplaceability.HasFlag(MultiBrush.Replaceability.Tile) || CurrentAdaptHeight;

			if (IsFloodFill)
			{
				var preview = EditorBlit.PreviewBlitSource(
					CurrentFloodFillBlitSource,
					currentFilters,
					CVec.Zero,
					wr,
					useGroundHeight);

				foreach (var renderable in preview)
					yield return renderable;
			}
			else if (CurrentShape == ShapeType.Single)
			{
				var offset = GetSingleBlitCellCoordsPos(mousePosition) - CPos.Zero;
				var preview = EditorBlit.PreviewBlitSource(
					CurrentSingleBlitSource,
					currentFilters,
					offset,
					wr,
					useGroundHeight);

				foreach (var renderable in preview)
					yield return renderable;
			}
			else
			{
				var offset = GetCursorBlitCellCoordsPos(mousePosition) - CPos.Zero;
				var preview = EditorBlit.PreviewBlitSource(
					CurrentCursorBlitSource,
					currentFilters,
					offset,
					wr,
					useGroundHeight);
				foreach (var renderable in preview)
					yield return renderable;
			}
		}

		public IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, CPos mousePosition, EditorViewportControllerWidget editorWidget)
		{
			if (!CanPaint())
				yield break;

			if (IsFloodFill)
			{
				yield return new EditorSelectionAnnotationRenderable(
					floodBrushMask.CellRegion.Where(c => floodBrushMask[c]),
					editorWidget.SelectionAltColor,
					editorWidget.SelectionAltOffset,
					CVec.Zero);

				yield return new EditorSelectionAnnotationRenderable(
					floodBrushMask.CellRegion.Where(c => floodBrushMask[c]),
					editorWidget.PasteColor,
					int2.Zero,
					CVec.Zero);
			}
			else if (CurrentShape == ShapeType.Single)
			{
				var offset = GetSingleBlitCellCoordsPos(mousePosition) - CPos.Zero;
				yield return new EditorSelectionAnnotationRenderable(
					targetSingleCellCoords,
					editorWidget.SelectionAltColor,
					editorWidget.SelectionAltOffset,
					offset);

				yield return new EditorSelectionAnnotationRenderable(
					targetSingleCellCoords,
					editorWidget.PasteColor,
					int2.Zero,
					offset);
			}
			else
			{
				var offset = GetCursorBlitCellCoordsPos(mousePosition) - CPos.Zero;
				yield return new EditorSelectionAnnotationRenderable(
					brushMask.CellRegion.Where(c => brushMask[c]),
					editorWidget.SelectionAltColor,
					editorWidget.SelectionAltOffset,
					offset);

				yield return new EditorSelectionAnnotationRenderable(
					brushMask.CellRegion.Where(c => brushMask[c]),
					editorWidget.PasteColor,
					int2.Zero,
					offset);
			}
		}
	}
}
