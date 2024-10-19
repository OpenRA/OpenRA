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

		readonly World world;
		readonly EditorActorLayer editorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editorWidget;
		readonly WVec centerOffset;
		readonly bool sharesCell;

		CPos cell;
		SubCell subcell = SubCell.Invalid;

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, ActorInfo actor, PlayerReference owner, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			world = wr.World;
			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			centerOffset = (ios as BuildingInfo)?.CenterOffset(world) ?? WVec.Zero;
			sharesCell = ios != null && ios.SharesCell;

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

			Preview = new EditorActorPreview(wr, null, reference, owner);
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
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
				// Check the actor is inside the map
				if (!Preview.Footprint.All(c => world.Map.Tiles.Contains(c.Key)))
					return true;

				var action = new AddActorAction(editorLayer, Preview.Export());
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

			// Take an immutable copy of the reference
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
}
