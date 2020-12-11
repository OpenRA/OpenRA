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
	public enum EditorCursorType { None, Actor, TerrainTemplate, Resource }

	[Desc("Required for the map editor to work. Attach this to the world actor.")]
	public class EditorCursorLayerInfo : TraitInfo, Requires<EditorActorLayerInfo>
	{
		public readonly WAngle PreviewFacing = new WAngle(384);

		public override object Create(ActorInitializer init) { return new EditorCursorLayer(init.Self, this); }
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

		public TerrainTemplateInfo TerrainTemplate { get; private set; }
		public ResourceTypeInfo Resource { get; private set; }
		CPos terrainOrResourceCell;
		bool terrainOrResourceDirty;
		readonly List<IRenderable> terrainOrResourcePreview = new List<IRenderable>();

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

			if (Type == EditorCursorType.TerrainTemplate || Type == EditorCursorType.Resource)
			{
				var cell = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				if (terrainOrResourceCell != cell || terrainOrResourceDirty)
				{
					terrainOrResourceCell = cell;
					terrainOrResourceDirty = false;
					terrainOrResourcePreview.Clear();
					var pos = world.Map.CenterOfCell(cell);

					if (Type == EditorCursorType.TerrainTemplate)
					{
						var i = 0;
						for (var y = 0; y < TerrainTemplate.Size.Y; y++)
						{
							for (var x = 0; x < TerrainTemplate.Size.X; x++)
							{
								var tile = new TerrainTile(TerrainTemplate.Id, (byte)i++);
								if (!world.Map.Rules.TileSet.TryGetTileInfo(tile, out var tileInfo))
									continue;

								var sprite = wr.Theater.TileSprite(tile, 0);
								var offset = world.Map.Offset(new CVec(x, y), tileInfo.Height);
								var palette = wr.Palette(TerrainTemplate.Palette ?? TileSet.TerrainPaletteInternalName);

								terrainOrResourcePreview.Add(new SpriteRenderable(sprite, pos, offset, 0, palette, 1, false));
							}
						}
					}
					else
					{
						var variant = Resource.Sequences.FirstOrDefault();
						var sequence = wr.World.Map.Rules.Sequences.GetSequence("resources", variant);
						var sprite = sequence.GetSprite(Resource.MaxDensity - 1);
						var palette = wr.Palette(Resource.Palette);

						var tintModifiers = sequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;
						terrainOrResourcePreview.Add(new SpriteRenderable(sprite, pos, WVec.Zero, 0, palette, 1, false, tintModifiers));
					}
				}
			}
			else if (Type == EditorCursorType.Actor)
			{
				// Offset mouse position by the center offset (in world pixels)
				var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(actorCenterOffset);
				var cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));
				var subCell = actorSharesCell ? editorLayer.FreeSubCellAt(cell) : SubCell.Invalid;
				var updated = false;
				if (actorLocation != cell)
				{
					actorLocation = cell;
					Actor.ReplaceInit(new LocationInit(cell));
					updated = true;
				}

				if (actorSubCell != subCell)
				{
					actorSubCell = subCell;

					if (Actor.RemoveInits<SubCellInit>() > 0)
						updated = true;

					var subcell = world.Map.Tiles.Contains(cell) ? editorLayer.FreeSubCellAt(cell) : SubCell.Invalid;
					if (subcell != SubCell.Invalid)
					{
						Actor.AddInit(new SubCellInit(subcell));
						updated = true;
					}
				}

				if (updated)
					Actor = new EditorActorPreview(wr, null, Actor.Export(), Actor.Owner);
			}
		}

		static readonly IEnumerable<IRenderable> NoRenderables = Enumerable.Empty<IRenderable>();
		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			if (Type == EditorCursorType.TerrainTemplate || Type == EditorCursorType.Resource)
				return terrainOrResourcePreview;

			if (Type == EditorCursorType.Actor)
				return Actor.Render().OrderBy(WorldRenderer.RenderableZPositionComparisonKey);

			return NoRenderables;
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (wr.World.Type != WorldType.Editor)
				return NoRenderables;

			return Type == EditorCursorType.Actor ? Actor.RenderAnnotations() : NoRenderables;
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
			reference.Add(new OwnerInit(ownerName));
			reference.Add(new FactionInit(owner.Faction));

			var worldPx = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos) - wr.ScreenPxOffset(actorCenterOffset);
			var cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(worldPx));

			reference.Add(new LocationInit(cell));
			if (ios != null && ios.SharesCell)
			{
				actorSubCell = editorLayer.FreeSubCellAt(cell);
				if (actorSubCell != SubCell.Invalid)
					reference.Add(new SubCellInit(actorSubCell));
			}

			if (actor.HasTraitInfo<IFacingInfo>())
				reference.Add(new FacingInit(info.PreviewFacing));

			Type = EditorCursorType.Actor;
			Actor = new EditorActorPreview(wr, null, reference, owner);
			TerrainTemplate = null;
			Resource = null;

			return ++CurrentToken;
		}

		public int SetTerrainTemplate(WorldRenderer wr, TerrainTemplateInfo template)
		{
			terrainOrResourceCell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(Viewport.LastMousePos));

			Type = EditorCursorType.TerrainTemplate;
			TerrainTemplate = template;
			Actor = null;
			Resource = null;
			terrainOrResourceDirty = true;

			return ++CurrentToken;
		}

		public int SetResource(WorldRenderer wr, ResourceTypeInfo resource)
		{
			terrainOrResourceCell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(Viewport.LastMousePos));

			Type = EditorCursorType.Resource;
			Resource = resource;
			Actor = null;
			TerrainTemplate = null;
			terrainOrResourceDirty = true;

			return ++CurrentToken;
		}

		public void Clear(int token)
		{
			if (token != CurrentToken)
				return;

			Type = EditorCursorType.None;
			Actor = null;
			TerrainTemplate = null;
			Resource = null;
		}
	}
}
