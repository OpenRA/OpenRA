#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;
using System.Linq;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class TooltipWorldInteractionControllerWidget : WorldInteractionControllerWidget
	{
		public readonly string TooltipTemplate = "WORLD_TOOLTIP";
		public readonly string TooltipContainer;
		Lazy<TooltipContainerWidget> tooltipContainer;

		public enum WorldTooltipType { None, Unexplored, Actor }
		public WorldTooltipType TooltipType { get; private set; }
		public IToolTip ActorTooltip { get; private set; }

		[ObjectCreator.UseCtor]
		public TooltipWorldInteractionControllerWidget([ObjectCreator.Param] World world,
		                                               [ObjectCreator.Param] WorldRenderer worldRenderer)
			: base(world, worldRenderer)
		{
			tooltipContainer = new Lazy<TooltipContainerWidget>(() =>
				Widget.RootWidget.GetWidget<TooltipContainerWidget>(TooltipContainer));
		}

		public override void MouseEntered()
		{
			if (TooltipContainer == null)
				return;

			tooltipContainer.Value.SetTooltip(
				Widget.LoadWidget(TooltipTemplate, null, new WidgetArgs() {{ "world", world }, { "wic", this }}));
		}

		public override void MouseExited()
		{
			if (TooltipContainer == null) return;
			tooltipContainer.Value.RemoveTooltip();
		}

		public void UpdateMouseover()
		{
			TooltipType = WorldTooltipType.None;
			var cell = Game.viewport.ViewToWorld(Viewport.LastMousePos).ToInt2();
			if (!world.Map.IsInMap(cell))
				return;

			if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.IsExplored(cell))
			{
				TooltipType = WorldTooltipType.Unexplored;
				return;
			}

			var actor = world.FindUnitsAtMouse(Viewport.LastMousePos).FirstOrDefault();
			if (actor == null)
				return;

			ActorTooltip = actor.TraitsImplementing<IToolTip>().FirstOrDefault();
			if (ActorTooltip != null)
				TooltipType = WorldTooltipType.Actor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				UpdateMouseover();

			return base.HandleMouseInput(mi);
		}

		float2 cachedLocation;
		public override void Tick()
		{
			if (Game.viewport.Location != cachedLocation)
			{
				UpdateMouseover();
				cachedLocation = Game.viewport.Location;
			}
			base.Tick();
		}
	}
}