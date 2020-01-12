#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public enum EditorCursorType { None, Actor }

	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorCursorLayerInfo : ITraitInfo, Requires<EditorActorLayerInfo>
	{
		public readonly int PreviewFacing = 96;

		public object Create(ActorInitializer init) { return new EditorCursorLayer(init.Self, this); }
	}

	public class EditorCursorLayer : ITickRender, IRenderAboveShroud, IRenderAnnotations
	{
		readonly EditorCursorLayerInfo info;
		readonly EditorActorLayer editorLayer;
		readonly World world;

		public int CurrentToken { get; private set; }
		public EditorCursorType Type { get; private set; }
		public EditorActorPreview Actor { get; private set; }
		CPos actorLocation;
		SubCell actorSubCell;
		WVec actorCenterOffset;
		bool actorSharesCell;

		public EditorCursorLayer(Actor self, EditorCursorLayerInfo info)
		{
			this.info = info;
			world = self.World;
			editorLayer = self.Trait<EditorActorLayer>();

			Type = EditorCursorType.None;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			if (wr.World.Type != WorldType.Editor)
				return;

			if (Actor != null)
			{
				// Offset mouse position by the center offset (in world pixels)
				var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(actorCenterOffset);
				var cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));
				var subCell = actorSharesCell ? editorLayer.FreeSubCellAt(cell) : SubCell.Invalid;
				var updated = false;
				if (actorLocation != cell)
				{
					actorLocation = cell;
					Actor.Actor.InitDict.Remove(Actor.Actor.InitDict.Get<LocationInit>());
					Actor.Actor.InitDict.Add(new LocationInit(cell));
					updated = true;
				}

				if (actorSubCell != subCell)
				{
					actorSubCell = subCell;

					var subcellInit = Actor.Actor.InitDict.GetOrDefault<SubCellInit>();
					if (subcellInit != null)
					{
						Actor.Actor.InitDict.Remove(subcellInit);
						updated = true;
					}

					var subcell = world.Map.Tiles.Contains(cell) ? editorLayer.FreeSubCellAt(cell) : SubCell.Invalid;
					if (subcell != SubCell.Invalid)
					{
						Actor.Actor.InitDict.Add(new SubCellInit(subcell));
						updated = true;
					}
				}

				if (updated)
					Actor = new EditorActorPreview(wr, null, Actor.Actor, Actor.Owner);
			}
		}

		static readonly IEnumerable<IRenderable> NoRenderables = Enumerable.Empty<IRenderable>();
		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			if (Type == EditorCursorType.Actor)
				return Actor.Render().OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey);

			return NoRenderables;
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return Actor != null ? Actor.RenderAnnotations() : NoRenderables;
		}

		bool IRenderAnnotations.SpatiallyPartitionable { get { return false; } }

		public int SetActor(WorldRenderer wr, ActorInfo actor, PlayerReference owner)
		{
			var ios = actor.TraitInfoOrDefault<IOccupySpaceInfo>();
			var buildingInfo = ios as BuildingInfo;
			actorCenterOffset = buildingInfo != null ? buildingInfo.CenterOffset(world) : WVec.Zero;
			actorSharesCell = ios != null && ios.SharesCell;
			actorSubCell = SubCell.Invalid;

			// Enforce first entry of ValidOwnerNames as owner if the actor has RequiresSpecificOwners
			var ownerName = owner.Name;
			var specificOwnerInfo = actor.TraitInfoOrDefault<RequiresSpecificOwnersInfo>();
			if (specificOwnerInfo != null && !specificOwnerInfo.ValidOwnerNames.Contains(ownerName))
				ownerName = specificOwnerInfo.ValidOwnerNames.First();

			var reference = new ActorReference(actor.Name);
			reference.InitDict.Add(new OwnerInit(ownerName));
			reference.InitDict.Add(new FactionInit(owner.Faction));

			var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(actorCenterOffset);
			var cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));

			reference.InitDict.Add(new LocationInit(cell));
			if (ios != null && ios.SharesCell)
			{
				actorSubCell = editorLayer.FreeSubCellAt(cell);
				if (actorSubCell != SubCell.Invalid)
					reference.InitDict.Add(new SubCellInit(actorSubCell));
			}

			if (actor.HasTraitInfo<IFacingInfo>())
				reference.InitDict.Add(new FacingInit(info.PreviewFacing));

			if (actor.HasTraitInfo<TurretedInfo>())
				reference.InitDict.Add(new TurretFacingInit(info.PreviewFacing));

			Type = EditorCursorType.Actor;
			Actor = new EditorActorPreview(wr, null, reference, owner);

			return ++CurrentToken;
		}

		public void Clear(int token)
		{
			if (token != CurrentToken)
				return;

			Type = EditorCursorType.None;
			Actor = null;
		}
	}
}
