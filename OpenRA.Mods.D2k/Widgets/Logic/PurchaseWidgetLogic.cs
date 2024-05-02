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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class PurchseWidgetLogic : ChromeLogic
	{
		[TranslationReference("time")]
		const string DeliveryIn = "label-deliver-in-timer";
		string time = "";
		readonly World world;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		readonly Lazy<Widget> backgroundBottom;
		readonly ImageWidget bottomExtension;
		readonly ButtonWidget purchaseButton;
		readonly LabelWidget deliveryLabel;

		[ObjectCreator.UseCtor]
		public PurchseWidgetLogic(Widget widget, World world)
		{
			this.world = world;
			backgroundBottom = Exts.Lazy(() => Ui.Root.Get("BOTTOM_CAP"));
			purchaseButton = widget.Get<ButtonWidget>("PURCHASE_BUTTON");
			bottomExtension = widget.Get<ImageWidget>("BOTTOM_EXTENSION");
			deliveryLabel = widget.Get<LabelWidget>("DELIVERY_IN");
			var textCache = new CachedTransform<string, string>(s => TranslationProvider.GetString(DeliveryIn, Translation.Arguments("time", time)));
			deliveryLabel.GetText = () => textCache.Update(time);
			paletteWidget = Exts.Lazy(() => Ui.Root.Get("PRODUCTION_PALETTE") as ProductionPaletteWidget);
			purchaseButton.IsVisible = () => false;
			bottomExtension.IsVisible = () => false;
			purchaseButton.OnClick = () => ResolveOrder();
		}

		public override void Tick()
		{
			if (world.LocalPlayer == null)
				return;
			if (paletteWidget.Value.CurrentQueue is BulkProductionQueue bulkProduction)
			{
				bottomExtension.Bounds.Y = backgroundBottom.Value.Bounds.Bottom;
				bottomExtension.IsVisible = () => true;
				purchaseButton.IsVisible = () => bulkProduction.GetActorsReadyForDelivery().Count != 0 && !bulkProduction.HasDeliveryStarted();
				if (bulkProduction.HasDeliveryStarted())
				{
					time = WidgetUtils.FormatTime(bulkProduction.DeliveryDelay, world.Timestep);
					deliveryLabel.IsVisible = () => true;
				}
				else
				{
					deliveryLabel.IsVisible = () => false;
				}
			}
			else
			{
				bottomExtension.IsVisible = () => false;
			}
		}

		void ResolveOrder()
		{
			world.IssueOrder(Order.PurchaseOrder(paletteWidget.Value.CurrentQueue.Actor, paletteWidget.Value.CurrentQueue.Info.Type));
		}
	}
}
