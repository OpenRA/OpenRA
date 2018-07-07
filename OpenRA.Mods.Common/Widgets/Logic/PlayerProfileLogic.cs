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
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LocalProfileLogic : ChromeLogic
	{
		readonly LocalPlayerProfile localProfile;
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

			localProfile.RefreshPlayerData(() => RefreshComplete(false));
		}

		public void RefreshComplete(bool updateNotFound)
		{
			if (updateNotFound)
				notFound = localProfile.State == LocalPlayerProfile.LinkState.Unlinked;

			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}

	public class RegisteredProfileTooltipLogic : ChromeLogic
	{
		readonly PlayerDatabase playerDatabase;
		PlayerProfile profile;
		bool profileLoaded;

		[ObjectCreator.UseCtor]
		public RegisteredProfileTooltipLogic(Widget widget, WorldRenderer worldRenderer, ModData modData, Session.Client client)
		{
			playerDatabase = modData.Manifest.Get<PlayerDatabase>();

			var header = widget.Get("HEADER");

			var profileHeader = header.Get("PROFILE_HEADER");
			var messageHeader = header.Get("MESSAGE_HEADER");
			var message = messageHeader.Get<LabelWidget>("MESSAGE");
			var messageFont = Game.Renderer.Fonts[message.Font];

			profileHeader.IsVisible = () => profileLoaded;
			messageHeader.IsVisible = () => !profileLoaded;

			var profileWidth = 0;
			var messageText = "Loading player profile...";
			var messageWidth = messageFont.Measure(messageText).X + 2 * message.Bounds.Left;

			Action<DownloadDataCompletedEventArgs> onQueryComplete = i =>
			{
				try
				{
					if (i.Error == null)
					{
						var yaml = MiniYaml.FromString(Encoding.UTF8.GetString(i.Result)).First();
						if (yaml.Key == "Player")
						{
							profile = FieldLoader.Load<PlayerProfile>(yaml.Value);
							Game.RunAfterTick(() =>
							{
								var nameLabel = profileHeader.Get<LabelWidget>("PROFILE_NAME");
								var nameFont = Game.Renderer.Fonts[nameLabel.Font];
								var rankLabel = profileHeader.Get<LabelWidget>("PROFILE_RANK");
								var rankFont = Game.Renderer.Fonts[rankLabel.Font];

								var adminContainer = profileHeader.Get("GAME_ADMIN");
								var adminLabel = adminContainer.Get<LabelWidget>("LABEL");
								var adminFont = Game.Renderer.Fonts[adminLabel.Font];

								var headerSizeOffset = profileHeader.Bounds.Height - messageHeader.Bounds.Height;

								nameLabel.GetText = () => profile.ProfileName;
								rankLabel.GetText = () => profile.ProfileRank;

								profileWidth = Math.Max(profileWidth, nameFont.Measure(profile.ProfileName).X + 2 * nameLabel.Bounds.Left);
								profileWidth = Math.Max(profileWidth, rankFont.Measure(profile.ProfileRank).X + 2 * rankLabel.Bounds.Left);

								header.Bounds.Height += headerSizeOffset;
								if (client.IsAdmin)
								{
									profileWidth = Math.Max(profileWidth, adminFont.Measure(adminLabel.Text).X + 2 * adminLabel.Bounds.Left);

									adminContainer.IsVisible = () => true;
									profileHeader.Bounds.Height += adminLabel.Bounds.Height;
									header.Bounds.Height += adminLabel.Bounds.Height;
								}

								profileWidth = Math.Min(profileWidth, widget.Bounds.Width);
								header.Bounds.Width = widget.Bounds.Width = profileWidth;
								widget.Bounds.Height = header.Bounds.Height;

								profileLoaded = true;
							});
						}
					}
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to parse player data result with exception: {0}", e);
				}
				finally
				{
					if (profile == null)
					{
						messageText = "Failed to load player profile.";
						messageWidth = messageFont.Measure(messageText).X + 2 * message.Bounds.Left;
						header.Bounds.Width = widget.Bounds.Width = messageWidth;
					}
				}
			};

			message.GetText = () => messageText;
			header.Bounds.Height += messageHeader.Bounds.Height;
			header.Bounds.Width = widget.Bounds.Width = messageWidth;
			widget.Bounds.Height = header.Bounds.Height;

			new Download(playerDatabase.Profile + client.Fingerprint, _ => { }, onQueryComplete);
		}
	}

	public class AnonymousProfileTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AnonymousProfileTooltipLogic(Widget widget, OrderManager orderManager, Session.Client client)
		{
			var address = LobbyUtils.GetExternalIP(client, orderManager);
			var cachedDescriptiveIP = address ?? "Unknown IP";

			var nameLabel = widget.Get<LabelWidget>("NAME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			widget.Bounds.Width = nameFont.Measure(nameLabel.Text).X + 2 * nameLabel.Bounds.Left;

			var ipLabel = widget.Get<LabelWidget>("IP");
			ipLabel.GetText = () => cachedDescriptiveIP;

			var locationLabel = widget.Get<LabelWidget>("LOCATION");
			var cachedCountryLookup = GeoIP.LookupCountry(address);
			locationLabel.GetText = () => cachedCountryLookup;

			if (client.IsAdmin)
			{
				var adminLabel = widget.Get("GAME_ADMIN");
				adminLabel.IsVisible = () => client.IsAdmin;
				widget.Bounds.Height += adminLabel.Bounds.Height;
			}
		}
	}
}
