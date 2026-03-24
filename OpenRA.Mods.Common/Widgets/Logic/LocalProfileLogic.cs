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
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LocalProfileLogic : ChromeLogic
	{
		readonly WorldRenderer worldRenderer;
		readonly LocalPlayerProfile localProfile;
		readonly Widget badgeContainer;
		readonly Widget widget;
		bool badgesVisible;

		[ObjectCreator.UseCtor]
		public LocalProfileLogic(Widget widget, WorldRenderer worldRenderer, Func<bool> minimalProfile)
		{
			this.worldRenderer = worldRenderer;
			this.widget = widget;
			localProfile = Game.LocalPlayerProfile;

			widget.Get("CHECKING_FINGERPRINT").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.CheckingLink && !minimalProfile();
			widget.Get("LINK_ACCOUNT").IsVisible = () => localProfile.State < LocalPlayerProfile.LinkState.ConnectionFailed && !minimalProfile();
			widget.Get("CONNECTION_ERROR").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.ConnectionFailed && !minimalProfile();

			widget.Get<ButtonWidget>("LINK_BUTTON").OnClick = () => Ui.Root.GetOrNull<ButtonWidget>("SETTINGS_BUTTON")?.OnClick();
			widget.Get<ButtonWidget>("RETRY_BUTTON").OnClick = () => localProfile.RefreshPlayerData();

			// Profile view
			widget.Get("PROFILE_HEADER").IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.Linked;
			widget.Get<LabelWidget>("PROFILE_NAME").GetText = () => localProfile.ProfileData.ProfileName;
			widget.Get<LabelWidget>("PROFILE_RANK").GetText = () => localProfile.ProfileData.ProfileRank;

			var destroyKey = widget.Get<ButtonWidget>("DESTROY_KEY");
			destroyKey.OnClick = localProfile.DeleteKeypair;
			destroyKey.IsDisabled = minimalProfile;

			badgeContainer = widget.Get("BADGES_CONTAINER");
			badgeContainer.IsVisible = () => badgesVisible && !minimalProfile()
				&& localProfile.State == LocalPlayerProfile.LinkState.Linked;

			localProfile.OnStateChanged += RefreshBadges;
			RefreshBadges();
		}

		public void RefreshBadges()
		{
			Game.RunAfterTick(() =>
			{
				badgesVisible = false;

				// Remove any stale badges that may be left over from a previous session
				badgeContainer.RemoveChildren();

				if (localProfile.State == LocalPlayerProfile.LinkState.Linked && localProfile.ProfileData.Badges.Count > 0)
				{
					Func<int, int> negotiateWidth = _ => widget.Get("PROFILE_HEADER").Bounds.Width;

					var badges = Ui.LoadWidget("PLAYER_PROFILE_BADGES_INSERT", badgeContainer, new WidgetArgs()
						{
							{ "worldRenderer", worldRenderer },
							{ "profile", localProfile.ProfileData },
							{ "negotiateWidth", negotiateWidth }
						});

					if (badges.Bounds.Height > 0)
					{
						badgeContainer.Bounds.Height = badges.Bounds.Height;
						badgesVisible = true;
					}
				}

				Ui.ResetTooltips();
			});
		}

		protected override void Dispose(bool disposing)
		{
			localProfile.OnStateChanged -= RefreshBadges;
			base.Dispose(disposing);
		}
	}
}
