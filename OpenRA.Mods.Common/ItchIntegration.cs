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
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	public class ItchIntegration : IGlobalModData
	{
		sealed class User
		{
			[JsonProperty("url")]
			public string Url { get; set; }

			[JsonProperty("gamer")]
			public bool Gamer { get; set; }

			[JsonProperty("id")]
			public int Id { get; set; }

			[JsonProperty("press_user")]
			public bool PressUser { get; set; }

			[JsonProperty("developer")]
			public bool Developer { get; set; }

			[JsonProperty("username")]
			public string Username { get; set; }

			[JsonProperty("display_name")]
			public string DisplayName { get; set; }
		}

		sealed class Root
		{
			public User User { get; set; }
		}

		public void GetPlayerName(Action<string> callback)
		{
			Task.Run(async () =>
			{
				User user = null;

				var apiKey = Environment.GetEnvironmentVariable("ITCHIO_API_KEY", EnvironmentVariableTarget.Process);
				if (!string.IsNullOrEmpty(apiKey))
				{
					try
					{
						var client = HttpClientFactory.Create();
						client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
						var httpResponseMessage = await client.GetAsync("https://itch.io/api/1/jwt/me");
						httpResponseMessage.EnsureSuccessStatusCode();
						var result = await httpResponseMessage.Content.ReadAsStringAsync();
						user = JsonConvert.DeserializeObject<Root>(result).User;
					}
					catch (Exception e)
					{
						Log.Write("debug", "Failed to query player name from itch.io API.");
						Log.Write("debug", e);
					}
				}

				var name = "";
				if (user != null)
				{
					if (string.IsNullOrEmpty(user.DisplayName))
						name = user.Username;
					else
						name = user.DisplayName;
				}

				Game.RunAfterTick(() => callback?.Invoke(name));
			});
		}
	}
}
