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
	public class RegisteredProfileTooltipLogic : ChromeLogic
	{
		[FluentReference]
		const string LoadingPlayerProfile = "label-loading-player-profile";

		[FluentReference]
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
			var messageText = FluentProvider.GetMessage(LoadingPlayerProfile);
			var messageWidth = messageFont.Measure(messageText).X + 2 * message.Bounds.Left;

			Task.Run(async () =>
			{
				try
				{
					var httpClient = HttpClientFactory.Create();

					var url = playerDatabase.Profile + client.Fingerprint;
					var httpResponseMessage = await httpClient.GetAsync(url);
					var result = await httpResponseMessage.Content.ReadAsStreamAsync();

					var yaml = MiniYaml.FromStream(result, url).First();
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
								profileWidth = Math.Max(profileWidth, adminFont.Measure(adminLabel.GetText()).X + 2 * adminLabel.Bounds.Left);

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
						messageText = FluentProvider.GetMessage(LoadingPlayerProfileFailed);
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
}
