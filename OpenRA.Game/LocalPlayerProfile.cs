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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class LocalPlayerProfile
	{
		public enum LinkState { Unlinked, Linking, CheckingLink, ConnectionFailed, Linked }
		public enum LinkResult { Success, AuthFailure, LoginAttempts, Banned, Error, ConnectionFailed }

		sealed record KeyData(RSAParameters Parameters, string Fingerprint, string PublicKey);
		const int AuthKeySize = 2048;

		readonly string filePath;
		readonly PlayerDatabase playerDatabase;

		volatile LinkState innerState = LinkState.Unlinked;
		volatile PlayerProfile profileData;
		volatile KeyData keyData;
		volatile bool refreshing;

		public LinkState State
		{
			get => innerState;
			private set
			{
				innerState = value;
				OnStateChanged();
			}
		}

		public event Action OnStateChanged = () => { };

		public string Fingerprint => keyData?.Fingerprint;
		public string PublicKey => keyData?.PublicKey;

		public PlayerProfile ProfileData => profileData;

		public LocalPlayerProfile(string filePath, PlayerDatabase playerDatabase)
		{
			this.filePath = filePath;
			this.playerDatabase = playerDatabase;
			LoadKeyPair();
			RefreshPlayerData();
		}

		void LoadKeyPair()
		{
			try
			{
				if (File.Exists(filePath))
				{
					using (var rsa = new RSACryptoServiceProvider())
					{
						using (var data = File.OpenRead(filePath))
						{
							var xmlData = Convert.FromBase64String(data.ReadAllText());
							rsa.FromXmlString(new string(Encoding.ASCII.GetChars(xmlData)));
						}

						var parameters = rsa.ExportParameters(true);
						var publicKey = CryptoUtil.EncodePEMPublicKey(parameters);
						var fingerprint = CryptoUtil.PublicKeyFingerprint(parameters);

						keyData = new KeyData(parameters, fingerprint, publicKey);
						innerState = LinkState.ConnectionFailed;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to load keys:");
				Console.WriteLine(e);
				Log.Write("debug", $"Failed to load player keypair from `{filePath}` with exception:");
				Log.Write("debug", e);
			}
		}

		public void DeleteKeypair()
		{
			try
			{
				File.Delete(filePath);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to delete keypair with exception:");
				Log.Write("debug", e);
				Console.WriteLine("Key deletion failed:");
				Console.WriteLine(e);
			}

			keyData = null;
			profileData = null;
			State = LinkState.Unlinked;
		}

		/// <summary>
		/// Player-initiated unlink (Settings → Account "Unlink", lobby "Destroy key").
		/// Deletes the local keypair first, then asks the player database to revoke
		/// the key so the device slot is freed. The revoke is best-effort and
		/// fire-and-forget: the key is gone locally whatever the service answers, and
		/// a PlayerDatabase without an Unlink URL (stock mods) skips the request.
		/// The request is a proof of possession: the key signs
		/// "unlink" + fingerprint + timestamp with itself; no credentials are sent.
		/// </summary>
		public void UnlinkAccount()
		{
			if (State != LinkState.Linked || keyData == null)
				return;

			var fingerprint = Fingerprint;
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
			var signature = Sign("unlink", fingerprint, timestamp);

			DeleteKeypair();

			if (string.IsNullOrEmpty(playerDatabase.Unlink) || signature == null)
				return;

			Task.Run(async () =>
			{
				try
				{
					var args = new Dictionary<string, string>
					{
						{ "fingerprint", fingerprint },
						{ "timestamp", timestamp },
						{ "signature", signature },
					};

					var client = HttpClientFactory.Create();
					client.DefaultRequestHeaders.Add("User-Agent", UserAgent());
					var httpResponseMessage = await client.PostAsync(playerDatabase.Unlink, new FormUrlEncodedContent(args));
					var result = await httpResponseMessage.Content.ReadAsStringAsync();
					Log.Write("debug", $"Unlink {fingerprint}: HTTP {(int)httpResponseMessage.StatusCode} {result}");
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Failed to revoke key {fingerprint} with exception:");
					Log.Write("debug", e);
				}
			});
		}

		static string UserAgent()
		{
			var manifest = Game.ModData.Manifest;
			var agentEngineVersion = Uri.EscapeDataString(Game.EngineVersion);
			var agentModId = Uri.EscapeDataString(manifest.Id);
			var agentModVersion = Uri.EscapeDataString(manifest.Metadata.Version);
			return $"OpenRA/{agentEngineVersion} {agentModId}/{agentModVersion}";
		}

		public void RefreshPlayerData()
		{
			if (refreshing || State == LinkState.Unlinked)
				return;

			refreshing = true;
			State = LinkState.CheckingLink;

			Task.Run(async () =>
			{
				try
				{
					var client = HttpClientFactory.Create();
					var url = playerDatabase.Profile + Fingerprint;
					var httpResponseMessage = await client.GetAsync(url);
					var result = await httpResponseMessage.Content.ReadAsStreamAsync();

					var yaml = MiniYaml.FromStream(result, url).First();
					if (yaml.Key == "Player")
					{
						var data = FieldLoader.Load<PlayerProfile>(yaml.Value);
						if (data.KeyRevoked)
						{
							Log.Write("debug", $"Revoking key with fingerprint {Fingerprint}");
							DeleteKeypair();
						}
						else
						{
							profileData = data;
							State = LinkState.Linked;
						}
					}
					else
					{
						Log.Write("debug", $"Unknown key with fingerprint {Fingerprint}");
						DeleteKeypair();
					}
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to parse player data result with exception:");
					Log.Write("debug", e);
					State = LinkState.ConnectionFailed;
				}

				refreshing = false;
			});
		}

		public void LinkForumAccount(string username, string password, Action<LinkResult> onComplete = null)
		{
			if (State != LinkState.Unlinked)
				return;

			State = LinkState.Linking;

			Task.Run(async () =>
			{
				try
				{
					var rsa = new RSACryptoServiceProvider(AuthKeySize);
					var parameters = rsa.ExportParameters(true);
					var publicKey = CryptoUtil.EncodePEMPublicKey(parameters);

					var args = new Dictionary<string, string>
					{
						{ "username", username },
						{ "password", password },
						{ "pubkey", publicKey },
					};

					var client = HttpClientFactory.Create();
					client.DefaultRequestHeaders.Add("User-Agent", UserAgent());
					var httpResponseMessage = await client.PostAsync(playerDatabase.Link, new FormUrlEncodedContent(args));
					var result = await httpResponseMessage.Content.ReadAsStringAsync();
					if (httpResponseMessage.IsSuccessStatusCode)
					{
						switch (result)
						{
							case "Success":
							case "Error: key exists":
							{
								var data = Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(true)));
								await File.WriteAllTextAsync(filePath, data);
								LoadKeyPair();
								onComplete?.Invoke(LinkResult.Success);
								RefreshPlayerData();
								return;
							}

							case "Error: authentication failed":
								State = LinkState.Unlinked;
								onComplete?.Invoke(LinkResult.AuthFailure);
								return;

							case "Error: too many login attempts":
								State = LinkState.Unlinked;
								onComplete?.Invoke(LinkResult.LoginAttempts);
								return;

							case "Error: banned":
								State = LinkState.Unlinked;
								onComplete?.Invoke(LinkResult.Banned);
								return;

							default:
								State = LinkState.Unlinked;
								onComplete?.Invoke(LinkResult.Error);
								return;
						}
					}

					State = LinkState.Unlinked;
					onComplete?.Invoke(LinkResult.ConnectionFailed);
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to link forum account with exception:");
					Log.Write("debug", e);
					State = LinkState.Unlinked;
					onComplete?.Invoke(LinkResult.Error);
				}
			});
		}

		public string Sign(params string[] data)
		{
			// If we don't have any keys, or we know for sure that they haven't been linked to the forum
			// then we can't do much here. If we have keys but don't yet know if they have been linked to the
			// forum (LinkState.CheckingLink or ConnectionFailed) then we sign to avoid blocking the main thread
			// but accept that - if the cert is invalid - the server will reject the result.
			if (State < LinkState.CheckingLink)
				return null;

			return CryptoUtil.Sign(keyData.Parameters, data.Where(x => !string.IsNullOrEmpty(x)).JoinWith(string.Empty));
		}

		public string DecryptString(string data)
		{
			if (State < LinkState.CheckingLink)
				return null;

			return CryptoUtil.DecryptString(keyData.Parameters, data);
		}
	}
}
