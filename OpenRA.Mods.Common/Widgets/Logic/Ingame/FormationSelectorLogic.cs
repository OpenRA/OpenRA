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
using System.Linq;
using OpenRA;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class FormationSelectorLogic : ChromeLogic
	{
		[FluentReference]
		const string DefaultLabel = "options-formation-default";

		[FluentReference]
		const string SquareLabel = "options-formation-square";

		[FluentReference]
		const string CircleLabel = "options-formation-circle";

		[FluentReference]
		const string LineHorizontalLabel = "options-formation-line-horizontal";

		[FluentReference]
		const string LineVerticalLabel = "options-formation-line-vertical";

		[FluentReference]
		const string PyramidLabel = "options-formation-pyramid";

		[FluentReference]
		const string PyramidInvertedLabel = "options-formation-pyramid-inverted";

		[FluentReference]
		const string VFormationLabel = "options-formation-v-formation";

		[FluentReference]
		const string VInvertedLabel = "options-formation-v-inverted";

		[FluentReference]
		const string VLeftLabel = "options-formation-v-left";

		[FluentReference]
		const string VRightLabel = "options-formation-v-right";

		readonly World world;
		int selectionHash;
		bool formationDisabled = true;

		static readonly FormationType[] Options =
		[
			FormationType.Default,
			FormationType.Square,
			FormationType.Circle,
			FormationType.LineHorizontal,
			FormationType.LineVertical,
			FormationType.Pyramid,
			FormationType.PyramidInverted,
			FormationType.VFormation,
			FormationType.VInverted,
			FormationType.VLeft,
			FormationType.VRight,
		];

		[ObjectCreator.UseCtor]
		public FormationSelectorLogic(Widget widget, World world)
		{
			this.world = world;

			var formationButton = widget.GetOrNull<DropDownButtonWidget>("FORMATION");
			if (formationButton == null)
				return;

			WidgetUtils.BindButtonIcon(formationButton);

			formationButton.IsDisabled = () => { UpdateStateIfNecessary(); return formationDisabled; };
			formationButton.IsHighlighted = () => FormationPreferences.Selected != FormationType.Default;

			formationButton.OnMouseDown = _ =>
			{
				if (formationButton.IsDisabled())
					return;

				ScrollItemWidget SetupItem(FormationType option, ScrollItemWidget template)
				{
					bool IsSelected() => FormationPreferences.Selected == option;
					void OnClick()
					{
						FormationPreferences.Selected = option;
						FormationResolver.ApplyImmediateFormation(world, world.Selection.Actors, option);
					}

					var item = ScrollItemWidget.Setup(template, IsSelected, OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => GetLabel(option);
					return item;
				}

				formationButton.ShowDropDown("FORMATION_DROPDOWN_TEMPLATE", Options.Length * 25, Options, SetupItem);
			};
		}

		void UpdateStateIfNecessary()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			formationDisabled = !world.Selection.Actors
				.Any(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<IMoveInfo>());

			selectionHash = world.Selection.Hash;
		}

		static string GetLabel(FormationType option)
		{
			return option switch
			{
				FormationType.Default => FluentProvider.GetMessage(DefaultLabel),
				FormationType.Square => FluentProvider.GetMessage(SquareLabel),
				FormationType.Circle => FluentProvider.GetMessage(CircleLabel),
				FormationType.LineHorizontal => FluentProvider.GetMessage(LineHorizontalLabel),
				FormationType.LineVertical => FluentProvider.GetMessage(LineVerticalLabel),
				FormationType.Pyramid => FluentProvider.GetMessage(PyramidLabel),
				FormationType.PyramidInverted => FluentProvider.GetMessage(PyramidInvertedLabel),
				FormationType.VFormation => FluentProvider.GetMessage(VFormationLabel),
				FormationType.VInverted => FluentProvider.GetMessage(VInvertedLabel),
				FormationType.VLeft => FluentProvider.GetMessage(VLeftLabel),
				FormationType.VRight => FluentProvider.GetMessage(VRightLabel),
				_ => option.ToString(),
			};
		}
	}
}
