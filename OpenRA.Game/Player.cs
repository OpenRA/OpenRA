#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	public enum PowerState { Normal, Low, Critical };

	public class Player
	{
		public Actor PlayerActor;
		public int PaletteIndex;
		public int Kills;
		public string PlayerName;
		public string InternalName;
		public CountryInfo Country;
		public readonly int Index;
		public int Cash = 10000;
		public int Ore = 0;
		public int OreCapacity;
		public int DisplayCash = 0;
		public int PowerProvided = 0;
		public int PowerDrained = 0;
		public int SpawnPointIndex = 0;

		public World World { get { return PlayerActor.World; } }

		public static List<Tuple<string, string, Color>> PlayerColors = new List<Tuple<string, string, Color>>();
		public static void ResetPlayerColorList()
		{
			// This is unsafe if the mapchange introduces/removes mods that defines new colors
			// TODO: ensure that each player's palette index is reassigned appropriately
			PlayerColors = new List<Tuple<string, string, Color>>();
		}
		
		public static void RegisterPlayerColor(string palette, string name, Color c)
		{
			PlayerColors.Add(new Tuple<string, string, Color>(palette, name, c));
		}

		public Color Color
		{
			get { return PlayerColors[PaletteIndex].c; }
		}
		
		public string Palette
		{
			get { return PlayerColors[PaletteIndex].a; }
		}

		public Shroud Shroud;

		public Player( World world, int index, Session.Client client )
		{
			Shroud = new Shroud(this, world.Map);
			this.PlayerActor = world.CreateActor("Player", new int2(int.MaxValue, int.MaxValue), this);
			this.Index = index;
			this.InternalName = "Multi{0}".F(index);

			this.PaletteIndex = client != null ? client.PaletteIndex : index;
			this.PlayerName = client != null ? client.Name : "Player {0}".F(index+1);
			this.Country = world.GetCountries().FirstOrDefault( c => client != null && client.Country == c.Name )
			?? world.GetCountries().First();
		}
	
		void UpdatePower()
		{
			var oldBalance = PowerProvided - PowerDrained;

			PowerProvided = 0;
			PowerDrained = 0;

			var myBuildings = World.Queries.OwnedBy[this]
				.WithTrait<Building>();

			foreach (var a in myBuildings)
			{
				var p = a.Trait.GetPowerUsage();
				if (p > 0)
					PowerProvided += p;
				else 
					PowerDrained -= p;
			}

			if (PowerProvided - PowerDrained < 0)
				if (PowerProvided - PowerDrained  != oldBalance)
					GiveAdvice(World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().LowPower);
		}
				
		public float GetSiloFullness()
		{
			return (float)Ore / OreCapacity;
		}

		public PowerState GetPowerState()
		{
			if (PowerProvided >= PowerDrained) return PowerState.Normal;
			if (PowerProvided > PowerDrained / 2) return PowerState.Low;
			return PowerState.Critical;
		}

		void UpdateOreCapacity()
		{
			OreCapacity = World.Queries.OwnedBy[this]
				.Where(a => a.traits.Contains<StoresOre>())
				.Select(a => a.Info.Traits.Get<StoresOreInfo>())
				.Sum(b => b.Capacity);
		}

		void GiveAdvice(string advice)
		{
			// todo: store the condition or something.
			// repeat after Rules.General.SpeakDelay, as long as the condition holds.
			Sound.PlayToPlayer(this, advice);
		}

		public void GiveCash( int num ) { Cash += num; }
		public void GiveOre(int num)
		{
			Ore += num;

			if (Ore > OreCapacity)
				Ore = OreCapacity;		// trim off the overflow.

			if (Ore > .8 * OreCapacity)
				GiveAdvice(World.WorldActor.Info.Traits.Get<EvaAlertsInfo>().SilosNeeded);
		}

		public bool TakeCash( int num )
		{
			if (Cash + Ore < num) return false;
			if (Ore <= num)
			{
				num -= Ore;
				Ore = 0;
				Cash -= num;
			}
			else
				Ore -= num;
			
			return true;
		}

		const float displayCashFracPerFrame = .07f;
		const int displayCashDeltaPerFrame = 37;

		public void Tick()
		{
			UpdatePower();
			UpdateOreCapacity();
			Shroud.Tick( World );

			var totalMoney = Cash + Ore;
			var diff = Math.Abs(totalMoney - DisplayCash);
			var move = Math.Min(Math.Max((int)(diff * displayCashFracPerFrame),
					displayCashDeltaPerFrame), diff);
			
			var eva = World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			if (DisplayCash < totalMoney)
			{
				DisplayCash += move;
				Sound.PlayToPlayer(this, eva.CashTickUp);
			}
			else if (DisplayCash > totalMoney)
			{
				DisplayCash -= move;
				Sound.PlayToPlayer(this, eva.CashTickDown);
			}
		}

		public void SyncFromLobby(Session.Client client)
		{
			if (PlayerName != client.Name)
			{
				Game.chat.AddLine(this, "is now known as " + client.Name);
				PlayerName = client.Name;
			}

			if (string.IsNullOrEmpty(client.Country))
				client.Country = PlayerActor.World.GetCountries().First().Name;

			if (Country.Name != client.Country)
			{
				Game.chat.AddLine(this, "is now playing {0}".F(client.Country));
				Country = PlayerActor.World.GetCountries().First(c => c.Name == client.Country);
			}

			if (PaletteIndex != client.PaletteIndex)
			{
				PaletteIndex = client.PaletteIndex;
				Game.chat.AddLine(this, "has changed color to {0}".F(PlayerColors[client.PaletteIndex].b));
			}

			if (SpawnPointIndex != client.SpawnPoint)
			{
				SpawnPointIndex = client.SpawnPoint;
				Game.chat.AddLine(this, "has changed spawn point to {0}".F(client.SpawnPoint));
			}
		}
	}
}
