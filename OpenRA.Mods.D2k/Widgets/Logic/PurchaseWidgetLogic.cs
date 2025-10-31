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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class PurchaseWidgetLogic : ChromeLogic
	{
		[FluentReference("time")]
		const string DeliveryIn = "label-deliver-in-timer";
		string time = "";
		readonly World world;
		readonly Lazy<ProductionPaletteWidget> paletteWidget;
		readonly ButtonWidget purchaseButton;
		readonly LabelWidget deliveryLabel;
		readonly Color textColor;

		[ObjectCreator.UseCtor]
		public PurchaseWidgetLogic(Widget widget, World world)
		{
			this.world = world;
			purchaseButton = widget.Get<ButtonWidget>("PURCHASE_BUTTON");
			deliveryLabel = widget.Get<LabelWidget>("DELIVERY_IN");
			textColor = deliveryLabel.TextColor;
			var textCache = new CachedTransform<string, string>(s => FluentProvider.GetMessage(DeliveryIn, "time", time));
			deliveryLabel.GetText = () => textCache.Update(time);
			paletteWidget = Exts.Lazy(() => Ui.Root.Get("PRODUCTION_PALETTE") as ProductionPaletteWidget);
			purchaseButton.IsVisible = () => false;
			purchaseButton.OnClick = ResolveOrder;
		}

		public override void Tick()
		{
			if (world.LocalPlayer == null)
				return;
			if (paletteWidget.Value.CurrentQueue is BulkProductionQueue bulkProduction)
			{
				purchaseButton.IsVisible = () => bulkProduction.GetActorsReadyForDelivery().
					Count != 0 && !bulkProduction.HasDeliveryStarted();
			}
			else
			{
				purchaseButton.IsVisible = () => false;
			}

			foreach (var bulkQueue in world.LocalPlayer.PlayerActor.TraitsImplementing<BulkProductionQueue>())
			{
				if (bulkQueue.HasDeliveryStarted())
				{
					time = WidgetUtils.FormatTime(bulkQueue.DeliveryDelay, world.Timestep);
					deliveryLabel.Visible = true;
					if (bulkQueue.DeliveryDelay <= 0 && Game.LocalTick % 25 < 15)
						deliveryLabel.TextColor = Color.White;
					else
						deliveryLabel.TextColor = textColor;
				}
				else
					deliveryLabel.Visible = false;
			}
		}

		void ResolveOrder()
		{
			world.IssueOrder(
				new Order("PurchaseOrder", paletteWidget.Value.CurrentQueue.Actor, false)
				{
					TargetString = paletteWidget.Value.CurrentQueue.Info.Type
				});
		}
	}
}
