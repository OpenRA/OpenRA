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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Support;

namespace OpenRA
{
	public sealed class LocalPlayerProfile
	{
		const int AuthKeySize = 2048;
		public enum LinkState { Uninitialized, GeneratingKeys, Unlinked, CheckingLink, ConnectionFailed, Linked }

		public LinkState State => innerState;
		public string Fingerprint => innerFingerprint;
		public string PublicKey => innerPublicKey;

		public PlayerProfile ProfileData => innerData;

		volatile LinkState innerState;
		volatile PlayerProfile innerData;
		volatile string innerFingerprint;
		volatile string innerPublicKey;

		RSAParameters parameters;
		readonly string filePath;
		readonly PlayerDatabase playerDatabase;

		public LocalPlayerProfile(string filePath, PlayerDatabase playerDatabase)
		{
			this.filePath = filePath;
			this.playerDatabase = playerDatabase;
			innerState = LinkState.Uninitialized;

			try
			{
				if (File.Exists(filePath))
				{
					using (var rsa = new RSACryptoServiceProvider())
					{
						using (var data = File.OpenRead(filePath))
						{
							var keyData = Convert.FromBase64String(data.ReadAllText());
							rsa.FromXmlString(new string(Encoding.ASCII.GetChars(keyData)));
						}

						parameters = rsa.ExportParameters(true);
						innerPublicKey = CryptoUtil.EncodePEMPublicKey(parameters);
						innerFingerprint = CryptoUtil.PublicKeyFingerprint(parameters);
						innerState = LinkState.Unlinked;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to load keys: {0}", e);
				Log.Write("debug", "Failed to load player keypair from `{0}` with exception: {1}", filePath, e);
			}
		}

		public void RefreshPlayerData(Action onComplete = null)
		{
			if (State != LinkState.Unlinked && State != LinkState.Linked && State != LinkState.ConnectionFailed)
				return;

			Task.Run(async () =>
			{
				try
				{
					var client = HttpClientFactory.Create();

					var httpResponseMessage = await client.GetAsync(playerDatabase.Profile + Fingerprint);
					var result = await httpResponseMessage.Content.ReadAsStreamAsync();

					var yaml = MiniYaml.FromStream(result).First();
					if (yaml.Key == "Player")
					{
						innerData = FieldLoader.Load<PlayerProfile>(yaml.Value);
						if (innerData.KeyRevoked)
						{
							Log.Write("debug", "Revoking key with fingerprint {0}", Fingerprint);
							DeleteKeypair();
						}
						else
							innerState = LinkState.Linked;
					}
					else
						innerState = LinkState.Unlinked;
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to parse player data result with exception: {0}", e);
					innerState = LinkState.ConnectionFailed;
				}
				finally
				{
					onComplete?.Invoke();
				}
			});

			innerState = LinkState.CheckingLink;
		}

		public void GenerateKeypair()
		{
			if (State != LinkState.Uninitialized)
				return;

			innerState = LinkState.GeneratingKeys;
			new Task(() =>
			{
				try
				{
					var rsa = new RSACryptoServiceProvider(AuthKeySize);
					parameters = rsa.ExportParameters(true);
					innerPublicKey = CryptoUtil.EncodePEMPublicKey(parameters);
					innerFingerprint = CryptoUtil.PublicKeyFingerprint(parameters);

					var data = Convert.ToBase64String(Encoding.ASCII.GetBytes(rsa.ToXmlString(true)));
					File.WriteAllText(filePath, data);

					innerState = LinkState.Unlinked;
				}
				catch (Exception e)
				{
					Log.Write("debug", "Failed to generate keypair with exception: {1}", e);
					Console.WriteLine("Key generation failed: {0}", e);

					innerState = LinkState.Uninitialized;
				}
			}).Start();
		}

		public void DeleteKeypair()
		{
			try
			{
				File.Delete(filePath);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to delete keypair with exception: {1}", e);
				Console.WriteLine("Key deletion failed: {0}", e);
			}

			innerState = LinkState.Uninitialized;
			parameters = default;
			innerFingerprint = null;
			innerData = null;
		}

		public string Sign(params string[] data)
		{
			// If we don't have any keys, or we know for sure that they haven't been linked to the forum
			// then we can't do much here. If we have keys but don't yet know if they have been linked to the
			// forum (LinkState.CheckingLink or ConnectionFailed) then we sign to avoid blocking the main thread
			// but accept that - if the cert is invalid - the server will reject the result.
			if (State <= LinkState.Unlinked)
				return null;

			return CryptoUtil.Sign(parameters, data.Where(x => !string.IsNullOrEmpty(x)).JoinWith(string.Empty));
		}

		public string DecryptString(string data)
		{
			if (State <= LinkState.Unlinked)
				return null;

			return CryptoUtil.DecryptString(parameters, data);
		}
	}
}
