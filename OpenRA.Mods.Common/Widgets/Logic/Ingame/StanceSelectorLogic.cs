#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class StanceSelectorLogic : ChromeLogic
	{
		readonly World world;

		int selectionHash;
		TraitPair<AutoTarget>[] actorStances = { };

		[ObjectCreator.UseCtor]
		public StanceSelectorLogic(Widget widget, World world)
		{
			this.world = world;

			var holdFireButton = widget.GetOrNull<ButtonWidget>("STANCE_HOLDFIRE");
			if (holdFireButton != null)
				BindStanceButton(holdFireButton, UnitStance.HoldFire);

			var returnFireButton = widget.GetOrNull<ButtonWidget>("STANCE_RETURNFIRE");
			if (returnFireButton != null)
				BindStanceButton(returnFireButton, UnitStance.ReturnFire);

			var defendButton = widget.GetOrNull<ButtonWidget>("STANCE_DEFEND");
			if (defendButton != null)
				BindStanceButton(defendButton, UnitStance.Defend);

			var attackAnythingButton = widget.GetOrNull<ButtonWidget>("STANCE_ATTACKANYTHING");
			if (attackAnythingButton != null)
				BindStanceButton(attackAnythingButton, UnitStance.AttackAnything);
		}

		void BindStanceButton(ButtonWidget button, UnitStance stance)
		{
			var icon = button.Get<ImageWidget>("ICON");
			var hasDisabled = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-disabled") != null;
			var hasActive = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-active") != null;
			var hasActiveHover = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-active-hover") != null;
			var hasHover = ChromeProvider.GetImage(icon.ImageCollection, icon.ImageName + "-hover") != null;

			icon.GetImageName = () => hasActive && button.IsHighlighted() ?
						(hasActiveHover && Ui.MouseOverWidget == button ? icon.ImageName + "-active-hover" : icon.ImageName + "-active") :
					hasDisabled && button.IsDisabled() ? icon.ImageName + "-disabled" :
					hasHover && Ui.MouseOverWidget == button ? icon.ImageName + "-hover" : icon.ImageName;

			button.IsDisabled = () => { UpdateStateIfNecessary(); return !actorStances.Any(); };
			button.IsHighlighted = () => actorStances.Any(
				at => !at.Trait.IsTraitDisabled && at.Trait.PredictedStance == stance);
			button.OnClick = () => SetSelectionStance(stance);
		}

		void UpdateStateIfNecessary()
		{
			if (selectionHash == world.Selection.Hash)
				return;

			actorStances = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld)
				.SelectMany(a => a.TraitsImplementing<AutoTarget>()
					.Where(at => at.Info.EnableStances)
					.Select(at => new TraitPair<AutoTarget>(a, at)))
				.ToArray();

			selectionHash = world.Selection.Hash;
		}

		void SetSelectionStance(UnitStance stance)
		{
			foreach (var at in actorStances)
			{
				if (!at.Trait.IsTraitDisabled)
					at.Trait.PredictedStance = stance;

				world.IssueOrder(new Order("SetUnitStance", at.Actor, false) { ExtraData = (uint)stance });
			}
		}
	}
}
