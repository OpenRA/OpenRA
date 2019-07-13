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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorActorBrush : IEditorBrush
	{
		public readonly ActorInfo Actor;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorActorLayer editorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorViewportControllerWidget editorWidget;
		readonly ActorPreviewWidget preview;
		readonly WVec centerOffset;
		readonly PlayerReference owner;
		readonly CVec[] footprint;

		int facing = 96;

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, ActorInfo actor, PlayerReference owner, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			Actor = actor;
			this.owner = owner;
			var ownerName = owner.Name;

			preview = editorWidget.Get<ActorPreviewWidget>("DRAG_ACTOR_PREVIEW");
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;

			var buildingInfo = actor.TraitInfoOrDefault<BuildingInfo>();
			if (buildingInfo != null)
				centerOffset = buildingInfo.CenterOffset(world);

			// Enforce first entry of ValidOwnerNames as owner if the actor has RequiresSpecificOwners
			var specificOwnerInfo = actor.TraitInfoOrDefault<RequiresSpecificOwnersInfo>();
			if (specificOwnerInfo != null && !specificOwnerInfo.ValidOwnerNames.Contains(ownerName))
				ownerName = specificOwnerInfo.ValidOwnerNames.First();

			var td = new TypeDictionary();
			td.Add(new FacingInit(facing));
			td.Add(new TurretFacingInit(facing));
			td.Add(new OwnerInit(ownerName));
			td.Add(new FactionInit(owner.Faction));
			preview.SetPreview(actor, td);

			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (ios != null)
				footprint = ios.OccupiedCells(actor, CPos.Zero)
					.Select(c => c.Key - CPos.Zero)
					.ToArray();
			else
				footprint = new CVec[0];

			// The preview widget may be rendered by the higher-level code before it is ticked.
			// Force a manual tick to ensure the bounds are set correctly for this first draw.
			Tick();
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

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location - worldRenderer.ScreenPxOffset(centerOffset));
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				// Check the actor is inside the map
				if (!footprint.All(c => world.Map.Tiles.Contains(cell + c)))
					return true;

				// Enforce first entry of ValidOwnerNames as owner if the actor has RequiresSpecificOwners
				var action = new AddActorAction(editorLayer, Actor, cell, owner, facing);
				editorActionManager.Add(action);
			}

			return true;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos - worldRenderer.ScreenPxOffset(centerOffset));
			var pos = world.Map.CenterOfCell(cell) + centerOffset;

			var origin = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPxPosition(pos));

			var zoom = worldRenderer.Viewport.Zoom;
			var s = preview.IdealPreviewSize;
			var o = preview.PreviewOffset;
			preview.Bounds.X = origin.X - (int)(zoom * (o.X + s.X / 2));
			preview.Bounds.Y = origin.Y - (int)(zoom * (o.Y + s.Y / 2));
			preview.Bounds.Width = (int)(zoom * s.X);
			preview.Bounds.Height = (int)(zoom * s.Y);
		}

		public void Dispose() { }
	}

	class AddActorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly EditorActorLayer editorLayer;
		readonly ActorInfo actor;
		readonly CPos cell;
		readonly PlayerReference owner;
		readonly int facing;

		EditorActorPreview editorActorPreview;

		public AddActorAction(EditorActorLayer editorLayer, ActorInfo actor, CPos cell, PlayerReference owner, int facing)
		{
			this.editorLayer = editorLayer;
			this.actor = actor;
			this.cell = cell;
			this.owner = owner;
			this.facing = facing;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var ownerName = owner.Name;
			var specificOwnerInfo = actor.TraitInfoOrDefault<RequiresSpecificOwnersInfo>();
			if (specificOwnerInfo != null && !specificOwnerInfo.ValidOwnerNames.Contains(ownerName))
				ownerName = specificOwnerInfo.ValidOwnerNames.First();

			var newActorReference = new ActorReference(actor.Name);
			newActorReference.Add(new OwnerInit(ownerName));

			newActorReference.Add(new LocationInit(cell));

			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			if (ios != null && ios.SharesCell)
			{
				var subcell = editorLayer.FreeSubCellAt(cell);
				if (subcell != SubCell.Invalid)
					newActorReference.Add(new SubCellInit(subcell));
			}

			var initDict = newActorReference.InitDict;

			if (actor.HasTraitInfo<IFacingInfo>())
				initDict.Add(new FacingInit(facing));

			if (actor.HasTraitInfo<TurretedInfo>())
				initDict.Add(new TurretFacingInit(facing));

			editorActorPreview = editorLayer.Add(newActorReference);

			Text = "Added {0} ({1})".F(editorActorPreview.Info.Name, editorActorPreview.ID);
		}

		public void Undo()
		{
			editorLayer.Remove(editorActorPreview);
		}
	}
}
