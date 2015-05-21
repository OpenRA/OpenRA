#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorActorBrush : IEditorBrush
	{
		public readonly ActorInfo Actor;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorActorLayer editorLayer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly ActorPreviewWidget preview;
		readonly CVec locationOffset;
		readonly WVec previewOffset;
		readonly PlayerReference owner;

		int facing = 92;

		public EditorActorBrush(EditorViewportControllerWidget editorWidget, ActorInfo actor, PlayerReference owner, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			editorLayer = world.WorldActor.Trait<EditorActorLayer>();

			Actor = actor;
			this.owner = owner;

			preview = editorWidget.Get<ActorPreviewWidget>("DRAG_ACTOR_PREVIEW");
			preview.GetScale = () => worldRenderer.Viewport.Zoom;
			preview.IsVisible = () => editorWidget.CurrentBrush == this;

			var buildingInfo = actor.Traits.GetOrDefault<BuildingInfo>();
			if (buildingInfo != null)
			{
				locationOffset = -FootprintUtils.AdjustForBuildingSize(buildingInfo);
				previewOffset = FootprintUtils.CenterOffset(world, buildingInfo);
			}

			var td = new TypeDictionary();
			td.Add(new FacingInit(facing));
			td.Add(new TurretFacingInit(facing));
			td.Add(new OwnerInit(owner.Name));
			td.Add(new RaceInit(owner.Race));
			preview.SetPreview(actor, td);

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
				editorWidget.ClearBrush();
				return true;
			}

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down && world.Map.Contains(cell))
			{
				var newActorReference = new ActorReference(Actor.Name);
				newActorReference.Add(new OwnerInit(owner.Name));

				cell += locationOffset;
				newActorReference.Add(new LocationInit(cell));

				var ios = Actor.Traits.GetOrDefault<IOccupySpaceInfo>();
				if (ios != null && ios.SharesCell)
				{
					var subcell = editorLayer.FreeSubCellAt(cell);
					if (subcell != SubCell.Invalid)
						newActorReference.Add(new SubCellInit(subcell));
				}

				var initDict = newActorReference.InitDict;

				if (Actor.Traits.Contains<IFacingInfo>())
					initDict.Add(new FacingInit(facing));

				if (Actor.Traits.Contains<TurretedInfo>())
					initDict.Add(new TurretFacingInit(facing));

				editorLayer.Add(newActorReference);
			}

			return true;
		}

		public void Tick()
		{
			var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			var pos = world.Map.CenterOfCell(cell + locationOffset) + previewOffset;

			var origin = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPxPosition(pos));

			var zoom = worldRenderer.Viewport.Zoom;
			var s = preview.IdealPreviewSize;
			var o = preview.PreviewOffset;
			preview.Bounds.X = origin.X - (int)(zoom * (o.X + s.X / 2));
			preview.Bounds.Y = origin.Y - (int)(zoom * (o.Y + s.Y / 2));
			preview.Bounds.Width = (int)(zoom * s.X);
			preview.Bounds.Height = (int)(zoom * s.Y);
		}
	}
}
