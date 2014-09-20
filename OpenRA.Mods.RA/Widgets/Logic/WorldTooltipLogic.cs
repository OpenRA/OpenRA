#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class WorldTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, ViewportControllerWidget viewport)
		{
			widget.IsVisible = () => viewport.TooltipType != WorldTooltipType.None;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var owner = widget.Get<LabelWidget>("OWNER");
			var refund = widget.Get<LabelWidget>("REFUND");

			var font = Game.Renderer.Fonts[label.Font];
			var ownerFont = Game.Renderer.Fonts[owner.Font];
			var labelText = "";
			var refundText = "";
			var showOwner = false;
			var showRefund = false;
			var flagRace = "";
			var ownerName = "";
			var ownerColor = Color.White;

			var singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			var doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;
			var tripleHeight = widget.Get("TRIPLE_HEIGHT").Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				if (viewport == null || viewport.TooltipType == WorldTooltipType.None)
					return;

				Player o = null;
				switch (viewport.TooltipType)
				{
				case WorldTooltipType.Unexplored:
					showRefund = false;
					labelText = "Unexplored Terrain";
					break;
				case WorldTooltipType.Actor:
					labelText = viewport.ActorTooltip.Name();
					o = viewport.ActorTooltip.Owner();
					showRefund = false;
					break;
				case WorldTooltipType.SellableActor:
					labelText = viewport.ActorTooltip.Name();
					o = viewport.ActorTooltip.Owner();
					var TooltipActor = viewport.TooltipActor;
					var h = TooltipActor.TraitOrDefault<Health>();
					var si = TooltipActor.Info.Traits.Get<SellableInfo>();
					var cost = TooltipActor.GetSellValue();
					var refundamount = (cost * si.RefundPercent * (h == null ? 1 : h.HP)) / (100 * (h == null ? 1 : h.MaxHP));
					refundText = "Refund: $" + refundamount;
					showRefund = true;
					break;
				case WorldTooltipType.FrozenActor:
					labelText = viewport.FrozenActorTooltip.TooltipName;
					o = viewport.FrozenActorTooltip.TooltipOwner;
					showRefund = false;
					break;
				}

				var labeltextWidth = font.Measure(labelText).X;
				var refundtextWidth = font.Measure(refundText).X;
				var ownertextWidth = ownerFont.Measure(ownerName).X;

				label.Bounds.Width = labeltextWidth;
				refund.Bounds.Width = refundtextWidth;

				if (labeltextWidth >= refundtextWidth)
					widget.Bounds.Width = 2 * label.Bounds.X + labeltextWidth;
				else
					widget.Bounds.Width = 2 * refund.Bounds.X + refundtextWidth;

				showOwner = o != null && !o.NonCombatant;

				if (showOwner)
				{
					flagRace = o.Country.Race;
					ownerName = o.PlayerName;
					ownerColor = o.Color.RGB;
					if (!showRefund)
						widget.Bounds.Height = doubleHeight;

					widget.Bounds.Width = Math.Max(widget.Bounds.Width,
						owner.Bounds.X + ownertextWidth + label.Bounds.X);
				}
				else
				{
					widget.Bounds.Height = singleHeight;
					showRefund = false;
				}

				if (showRefund)
					widget.Bounds.Height = tripleHeight;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => showOwner;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => flagRace;
			refund.IsVisible = () => showRefund;
			refund.GetText = () => refundText;
			owner.IsVisible = () => showOwner;
			owner.GetText = () => ownerName;
			owner.GetColor = () => ownerColor;
			owner.Contrast = true;
		}
	}
}

