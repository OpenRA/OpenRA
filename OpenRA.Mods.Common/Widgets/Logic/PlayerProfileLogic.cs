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
					if (localProfile.ProfileData.Badges.Any())
					{
						Func<int, int> negotiateWidth = _ => (int)widget.Get("PROFILE_HEADER").Node.LayoutWidth;

						// Remove any stale badges that may be left over from a previous session
						badgeContainer.RemoveChildren();

						var badges = Ui.LoadWidget("PLAYER_PROFILE_BADGES_INSERT", badgeContainer, new WidgetArgs()
						{
							{ "worldRenderer", worldRenderer },
							{ "profile", localProfile.ProfileData },
							{ "negotiateWidth", negotiateWidth }
						});

						if ((int)badges.Node.LayoutHeight > 0)
						{
							badgeContainer.Node.Height = (int)badges.Node.LayoutHeight;
							badgeContainer.Node.CalculateLayout();
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
			var maxProfileWidth = (int)widget.Node.LayoutWidth;
			var messageText = "Loading player profile...";
			var messageWidth = messageFont.Measure(messageText).X + 2 * (int)message.Node.LayoutX;

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

								var headerSizeOffset = (int)profileHeader.Node.LayoutHeight - (int)messageHeader.Node.LayoutHeight;

								nameLabel.GetText = () => profile.ProfileName;
								rankLabel.GetText = () => profile.ProfileRank;

								profileWidth = Math.Max(profileWidth, nameFont.Measure(profile.ProfileName).X + 2 * (int)nameLabel.Node.LayoutX);
								profileWidth = Math.Max(profileWidth, rankFont.Measure(profile.ProfileRank).X + 2 * (int)rankLabel.Node.LayoutX);

								header.Node.Height = (int)header.Node.LayoutHeight + headerSizeOffset;
								header.Node.CalculateLayout();
								badgeContainer.Node.Top = (int)badgeContainer.Node.LayoutY + (int)header.Node.LayoutHeight;
								badgeContainer.Node.CalculateLayout();
								if (client.IsAdmin)
								{
									profileWidth = Math.Max(profileWidth, adminFont.Measure(adminLabel.Text).X + 2 * (int)adminLabel.Node.LayoutX);

									adminContainer.IsVisible = () => true;
									profileHeader.Node.Height = (int)profileHeader.Node.LayoutHeight + (int)adminLabel.Node.LayoutHeight;
									profileHeader.Node.CalculateLayout();
									header.Node.Height = (int)header.Node.LayoutHeight + (int)adminLabel.Node.LayoutHeight;
									header.Node.CalculateLayout();
									badgeContainer.Node.Top = (int)badgeContainer.Node.LayoutY + (int)adminLabel.Node.LayoutHeight;
									badgeContainer.Node.CalculateLayout();
								}

								Func<int, int> negotiateWidth = badgeWidth =>
								{
									profileWidth = Math.Min(Math.Max(badgeWidth, profileWidth), maxProfileWidth);
									return profileWidth;
								};

								if (profile.Badges.Any())
								{
									var badges = Ui.LoadWidget("PLAYER_PROFILE_BADGES_INSERT", badgeContainer, new WidgetArgs()
									{
										{ "worldRenderer", worldRenderer },
										{ "profile", profile },
										{ "negotiateWidth", negotiateWidth }
									});

									if ((int)badges.Node.LayoutHeight > 0)
									{
										badgeContainer.Node.Height = (int)badges.Node.LayoutHeight;
										badgeContainer.Node.CalculateLayout();
										badgeContainer.IsVisible = () => true;
									}
								}

								profileWidth = Math.Min(profileWidth, maxProfileWidth);
								header.Node.Width = widget.Node.Width = badgeContainer.Node.Width = profileWidth;
								badgeContainer.Node.CalculateLayout();
								header.Node.CalculateLayout();
								widget.Node.CalculateLayout();
								widget.Node.Height = (int)header.Node.LayoutHeight + (int)badgeContainer.Node.LayoutHeight;
								widget.Node.CalculateLayout();

								if (badgeSeparator != null)
								{
									badgeSeparator.Node.Width = profileWidth - 2 * (int)badgeSeparator.Node.LayoutX;
									badgeSeparator.Node.CalculateLayout();
								}

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
						messageWidth = messageFont.Measure(messageText).X + 2 * (int)message.Node.LayoutX;
						header.Node.Width = widget.Node.Width = messageWidth;
						header.Node.CalculateLayout();
						widget.Node.CalculateLayout();
					}
				}
			};

			message.GetText = () => messageText;
			header.Node.Height = (int)header.Node.LayoutHeight + (int)messageHeader.Node.LayoutHeight;
			header.Node.Width = widget.Node.Width = messageWidth;
			header.Node.CalculateLayout();
			widget.Node.CalculateLayout();
			widget.Node.Height = (int)header.Node.LayoutHeight;
			widget.Node.CalculateLayout();
			badgeContainer.Visible = false;

			new Download(playerDatabase.Profile + client.Fingerprint, _ => { }, onQueryComplete);
		}
	}

	public class PlayerProfileBadgesLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PlayerProfileBadgesLogic(Widget widget, PlayerProfile profile, Func<int, int> negotiateWidth)
		{
			var showBadges = profile.Badges.Any();
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

			widget.Node.Width = negotiateWidth(2 * (int)templateLabel.Node.LayoutX - (int)(templateIcon.Node.LayoutX + templateIcon.Node.LayoutWidth) + maxLabelWidth);
			widget.Node.CalculateLayout();

			var badgeOffset = (int)badgeTemplate.Node.LayoutY;
			if (profile.Badges.Any())
				badgeOffset += 3;

			foreach (var badge in profile.Badges)
			{
				var b = badgeTemplate.Clone();
				var icon = b.Get<BadgeWidget>("ICON");
				icon.Badge = badge;

				var label = b.Get<LabelWidget>("LABEL");
				var labelFont = Game.Renderer.Fonts[label.Font];

				var labelText = WidgetUtils.TruncateText(badge.Label, (int)widget.Node.LayoutWidth - (int)label.Node.LayoutX - (int)icon.Node.LayoutX, labelFont);
				label.GetText = () => labelText;

				b.Node.Top = badgeOffset;
				b.Node.CalculateLayout();
				widget.AddChild(b);

				badgeOffset += (int)badgeTemplate.Node.LayoutHeight;
			}

			if (badgeOffset > (int)badgeTemplate.Node.LayoutY)
				badgeOffset += 5;

			widget.Node.Height = badgeOffset;
			widget.Node.CalculateLayout();
		}
	}

	public class AnonymousProfileTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AnonymousProfileTooltipLogic(Widget widget, OrderManager orderManager, Session.Client client)
		{
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			widget.Node.Width = nameFont.Measure(nameLabel.Text).X + 2 * (int)nameLabel.Node.LayoutX;
			widget.Node.CalculateLayout();

			var locationLabel = widget.Get<LabelWidget>("LOCATION");
			var ipLabel = widget.Get<LabelWidget>("IP");
			var adminLabel = widget.Get("GAME_ADMIN");

			if (client.Location != null)
			{
				var locationFont = Game.Renderer.Fonts[locationLabel.Font];
				var locationWidth = (int)widget.Node.LayoutWidth - 2 * (int)locationLabel.Node.LayoutX;
				var location = WidgetUtils.TruncateText(client.Location, locationWidth, locationFont);
				locationLabel.IsVisible = () => true;
				locationLabel.GetText = () => location;
				widget.Node.Height = (int)widget.Node.LayoutHeight + (int)locationLabel.Node.LayoutHeight;
				widget.Node.CalculateLayout();
				ipLabel.Node.Top = (int)ipLabel.Node.LayoutY + (int)locationLabel.Node.LayoutHeight;
				ipLabel.Node.CalculateLayout();
				adminLabel.Node.Top = (int)adminLabel.Node.LayoutY + (int)locationLabel.Node.LayoutHeight;
				adminLabel.Node.CalculateLayout();
			}

			if (client.AnonymizedIPAddress != null)
			{
				ipLabel.IsVisible = () => true;
				ipLabel.GetText = () => client.AnonymizedIPAddress;
				widget.Node.Height = (int)widget.Node.LayoutHeight + (int)ipLabel.Node.LayoutHeight;
				widget.Node.CalculateLayout();
				adminLabel.Node.Top = (int)adminLabel.Node.LayoutY + (int)locationLabel.Node.LayoutHeight;
				adminLabel.Node.CalculateLayout();
			}

			if (client.IsAdmin)
			{
				adminLabel.IsVisible = () => true;
				widget.Node.Height = (int)widget.Node.LayoutHeight + (int)adminLabel.Node.LayoutHeight;
				widget.Node.CalculateLayout();
			}
		}
	}
}
