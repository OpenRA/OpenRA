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
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorViewportControllerWidget : Widget, IWorldTooltipInfo
	{
		public IEditorBrush CurrentBrush { get; private set; }

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly EditorDefaultBrush defaultBrush;
		readonly WorldRenderer worldRenderer;

		public string Label { get; private set; }
		public string Extra { get; private set; }
		public IPlayerSummary Owner { get; private set; }
		public bool ShowOwner { get; private set; }

		bool enableTooltips;

		[ObjectCreator.UseCtor]
		public EditorViewportControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			CurrentBrush = defaultBrush = new EditorDefaultBrush(this, worldRenderer);
		}

		public void ClearBrush() { SetBrush(null); }
		public void SetBrush(IEditorBrush brush)
		{
			if (CurrentBrush != null)
				CurrentBrush.Dispose();

			CurrentBrush = brush ?? defaultBrush;
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

			Label = tooltip;
			Owner = null;
			Extra = null;
			ShowOwner = false;
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "info", this as IWorldTooltipInfo } });
		}

		public void SetTooltip(EditorActorPreview actor)
		{
			if (!enableTooltips)
				return;

			var tooltip = actor.Info.TraitInfoOrDefault<TooltipInfo>();
			if (tooltip != null)
			{
				Label = tooltip.Name;
				ShowOwner = tooltip.IsOwnerRowVisible;
			}
			else
			{
				Label = actor.Info.Name;
				ShowOwner = true;
			}

			Owner = actor.Owner;
			Extra = "ID: " + actor.ID + "\nType: " + actor.Info.Name;
			tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "info", this as IWorldTooltipInfo } });
		}

		public void RemoveTooltip()
		{
			if (enableTooltips)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (CurrentBrush.HandleMouseInput(mi))
				return true;

			return base.HandleMouseInput(mi);
		}

		WPos cachedViewportPosition;
		public override void Tick()
		{
			// Clear any tooltips when the viewport is scrolled using the keyboard
			if (worldRenderer.Viewport.CenterPosition != cachedViewportPosition)
				RemoveTooltip();

			cachedViewportPosition = worldRenderer.Viewport.CenterPosition;
			CurrentBrush.Tick();
		}
	}
}
