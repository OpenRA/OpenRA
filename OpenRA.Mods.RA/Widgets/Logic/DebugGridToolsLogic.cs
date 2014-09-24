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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DebugGridToolsLogic
	{
		[ObjectCreator.UseCtor]
		public DebugGridToolsLogic(Widget widget, World world)
		{
			var gridTrait = world.WorldActor.Trait<CellGridDebugOverlay>();

			var toggleEntireGrid = widget.GetOrNull<ButtonWidget>("TOGGLE_GRID");
			if (toggleEntireGrid != null)
			{
				toggleEntireGrid.OnMouseUp = _ =>
				{
					gridTrait.Visible ^= true;
					toggleEntireGrid.Text = "Grid " + (gridTrait.Visible ? "Enabled" : "Disabled");
				};
			}

			var cycleRenderOrder = widget.GetOrNull<ButtonWidget>("TOGGLE_RENDER_ORDER");
			if (cycleRenderOrder != null)
			{
				cycleRenderOrder.Text = GetRenderOrderText(gridTrait);
				cycleRenderOrder.OnMouseUp = _ =>
				{
					gridTrait.SwapRenderOrder();
					cycleRenderOrder.Text = GetRenderOrderText(gridTrait);
				};
			}

			var allColors = Enum.GetValues(typeof(KnownColor)).Cast<KnownColor>();
			var colorFullDropDown = widget.GetOrNull<DropDownButtonWidget>("COLOR_FULL");
			if (colorFullDropDown != null)
			{
				colorFullDropDown.Text = "Full Cell Color";
				colorFullDropDown.OnMouseDown = _ =>
				{
					var options = new List<DropDownOption>();

					foreach (var color in allColors)
					{
						var newOption = new DropDownOption()
						{
							Title = color.ToString(),
							IsSelected = () => false,
							OnClick = () => gridTrait.FullCellColor = Color.FromName(color.ToString())
						};

						options.Add(newOption);
					}

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
					colorFullDropDown.OnClick = () => colorFullDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 175, options, setupItem);
				};
			}

			var colorHalfDropDown = widget.GetOrNull<DropDownButtonWidget>("COLOR_HALF");
			if (colorHalfDropDown != null)
			{
				colorHalfDropDown.Text = "Half Cell Color";
				colorHalfDropDown.OnMouseDown = _ =>
				{
					var options = new List<DropDownOption>();

					foreach (var color in allColors)
					{
						var newOption = new DropDownOption()
						{
							Title = color.ToString(),
							IsSelected = () => false,
							OnClick = () => gridTrait.HalfCellColor = Color.FromName(color.ToString())
						};

						options.Add(newOption);
					}

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};
					colorHalfDropDown.OnClick = () => colorHalfDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 175, options, setupItem);
				};
			}
		
			var textRadius = widget.GetOrNull<TextFieldWidget>("GRID_RADIUS");
			if (textRadius != null)
			{
				textRadius.Text = gridTrait.GridRadius.ToString();
				textRadius.OnEnterKey = () =>
				{
					var text = textRadius.Text;
					var radius = 0;

					if (text.Contains("-"))
						text = text.Replace("-", "");

					if (!int.TryParse(text, out radius))
					{
						Game.Debug("`{0}` is not a valid integer.", text);
						radius = gridTrait.GridRadius;
					}

					gridTrait.SetRadius(radius);

					textRadius.YieldKeyboardFocus();
					return true;
				};

				textRadius.OnEscKey = () => { textRadius.YieldKeyboardFocus(); return true; };
			}
		
			var toggleFull = widget.GetOrNull<ButtonWidget>("TOGGLE_GRID_FULL");
			if (toggleFull != null)
			{
				toggleFull.OnMouseUp = _ =>
				{
					gridTrait.RenderFullGrid ^= true;
					toggleFull.TextColor = gridTrait.RenderFullGrid ? gridTrait.FullCellColor : Color.White;
				};
			}

			var toggleHalf = widget.GetOrNull<ButtonWidget>("TOGGLE_GRID_HALF");
			if (toggleHalf != null)
			{
				toggleHalf.OnMouseUp = _ =>
				{
					gridTrait.RenderHalfGrid ^= true;
					toggleHalf.TextColor = gridTrait.RenderHalfGrid ? gridTrait.HalfCellColor : Color.White;
				};
			}

			var toggleType = widget.GetOrNull<ButtonWidget>("TOGGLE_GRID_TYPE");
			if (toggleType != null)
			{
				toggleType.OnMouseUp = _ =>
				{
					gridTrait.SwapGridType();
					var gt = gridTrait.GridType;
					toggleType.Text = gt == GridType.FullMap ? "Full Map" : "Follows Mouse";
				};
			}

			var reset = widget.GetOrNull<ButtonWidget>("RESET_GRID");
			if (reset != null)
				reset.OnMouseUp = _ => gridTrait.ResetAll();
		}

		string GetRenderOrderText(CellGridDebugOverlay grid)
		{
			var ro = grid.RenderOrder;
			return ro == RenderOrder.AfterActors ? "Above Actors" : "Below Actors";
		}

		public void Order(World world, string order)
		{
			world.IssueOrder(new Order(order, world.WorldActor, false));
		}
	}

	class DropDownOption
	{
		public string Title;
		public Func<bool> IsSelected;
		public Action OnClick;
	}
}
