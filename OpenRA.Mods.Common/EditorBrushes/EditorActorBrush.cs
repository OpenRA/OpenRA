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
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorActorBrush : IEditorBrush
	{
		public List<EditorActorPreview> Previews = [];

		readonly WorldRenderer worldRenderer;
		readonly EditorActorLayer editorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editorWidget;
		readonly WVec centerOffset;
		readonly bool sharesCell;

		readonly BuildingInfo buildingInfo;
		readonly int2 dimentions;
		readonly LineBuildNodeInfo lineBuildInfo;

		CPos cell;
		SubCell subcell = SubCell.Invalid;

		bool shiftHeldDown;
		bool dragging;
		CPos startCell;

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, ActorInfo actor, PlayerReference owner, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			var world = wr.World;
			worldRenderer = wr;
			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			buildingInfo = ios as BuildingInfo;
			centerOffset = buildingInfo?.CenterOffset(world) ?? WVec.Zero;

			dimentions = buildingInfo != null
				? new int2(buildingInfo.Dimensions.X, buildingInfo.Dimensions.Y)
				: new int2(1, 1);

			sharesCell = ios != null && ios.SharesCell;
			lineBuildInfo = actor.TraitInfoOrDefault<LineBuildNodeInfo>();

			// Enforce first entry of ValidOwnerNames as owner if the actor has RequiresSpecificOwners.
			var ownerName = owner.Name;
			var specificOwnerInfo = actor.TraitInfoOrDefault<RequiresSpecificOwnersInfo>();
			if (specificOwnerInfo != null && !specificOwnerInfo.ValidOwnerNames.Contains(ownerName))
				ownerName = specificOwnerInfo.ValidOwnerNames.First();

			var reference = new ActorReference(actor.Name)
			{
				new OwnerInit(ownerName),
				new FactionInit(owner.Faction)
			};

			var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(centerOffset);
			cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));
			reference.Add(new LocationInit(cell));
			if (sharesCell)
			{
				subcell = editorLayer.FreeSubCellAt(cell);
				if (subcell != SubCell.Invalid)
					reference.Add(new SubCellInit(subcell));
			}

			if (actor.HasTraitInfo<IFacingInfo>())
				reference.Add(new FacingInit(editorLayer.Info.DefaultActorFacing));

			Previews.Add(new EditorActorPreview(wr, null, reference, owner));
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
			{
				// Offset mouse position by the center offset (in world pixels)
				var worldPx = worldRenderer.Viewport.ViewToWorldPx(Viewport.LastMousePos) - worldRenderer.ScreenPxOffset(centerOffset);
				var currentCell = worldRenderer.Viewport.ViewToWorld(worldRenderer.Viewport.WorldToViewPx(worldPx));
				var currentSubcell = sharesCell ? editorLayer.FreeSubCellAt(currentCell) : SubCell.Invalid;
				if (cell != currentCell || subcell != currentSubcell)
				{
					cell = currentCell;
					if (sharesCell)
						subcell = editorLayer.FreeSubCellAt(cell);

					UpdateLine();
				}
			}

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

			if (mi.Button != MouseButton.Left)
				return true;

			if (mi.Event == MouseInputEvent.Down)
			{
				startCell = cell;
				dragging = true;
				UpdateLine();
			}

			if (mi.Event == MouseInputEvent.Up)
			{
				UpdateLine(true);

				var actors = Previews
					.Where(p => p.Footprint.All(c => worldRenderer.World.Map.Tiles.Contains(c.Key)))
					.Select(p => p.Export())
					.ToImmutableArray();

				if (actors.Length != 0)
					editorActionManager.Add(new AddActorsAction(editorLayer, actors));

				dragging = false;
				UpdateLine();
			}

			return true;
		}

		public bool HandleKeyboardInput(KeyInput ki)
		{
			if (ki.Key == Keycode.LSHIFT || ki.Key == Keycode.RSHIFT)
			{
				shiftHeldDown = ki.Event == KeyInputEvent.Down;
				UpdateLine();
			}

			return false;
		}

		public void UpdatePreviewsOwner(PlayerReference owner)
		{
			foreach (var preview in Previews)
			{
				preview.Owner = owner;
				preview.ReplaceInit(new OwnerInit(owner.Name));
				preview.ReplaceInit(new FactionInit(owner.Faction));
			}
		}

		// PERF: reduce allocations by reusing the list.
		readonly List<CPos> cells = [];
		void UpdateLine(bool commiting = false)
		{
			cells.Clear();
			if (dragging && shiftHeldDown)
				cells.AddRange(Util.GetCurvedLine(startCell, cell, dimentions));
			else if (dragging)
			{
				cells.Add(startCell);

				// If the user placed an actor, we want it to feel responsive.
				if (!commiting && cell != startCell)
					cells.Add(cell);
			}
			else
				cells.Add(cell);

			var basePreview = Previews[0];
			var currentPreviews = Previews.Count;
			var needToUpdate = cells.Count;
			if (cells.Count > currentPreviews)
			{
				for (var i = currentPreviews; i < cells.Count; i++)
				{
					var cell = cells[i];
					var reference = basePreview.Export();
					reference.Replace(new LocationInit(cell));
					if (sharesCell)
					{
						subcell = editorLayer.FreeSubCellAt(cell);
						if (subcell == SubCell.Invalid)
							reference.RemoveAll<SubCellInit>();
						else
							reference.Replace(new SubCellInit(subcell));
					}

					Previews.Add(new EditorActorPreview(worldRenderer, null, reference, basePreview.Owner));
				}

				needToUpdate = currentPreviews;
			}
			else if (cells.Count < currentPreviews)
				for (var i = currentPreviews - 1; i >= cells.Count; i--)
					Previews.RemoveAt(i);

			for (var i = 0; i < needToUpdate; i++)
			{
				var cell = cells[i];
				var preview = Previews[i];
				preview.ReplaceInit(new LocationInit(cell));
				if (sharesCell)
				{
					subcell = editorLayer.FreeSubCellAt(cell);
					if (subcell == SubCell.Invalid)
						preview.RemoveInit<SubCellInit>();
					else
						preview.ReplaceInit(new SubCellInit(subcell));
				}

				preview.UpdateFromMove();
			}

			if (lineBuildInfo == null)
				return;

			// It's nicer when previews can connect.
			for (var i = 0; i < Previews.Count; i++)
			{
				var dict = new Dictionary<CPos, string[]>();
				if (i != 0)
				{
					var neighbour = Previews[i - 1];
					dict[neighbour.Location] = [neighbour.Info.Name];
				}

				if (i != Previews.Count - 1)
				{
					var neighbour = Previews[i + 1];
					dict[neighbour.Location] = [neighbour.Info.Name];
				}

				Previews[i].ReplaceInit(new RuntimeNeighbourInit(dict));
			}
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return Previews.SelectMany(p => p.Render()).OrderBy(WorldRenderer.RenderableZPositionComparisonKey);
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return Previews.SelectMany(p => p.RenderAnnotations()).OrderBy(WorldRenderer.RenderableZPositionComparisonKey);
		}

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class AddActorsAction : IEditorAction
	{
		public string Text { get; private set; }

		[FluentReference("name", "id", "count")]
		const string AddedActors = "notification-added-actors";

		readonly EditorActorLayer editorLayer;
		readonly ImmutableArray<ActorReference> actor;

		ImmutableArray<EditorActorPreview> editorActorPreviews;

		public AddActorsAction(EditorActorLayer editorLayer, ImmutableArray<ActorReference> actor)
		{
			this.editorLayer = editorLayer;
			this.actor = actor;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			editorActorPreviews = editorLayer.AddRange(actor.AsSpan()).ToImmutableArray();
			Text = FluentProvider.GetMessage(AddedActors,
				"name", editorActorPreviews[0].Info.Name,
				"id", editorActorPreviews[0].ID,
				"count", editorActorPreviews.Length);
		}

		public void Undo()
		{
			editorLayer.RemoveRange(editorActorPreviews.AsSpan());
		}
	}
}
