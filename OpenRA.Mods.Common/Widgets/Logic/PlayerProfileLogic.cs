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
using System.Threading.Tasks;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LocalProfileLogic : ChromeLogic
	{
		readonly WorldRenderer worldRenderer;
		readonly LocalPlayerProfile localProfile;
		readonly Widget badgeContainer;
		readonly Widget widget;
		bool notFound;
		bool badgesVisible;

		[ObjectCreator.UseCtor]
		public LocalProfileLogic(Widget widget, WorldRenderer worldRenderer, Func<bool> minimalProfile)
		{
			this.worldRenderer = worldRenderer;
			this.widget = widget;
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

			badgeContainer = widget.Get("BADGES_CONTAINER");
			badgeContainer.IsVisible = () => badgesVisible && !minimalProfile()
				&& localProfile.State == LocalPlayerProfile.LinkState.Linked;

			localProfile.RefreshPlayerData(() => RefreshComplete(false));
		}

		public void RefreshComplete(bool updateNotFound)
		{
			if (updateNotFound)
				notFound = localProfile.State == LocalPlayerProfile.LinkState.Unlinked;

			Game.RunAfterTick(() =>
			{
				badgesVisible = false;

				if (localProfile.State == LocalPlayerProfile.LinkState.Linked)
				{
					if (localProfile.ProfileData.Badges.Count > 0)
					{
						Func<int, int> negotiateWidth = _ => widget.Get("PROFILE_HEADER").Bounds.Width;

						// Remove any stale badges that may be left over from a previous session
						badgeContainer.RemoveChildren();

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
				}

				Ui.ResetTooltips();
			});
		}
	}

	public class RegisteredProfileTooltipLogic : ChromeLogic
	{
		[TranslationReference]
		const string LoadingPlayerProfile = "label-loading-player-profile";

		[TranslationReference]
		const string LoadingPlayerProfileFailed = "label-loading-player-profile-failed";

		readonly PlayerDatabase playerDatabase;
		PlayerProfile profile;
		bool profileLoaded;

		[ObjectCreator.UseCtor]
		public RegisteredProfileTooltipLogic(Widget widget, WorldRenderer worldRenderer, ModData modData, Session.Client client)
		{
			playerDatabase = modData.Manifest.Get<PlayerDatabase>();

			var header = widget.Get("HEADER");
			var badgeContainer = widget.Get("BADGES_CONTAINER");
			var badgeSeparator = badgeContainer.GetOrNull("SEPARATOR");

			var profileHeader = header.Get("PROFILE_HEADER");
			var messageHeader = header.Get("MESSAGE_HEADER");
			var message = messageHeader.Get<LabelWidget>("MESSAGE");
			var messageFont = Game.Renderer.Fonts[message.Font];

			profileHeader.IsVisible = () => profileLoaded;
			messageHeader.IsVisible = () => !profileLoaded;

			var profileWidth = 0;
			var maxProfileWidth = widget.Bounds.Width;
			var messageText = modData.Translation.GetString(LoadingPlayerProfile);
			var messageWidth = messageFont.Measure(messageText).X + 2 * message.Bounds.Left;

			Task.Run(async () =>
			{
				try
				{
					var httpClient = HttpClientFactory.Create();

					var httpResponseMessage = await httpClient.GetAsync(playerDatabase.Profile + client.Fingerprint);
					var result = await httpResponseMessage.Content.ReadAsStreamAsync();

					var yaml = MiniYaml.FromStream(result).First();
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
							badgeContainer.Bounds.Y += header.Bounds.Height;
							if (client.IsAdmin)
							{
								profileWidth = Math.Max(profileWidth, adminFont.Measure(adminLabel.Text).X + 2 * adminLabel.Bounds.Left);

								adminContainer.IsVisible = () => true;
								profileHeader.Bounds.Height += adminLabel.Bounds.Height;
								header.Bounds.Height += adminLabel.Bounds.Height;
								badgeContainer.Bounds.Y += adminLabel.Bounds.Height;
							}

							Func<int, int> negotiateWidth = badgeWidth =>
							{
								profileWidth = Math.Min(Math.Max(badgeWidth, profileWidth), maxProfileWidth);
								return profileWidth;
							};

							if (profile.Badges.Count > 0)
							{
								var badges = Ui.LoadWidget("PLAYER_PROFILE_BADGES_INSERT", badgeContainer, new WidgetArgs()
								{
									{ nameof(worldRenderer), worldRenderer },
									{ nameof(profile), profile },
									{ nameof(negotiateWidth), negotiateWidth }
								});

								if (badges.Bounds.Height > 0)
								{
									badgeContainer.Bounds.Height = badges.Bounds.Height;
									badgeContainer.IsVisible = () => true;
								}
							}

							profileWidth = Math.Min(profileWidth, maxProfileWidth);
							header.Bounds.Width = widget.Bounds.Width = badgeContainer.Bounds.Width = profileWidth;
							widget.Bounds.Height = header.Bounds.Height + badgeContainer.Bounds.Height;

							if (badgeSeparator != null)
								badgeSeparator.Bounds.Width = profileWidth - 2 * badgeSeparator.Bounds.X;

							profileLoaded = true;
						});
					}
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to parse player data result with exception");
					Log.Write("debug", e);
				}
				finally
				{
					if (profile == null)
					{
						messageText = modData.Translation.GetString(LoadingPlayerProfileFailed);
						messageWidth = messageFont.Measure(messageText).X + 2 * message.Bounds.Left;
						header.Bounds.Width = widget.Bounds.Width = messageWidth;
					}
				}
			});

			message.GetText = () => messageText;
			header.Bounds.Height += messageHeader.Bounds.Height;
			header.Bounds.Width = widget.Bounds.Width = messageWidth;
			widget.Bounds.Height = header.Bounds.Height;
			badgeContainer.Visible = false;
		}
	}

	public class PlayerProfileBadgesLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PlayerProfileBadgesLogic(Widget widget, PlayerProfile profile, Func<int, int> negotiateWidth)
		{
			var showBadges = profile.Badges.Count > 0;
			widget.IsVisible = () => showBadges;

			var badgeTemplate = widget.Get("BADGE_TEMPLATE");
			widget.RemoveChild(badgeTemplate);

			// Negotiate the label length that the tooltip will allow
			var maxLabelWidth = 0;
			var templateIcon = badgeTemplate.Get("ICON");
			var templateLabel = badgeTemplate.Get<LabelWidget>("LABEL");
			var templateLabelFont = Game.Renderer.Fonts[templateLabel.Font];
			foreach (var badge in profile.Badges)
				maxLabelWidth = Math.Max(maxLabelWidth, templateLabelFont.Measure(badge.Label).X);

			widget.Bounds.Width = negotiateWidth(2 * templateLabel.Bounds.Left - templateIcon.Bounds.Right + maxLabelWidth);

			var badgeOffset = badgeTemplate.Bounds.Y;
			if (profile.Badges.Count > 0)
				badgeOffset += 3;

			foreach (var badge in profile.Badges)
			{
				var b = badgeTemplate.Clone();
				var icon = b.Get<BadgeWidget>("ICON");
				icon.Badge = badge;

				var label = b.Get<LabelWidget>("LABEL");
				var labelFont = Game.Renderer.Fonts[label.Font];

				var labelText = WidgetUtils.TruncateText(badge.Label, widget.Bounds.Width - label.Bounds.X - icon.Bounds.X, labelFont);
				label.GetText = () => labelText;

				b.Bounds.Y = badgeOffset;
				widget.AddChild(b);

				badgeOffset += badgeTemplate.Bounds.Height;
			}

			if (badgeOffset > badgeTemplate.Bounds.Y)
				badgeOffset += 5;

			widget.Bounds.Height = badgeOffset;
		}
	}

	public class AnonymousProfileTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AnonymousProfileTooltipLogic(Widget widget, Session.Client client)
		{
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			widget.Bounds.Width = nameFont.Measure(nameLabel.Text).X + 2 * nameLabel.Bounds.Left;

			var locationLabel = widget.Get<LabelWidget>("LOCATION");
			var ipLabel = widget.Get<LabelWidget>("IP");
			var adminLabel = widget.Get("GAME_ADMIN");

			if (client.Location != null)
			{
				var locationFont = Game.Renderer.Fonts[locationLabel.Font];
				var locationWidth = widget.Bounds.Width - 2 * locationLabel.Bounds.X;
				var location = WidgetUtils.TruncateText(client.Location, locationWidth, locationFont);
				locationLabel.IsVisible = () => true;
				locationLabel.GetText = () => location;
				widget.Bounds.Height += locationLabel.Bounds.Height;
				ipLabel.Bounds.Y += locationLabel.Bounds.Height;
				adminLabel.Bounds.Y += locationLabel.Bounds.Height;
			}

			if (client.AnonymizedIPAddress != null)
			{
				ipLabel.IsVisible = () => true;
				ipLabel.GetText = () => client.AnonymizedIPAddress;
				widget.Bounds.Height += ipLabel.Bounds.Height;
				adminLabel.Bounds.Y += locationLabel.Bounds.Height;
			}

			if (client.IsAdmin)
			{
				adminLabel.IsVisible = () => true;
				widget.Bounds.Height += adminLabel.Bounds.Height;
			}
		}
	}
}
