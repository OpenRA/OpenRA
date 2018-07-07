#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LocalProfileLogic : ChromeLogic
	{
		readonly LocalPlayerProfile localProfile;
		readonly Widget detailsContainer;
		bool notFound;

		[ObjectCreator.UseCtor]
		public LocalProfileLogic(Widget widget, WorldRenderer worldRenderer, Func<bool> minimalProfile)
		{
			localProfile = Game.LocalPlayerProfile;

			// Key registration
			widget.Get("GENERATE_KEYS").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.Uninitialized && !minimalProfile();
			widget.Get("GENERATING_KEYS").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.GeneratingKeys && !minimalProfile();

			var lastProfileState = LocalPlayerProfile.LinkState.CheckingLink;
			widget.Get("REGISTER_FINGERPRINT").IsVisible = () =>
			{
				// Take a copy of the state to avoid race conditions
				var state = localProfile.State;

				// Copy the key to the clipboard when displaying the link instructions
				if (state != lastProfileState && state == LocalPlayerProfile.LinkState.Unlinked)
					Game.SetClipboardText(localProfile.PublicKey);

				lastProfileState = state;
				return localProfile.State == LocalPlayerProfile.LinkState.Unlinked && !notFound && !minimalProfile();
			};

			widget.Get("CHECKING_FINGERPRINT").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.CheckingLink && !minimalProfile();
			widget.Get("FINGERPRINT_NOT_FOUND").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.Unlinked && notFound && !minimalProfile();
			widget.Get("CONNECTION_ERROR").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.ConnectionFailed && !minimalProfile();

			widget.Get<ButtonWidget>("GENERATE_KEY").OnClick = localProfile.GenerateKeypair;

			widget.Get<ButtonWidget>("CHECK_KEY").OnClick = () => localProfile.RefreshPlayerData(() => RefreshComplete(true));

			widget.Get<ButtonWidget>("DELETE_KEY").OnClick = () =>
			{
				localProfile.DeleteKeypair();
				Game.RunAfterTick(Ui.ResetTooltips);
			};

			widget.Get<ButtonWidget>("FINGERPRINT_NOT_FOUND_CONTINUE").OnClick = () =>
			{
				notFound = false;
				Game.RunAfterTick(Ui.ResetTooltips);
			};

			widget.Get<ButtonWidget>("CONNECTION_ERROR_RETRY").OnClick = () => localProfile.RefreshPlayerData(() => RefreshComplete(true));

			// Profile view
			widget.Get("PROFILE_HEADER").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.Linked;
			widget.Get<LabelWidget>("PROFILE_NAME").GetText = () => localProfile.ProfileData.ProfileName;
			widget.Get<LabelWidget>("PROFILE_RANK").GetText = () => localProfile.ProfileData.ProfileRank;

			var destroyKey = widget.Get<ButtonWidget>("DESTROY_KEY");
			destroyKey.OnClick = localProfile.DeleteKeypair;
			destroyKey.IsDisabled = minimalProfile;

			detailsContainer = widget.Get("PROFILE_DETAILS");
			detailsContainer.IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.Linked && !minimalProfile();
			localProfile.RefreshPlayerData(() => RefreshComplete(false));
		}

		public void RefreshComplete(bool updateNotFound)
		{
			if (updateNotFound)
				notFound = localProfile.State == LocalPlayerProfile.LinkState.Unlinked;

			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}
}
