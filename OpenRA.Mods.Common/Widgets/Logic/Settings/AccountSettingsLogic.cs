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
using System.Collections.Generic;
using System.Diagnostics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	/// <summary>
	/// Settings tab for linking the local authentication key to a player-database
	/// account (username + password, see <see cref="LocalPlayerProfile.LinkForumAccount"/>).
	/// Backport of the account section that upstream commit 1fce287392 added to the
	/// Gameplay settings tab, which does not exist on release-20250330.
	/// </summary>
	public class AccountSettingsLogic : ChromeLogic
	{
		[FluentReference]
		const string ButtonForumProfileLink = "button-forum-profile-link";

		[FluentReference]
		const string ButtonForumProfileUnlink = "button-forum-profile-unlink";

		[FluentReference]
		const string LabelForumProfileResultUnlinked = "label-forum-profile-result-unlinked";

		[FluentReference]
		const string LabelForumProfileResultLinked = "label-forum-profile-result-linked";

		[FluentReference]
		const string LabelForumProfileResultAuthFailure = "label-forum-profile-result-auth-failure";

		[FluentReference]
		const string LabelForumProfileResultLoginAttempts = "label-forum-profile-result-login-attempts";

		[FluentReference]
		const string LabelForumProfileResultBanned = "label-forum-profile-result-banned";

		[FluentReference]
		const string LabelForumProfileResultConnectionFailed = "label-forum-profile-result-connection-failed";

		[FluentReference]
		const string LabelForumProfileResultError = "label-forum-profile-result-error";

		readonly ModData modData;

		TextFieldWidget profileUsernameTextfield;
		PasswordFieldWidget profilePasswordTextfield;
		LocalPlayerProfile.LinkState lastState;

		[ObjectCreator.UseCtor]
		public AccountSettingsLogic(
			ModData modData, Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel,
			string panelID, string label)
		{
			this.modData = modData;
			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			var localProfile = Game.LocalPlayerProfile;
			profileUsernameTextfield = panel.Get<TextFieldWidget>("USERNAME");
			profileUsernameTextfield.IsDisabled = () => localProfile.State != LocalPlayerProfile.LinkState.Unlinked;

			profilePasswordTextfield = panel.Get<PasswordFieldWidget>("PASSWORD");
			profilePasswordTextfield.IsDisabled = profileUsernameTextfield.IsDisabled;

			localProfile.OnStateChanged += UpdateForumAuthFields;
			UpdateForumAuthFields();

			var checkingFingerprintLabel = panel.Get<LabelWidget>("CHECKING_FINGERPRINT");
			checkingFingerprintLabel.IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.CheckingLink;

			var connectionErrorLabel = panel.Get<LabelWidget>("CONNECTION_ERROR");
			connectionErrorLabel.IsVisible = () => localProfile.State == LocalPlayerProfile.LinkState.ConnectionFailed;

			var linkResult = LocalPlayerProfile.LinkResult.Success;
			var resultLabels = new Dictionary<LocalPlayerProfile.LinkResult, string>
			{
				{ LocalPlayerProfile.LinkResult.Success, null },
				{ LocalPlayerProfile.LinkResult.AuthFailure, FluentProvider.GetMessage(LabelForumProfileResultAuthFailure) },
				{ LocalPlayerProfile.LinkResult.LoginAttempts, FluentProvider.GetMessage(LabelForumProfileResultLoginAttempts) },
				{ LocalPlayerProfile.LinkResult.Banned, FluentProvider.GetMessage(LabelForumProfileResultBanned) },
				{ LocalPlayerProfile.LinkResult.Error, FluentProvider.GetMessage(LabelForumProfileResultError) },
				{ LocalPlayerProfile.LinkResult.ConnectionFailed, FluentProvider.GetMessage(LabelForumProfileResultConnectionFailed) },
			};

			var resultLinkedLabel = FluentProvider.GetMessage(LabelForumProfileResultLinked);
			var resultUnlinkedLabel = FluentProvider.GetMessage(LabelForumProfileResultUnlinked);

			var profileStatusLabel = panel.Get<LabelWidget>("PROFILE_STATUS");
			profileStatusLabel.IsVisible = () => localProfile.State != LocalPlayerProfile.LinkState.ConnectionFailed &&
				localProfile.State != LocalPlayerProfile.LinkState.CheckingLink;
			profileStatusLabel.GetText = () => resultLabels[linkResult] ?? (
				localProfile.State == LocalPlayerProfile.LinkState.Linked ? resultLinkedLabel : resultUnlinkedLabel);

			var linkLabel = FluentProvider.GetMessage(ButtonForumProfileLink);
			var unlinkLabel = FluentProvider.GetMessage(ButtonForumProfileUnlink);
			var linkButton = panel.Get<ButtonWidget>("LINK_BUTTON");
			linkButton.IsVisible = () => localProfile.State != LocalPlayerProfile.LinkState.ConnectionFailed;
			linkButton.IsDisabled = () =>
			{
				if (localProfile.State == LocalPlayerProfile.LinkState.Unlinked)
					return string.IsNullOrWhiteSpace(profileUsernameTextfield.Text) || string.IsNullOrWhiteSpace(profilePasswordTextfield.Text);
				return localProfile.State != LocalPlayerProfile.LinkState.Linked;
			};

			linkButton.GetText = () => localProfile.State == LocalPlayerProfile.LinkState.Unlinked ? linkLabel : unlinkLabel;
			linkButton.OnClick = () =>
			{
				if (localProfile.State == LocalPlayerProfile.LinkState.Linked)
					localProfile.DeleteKeypair();
				else if (localProfile.State == LocalPlayerProfile.LinkState.Unlinked)
					localProfile.LinkForumAccount(profileUsernameTextfield.Text, profilePasswordTextfield.Text, r => linkResult = r);
			};

			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = connectionErrorLabel.IsVisible;
			retryButton.OnClick = localProfile.RefreshPlayerData;

			var playerDatabase = modData.Manifest.Get<PlayerDatabase>();
			var forumButton = panel.Get<ButtonWidget>("FORUM_BUTTON");
			forumButton.OnClick = () => OpenUrl(playerDatabase.Forum);

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			// Nothing on this panel requires a restart.
			return () => false;
		}

		static Action ResetPanel(Widget panel)
		{
			// Account linking is not a preference; there is nothing to reset.
			return () => { };
		}

		void UpdateForumAuthFields()
		{
			// OnStateChanged is raised from the LocalPlayerProfile worker thread;
			// widgets may only be touched on the main thread (as LocalProfileLogic.RefreshBadges does).
			Game.RunAfterTick(() =>
			{
				var localProfile = Game.LocalPlayerProfile;
				var state = localProfile.State;
				if (state == LocalPlayerProfile.LinkState.Linked && localProfile.ProfileData != null)
				{
					profileUsernameTextfield.Text = localProfile.ProfileData.ProfileName;
					profilePasswordTextfield.Text = "";
				}
				else if (lastState == LocalPlayerProfile.LinkState.Linked)
					profileUsernameTextfield.Text = "";

				lastState = state;
			});
		}

		// release-20250330 has no engine-level URL opener (bleed's Renderer.TryOpenUrl
		// arrived later); UseShellExecute delegates to the platform handler.
		static void OpenUrl(string url)
		{
			try
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch (Exception e)
			{
				Log.Write("debug", $"Failed to open {url}:");
				Log.Write("debug", e);
			}
		}

		protected override void Dispose(bool disposing)
		{
			Game.LocalPlayerProfile.OnStateChanged -= UpdateForumAuthFields;
			base.Dispose(disposing);
		}
	}
}
