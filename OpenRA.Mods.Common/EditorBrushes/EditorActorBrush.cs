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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorActorBrush : IEditorBrush
	{
		public EditorActorPreview Preview;
		public readonly ActorInfo[] Actors;
		public PlayerReference Owner { get; private set; }
		public IEnumerable<ActorReference> ActorReferences => Actors.Select(a => CreateActorReference(a, cell, SubCellForActor(a, cell)));

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorActorLayer editorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editorWidget;
		readonly WVec centerOffset;
		readonly bool sharesCell;

		CPos cell;
		SubCell subcell = SubCell.Invalid;
		int nextActor;

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, ActorInfo actor, PlayerReference owner, WorldRenderer wr)
			: this(editorWidget, [actor], owner, wr) { }

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, IEnumerable<ActorInfo> actors, PlayerReference owner, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			Actors = actors.Distinct().ToArray();
			Owner = owner;
			var actor = Actors[0];

			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			centerOffset = (ios as BuildingInfo)?.CenterOffset(world) ?? WVec.Zero;
			sharesCell = ios != null && ios.SharesCell;

			var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(centerOffset);
			cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));
			subcell = sharesCell ? editorLayer.FreeSubCellAt(cell) : SubCell.Invalid;
			var reference = CreateActorReference(actor, cell, subcell);

			Preview = new EditorActorPreview(wr, null, reference, owner);
		}

		public void SetOwner(PlayerReference owner)
		{
			Owner = owner;
			var reference = CreateActorReference(Preview.Info, cell, subcell);
			Preview = new EditorActorPreview(worldRenderer, null, reference, owner);
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else.
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				var actor = PickActor();
				var selectedCell = CellForActor(actor, mi.Location);
				var selectedSubcell = SubCellForActor(actor, selectedCell);
				var reference = CreateActorReference(actor, selectedCell, selectedSubcell);

				if (!Footprint(actor, reference).All(world.Map.Tiles.Contains))
					return true;

				var action = new AddActorAction(editorLayer, reference);
				editorActionManager.Add(action);
			}

			return true;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self)
		{
			// Offset mouse position by the center offset (in world pixels)
			var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(centerOffset);
			var currentCell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));
			var currentSubcell = sharesCell ? editorLayer.FreeSubCellAt(currentCell) : SubCell.Invalid;
			if (cell != currentCell || subcell != currentSubcell)
			{
				cell = currentCell;
				Preview.ReplaceInit(new LocationInit(cell));

				if (sharesCell)
				{
					subcell = editorLayer.FreeSubCellAt(cell);
					if (subcell == SubCell.Invalid)
						Preview.RemoveInit<SubCellInit>();
					else
						Preview.ReplaceInit(new SubCellInit(subcell));
				}

				Preview.UpdateFromMove();
			}
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return Preview.Render().OrderBy(WorldRenderer.RenderableZPositionComparisonKey);
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return Preview.RenderAnnotations();
		}

		public void Tick() { }

		public void Dispose() { }

		ActorInfo PickActor()
		{
			if (Actors.Length == 1)
				return Actors[0];

			if (editorWidget.AssetMixMode == EditorAssetMixMode.Sequential)
				return Actors[nextActor++ % Actors.Length];

			return Actors[Game.CosmeticRandom.Next(Actors.Length)];
		}

		CPos CellForActor(ActorInfo actor, int2 location)
		{
			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			var offset = (ios as BuildingInfo)?.CenterOffset(world) ?? WVec.Zero;
			var worldPx = worldRenderer.Viewport.ViewToWorldPx(location) - worldRenderer.ScreenPxOffset(offset);
			return worldRenderer.Viewport.ViewToWorld(worldRenderer.Viewport.WorldToViewPx(worldPx));
		}

		SubCell SubCellForActor(ActorInfo actor, CPos location)
		{
			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			return ios != null && ios.SharesCell ? editorLayer.FreeSubCellAt(location) : SubCell.Invalid;
		}

		ActorReference CreateActorReference(ActorInfo actor, CPos location, SubCell subcell)
		{
			var ownerName = Owner.Name;
			var specificOwnerInfo = actor.TraitInfoOrDefault<RequiresSpecificOwnersInfo>();
			if (specificOwnerInfo != null && !specificOwnerInfo.ValidOwnerNames.Contains(ownerName))
				ownerName = specificOwnerInfo.ValidOwnerNames.First();

			var reference = new ActorReference(actor.Name)
			{
				new OwnerInit(ownerName),
				new FactionInit(Owner.Faction),
				new LocationInit(location)
			};

			if (subcell != SubCell.Invalid)
				reference.Add(new SubCellInit(subcell));

			if (actor.HasTraitInfo<IFacingInfo>())
				reference.Add(new FacingInit(editorLayer.Info.DefaultActorFacing));

			return reference;
		}

		static IEnumerable<CPos> Footprint(ActorInfo actorInfo, ActorReference actor)
		{
			var occupySpaceInfo = actorInfo.TraitInfoOrDefault<IOccupySpaceInfo>();
			var location = actor.Get<LocationInit>().Value;
			var subCellInit = actor.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

			return occupySpaceInfo?.OccupiedCells(actorInfo, location, subCell).Keys ?? [location];
		}
	}

	sealed class AddActorAction : IEditorAction
	{
		public string Text { get; private set; }

		[FluentReference("name", "id")]
		const string AddedActor = "notification-added-actor";

		readonly EditorActorLayer editorLayer;
		readonly ActorReference actor;

		EditorActorPreview editorActorPreview;

		public AddActorAction(EditorActorLayer editorLayer, ActorReference actor)
		{
			this.editorLayer = editorLayer;

			// Take an immutable copy of the reference.
			this.actor = actor.Clone();
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			editorActorPreview = editorLayer.Add(actor);
			Text = FluentProvider.GetMessage(AddedActor,
				"name", editorActorPreview.Info.Name,
				"id", editorActorPreview.ID);
		}

		public void Undo()
		{
			editorLayer.Remove(editorActorPreview);
		}
	}

	sealed class FillSelectionWithActorEditorAction : IEditorAction
	{
		public string Text { get; private set; }

		[FluentReference("name", "count")]
		const string FilledActors = "notification-filled-actors";

		readonly EditorActorLayer editorLayer;
		readonly ActorReference[] actors;
		readonly EditorAssetMixMode mixMode;
		readonly Map map;
		readonly CellCoordsRegion area;
		readonly IReadOnlySet<CPos> mask;

		readonly List<EditorActorPreview> editorActorPreviews = [];
		int nextActor;

		public FillSelectionWithActorEditorAction(EditorActorLayer editorLayer, ActorReference actor, Map map, CellCoordsRegion area, IReadOnlySet<CPos> mask = null)
			: this(editorLayer, [actor], EditorAssetMixMode.Random, map, area, mask) { }

		public FillSelectionWithActorEditorAction(
			EditorActorLayer editorLayer,
			IEnumerable<ActorReference> actors,
			EditorAssetMixMode mixMode,
			Map map,
			CellCoordsRegion area,
			IReadOnlySet<CPos> mask = null)
		{
			this.editorLayer = editorLayer;
			this.actors = actors.Select(a => a.Clone()).ToArray();
			this.mixMode = mixMode;
			this.map = map;
			this.area = area;
			this.mask = mask;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var actors = new List<ActorReference>();
			var firstActorInfo = map.Rules.Actors[this.actors[0].Type.ToLowerInvariant()];

			if (mask != null)
			{
				foreach (var cell in mask)
					PlaceActorAt(cell, actors);
			}
			else
			{
				foreach (var cell in area)
					PlaceActorAt(cell, actors);
			}

			foreach (var actorAtCell in actors)
				editorActorPreviews.Add(editorLayer.Add(actorAtCell));

			Text = FluentProvider.GetMessage(FilledActors, "name", firstActorInfo.Name, "count", editorActorPreviews.Count);
		}

		void PlaceActorAt(CPos cell, List<ActorReference> actors)
		{
			var actorAtCell = PickActor().Clone();
			var actorInfo = map.Rules.Actors[actorAtCell.Type.ToLowerInvariant()];
			var occupySpaceInfo = actorInfo.TraitInfoOrDefault<IOccupySpaceInfo>();
			var sharesCell = occupySpaceInfo != null && occupySpaceInfo.SharesCell;

			actorAtCell.Replace(new LocationInit(cell));

			if (sharesCell)
			{
				var subcell = editorLayer.FreeSubCellAt(cell);
				if (subcell == SubCell.Invalid)
					return;

				actorAtCell.Replace(new SubCellInit(subcell));
			}

			if (!Footprint(actorInfo, occupySpaceInfo, actorAtCell).All(map.Tiles.Contains))
				return;

			actors.Add(actorAtCell);
		}

		public void Undo()
		{
			foreach (var editorActorPreview in editorActorPreviews)
				editorLayer.Remove(editorActorPreview);

			editorActorPreviews.Clear();
		}

		ActorReference PickActor()
		{
			if (actors.Length == 1)
				return actors[0];

			if (mixMode == EditorAssetMixMode.Sequential)
				return actors[nextActor++ % actors.Length];

			return actors[Game.CosmeticRandom.Next(actors.Length)];
		}

		static IEnumerable<CPos> Footprint(ActorInfo actorInfo, IOccupySpaceInfo occupySpaceInfo, ActorReference actor)
		{
			var location = actor.Get<LocationInit>().Value;
			var subCellInit = actor.GetOrDefault<SubCellInit>();
			var subCell = subCellInit != null ? subCellInit.Value : SubCell.Any;

			return occupySpaceInfo?.OccupiedCells(actorInfo, location, subCell).Keys ?? [location];
		}
	}
}
