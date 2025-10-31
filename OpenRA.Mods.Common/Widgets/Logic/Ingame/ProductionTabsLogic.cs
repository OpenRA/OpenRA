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

using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ProductionTabsLogic : ChromeLogic
	{
		readonly ProductionTabsWidget tabs;
		readonly World world;

		void SetupProductionGroupButton(ProductionTypeButtonWidget button)
		{
			if (button == null)
				return;

			void SelectTab(bool reverse)
			{
				if (tabs.QueueGroup == button.ProductionGroup)
					tabs.SelectNextTab(reverse);
				else
					tabs.QueueGroup = button.ProductionGroup;

				tabs.PickUpCompletedBuilding();
			}

			button.IsDisabled = () => !tabs.Groups[button.ProductionGroup].Tabs.Any(t => t.Queue.BuildableItems().Any());
			button.OnMouseUp = mi => SelectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => SelectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.IsHighlighted = () => tabs.QueueGroup == button.ProductionGroup;

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName + "-disabled" :
				tabs.Groups[button.ProductionGroup].Alert ? chromeName + "-alert" : chromeName;
		}

		[ObjectCreator.UseCtor]
		public ProductionTabsLogic(Widget widget, World world)
		{
			this.world = world;
			tabs = widget.Get<ProductionTabsWidget>("PRODUCTION_TABS");
			world.ActorAdded += tabs.ActorChanged;
			world.ActorRemoved += tabs.ActorChanged;
			Game.BeforeGameStart += UnregisterEvents;

			var typesContainer = Ui.Root.Get(tabs.TypesContainer);
			foreach (var i in typesContainer.Children)
				SetupProductionGroupButton(i as ProductionTypeButtonWidget);

			var background = Ui.Root.GetOrNull(tabs.BackgroundContainer);
			if (background != null)
			{
				var palette = tabs.Parent.Get<ProductionPaletteWidget>(tabs.PaletteWidget);
				var icontemplate = background.Get("ICON_TEMPLATE");

				void UpdateBackground(int oldCount, int newCount)
				{
					background.RemoveChildren();

					for (var i = 0; i < newCount; i++)
					{
						var x = i % palette.Columns;
						var y = i / palette.Columns;

						var bg = icontemplate.Clone();
						bg.Bounds.X = palette.IconSize.X * x;
						bg.Bounds.Y = palette.IconSize.Y * y;
						background.AddChild(bg);
					}
				}

				palette.OnIconCountChanged += UpdateBackground;

				// Set the initial palette state
				UpdateBackground(0, 0);
			}
		}

		void UnregisterEvents()
		{
			Game.BeforeGameStart -= UnregisterEvents;
			world.ActorAdded -= tabs.ActorChanged;
			world.ActorRemoved -= tabs.ActorChanged;
		}
	}
}
