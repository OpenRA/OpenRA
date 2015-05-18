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
	public interface IEditorBrush
	{
		bool HandleMouseInput(MouseInput mi);
		void Tick();
	}

	public class EditorDefaultBrush : IEditorBrush
	{
		public readonly ActorInfo Actor;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActorLayer editorLayer;
		readonly Dictionary<int, ResourceType> resources;

		public EditorDefaultBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resources = world.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
			// Mouse move events are important for tooltips, so we always allow these through
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right && mi.Event != MouseInputEvent.Move)
				return false;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			if (mi.Event == MouseInputEvent.Up)
				return true;

			var underCursor = editorLayer.PreviewsAt(worldRenderer.Viewport.ViewToWorldPx(mi.Location))
				.FirstOrDefault();

			ResourceType type;
			if (underCursor != null)
				editorWidget.SetTooltip(underCursor.Tooltip);
			else if (world.Map.Contains(cell) && resources.TryGetValue(world.Map.MapResources.Value[cell].Type, out type))
				editorWidget.SetTooltip(type.Info.Name);
			else
				editorWidget.SetTooltip(null);

			// Finished with mouse move events, so let them bubble up the widget tree
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				editorWidget.SetTooltip(null);

				if (underCursor != null)
					editorLayer.Remove(underCursor);

				if (world.Map.MapResources.Value[cell].Type != 0)
					world.Map.MapResources.Value[cell] = new ResourceTile();
			}
			else if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (underCursor != null)
				{
					// Test case / demonstration of how to edit an existing actor
					var facing = underCursor.Init<FacingInit>();
					if (facing != null)
						underCursor.ReplaceInit(new FacingInit((facing.Value(world) + 32) % 256));
					else if (underCursor.Info.Traits.WithInterface<UsesInit<FacingInit>>().Any())
						underCursor.ReplaceInit(new FacingInit(32));

					var turret = underCursor.Init<TurretFacingInit>();
					if (turret != null)
						underCursor.ReplaceInit(new TurretFacingInit((turret.Value(world) + 32) % 256));
					else if (underCursor.Info.Traits.WithInterface<UsesInit<TurretFacingInit>>().Any())
						underCursor.ReplaceInit(new TurretFacingInit(32));
				}
			}

			return true;
		}

		public void Tick() { }
	}
}
