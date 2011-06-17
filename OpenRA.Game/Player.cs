#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical };
	public enum WinState { Won, Lost, Undefined };

	public class Player
	{
		public Actor PlayerActor;
		public int Kills;
		public int Deaths;
		public WinState WinState = WinState.Undefined;

        public readonly ColorRamp ColorRamp;

		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly CountryInfo Country;
		public readonly bool NonCombatant = false;
		public readonly int ClientIndex;
		public readonly PlayerReference PlayerRef;
		public bool IsBot;

		public Shroud Shroud { get { return World.LocalShroud; }}
		public World World { get; private set; }

		public Player(World world, Session.Client client, Session.Slot slot, PlayerReference pr)
		{
			World = world;
			InternalName = pr.Name;
			PlayerRef = pr;
			string botType = null;
			
			if (client != null)
			{
				ClientIndex = client.Index;
            	ColorRamp = client.ColorRamp;
				PlayerName = client.Name;

				Country = world.GetCountries()
					.FirstOrDefault(c => client.Country == c.Race)
					?? world.GetCountries().Random(world.SharedRandom);
			}
			else
			{
				// Map player or bot
				ClientIndex = 0; 		/* it's a map player, "owned" by host */
				ColorRamp = pr.ColorRamp;
				PlayerName = pr.Name;
				NonCombatant = pr.NonCombatant;
				IsBot = pr.Bot != null;
				botType = pr.Bot;

				Country = world.GetCountries()
					.FirstOrDefault(c => pr.Race == c.Race)
					?? world.GetCountries().Random(world.SharedRandom);

				// Multiplayer bot
				if (slot != null && slot.Bot != null)
				{
					IsBot = true;
					botType = slot.Bot;
					PlayerName = slot.Bot;

					// pick a random color for the bot
					var hue = (byte)world.SharedRandom.Next(255);
					var sat = (byte)world.SharedRandom.Next(255);
					var lum = (byte)world.SharedRandom.Next(51,255);
					ColorRamp = new ColorRamp(hue, sat, lum, 10);
				}
			}
			
			PlayerActor = world.CreateActor("Player", new TypeDictionary { new OwnerInit(this) });

			// Enable the bot logic
			if (IsBot && Game.IsHost)
			{
				var logic = PlayerActor.TraitsImplementing<IBot>()
							.FirstOrDefault(b => b.Info.Name == botType);
				if (logic == null)
					Log.Write("debug", "Invalid bot type: {0}", botType);
				else
					logic.Activate(this);
			}
		}

		public void GiveAdvice(string advice)
		{
			Sound.PlayToPlayer(this, advice);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
	}
}
