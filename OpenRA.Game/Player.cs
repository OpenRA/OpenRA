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
		public readonly int Index;
		public readonly bool NonCombatant = false;
		public readonly int ClientIndex;
		public readonly PlayerReference PlayerRef;
		public bool IsBot;

		public Shroud Shroud { get { return World.LocalShroud; }}
		public World World { get; private set; }

		public Player(World world, Session.Client client, PlayerReference pr, int index)
		{
			World = world;
			Index = index;
			InternalName = pr.Name;
			PlayerRef = pr;
			
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
				ClientIndex = 0; 		/* it's a map player, "owned" by host */
				ColorRamp = pr.ColorRamp;
				PlayerName = pr.Name;
				NonCombatant = pr.NonCombatant;
				
				Country = world.GetCountries()
					.FirstOrDefault(c => pr.Race == c.Race)
					?? world.GetCountries().Random(world.SharedRandom);
			}
			
			PlayerActor = world.CreateActor("Player", new TypeDictionary { new OwnerInit(this) });
		}

		public void GiveAdvice(string advice)
		{
			Sound.PlayToPlayer(this, advice);
		}

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
	}
}
