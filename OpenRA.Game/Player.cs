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
		public int Kills;

		public readonly string Palette;
		public readonly Color Color;
		
		public readonly string PlayerName;
		public readonly string InternalName;
		public readonly CountryInfo Country;
		public readonly int Index;

		public int Cash = 10000;
		public int Ore = 0;
		public int OreCapacity;
		public int DisplayCash = 0;
		public int PowerProvided = 0;
		public int PowerDrained = 0;

		public ShroudRenderer Shroud;
		public World World { get; private set; }

		public static List<Tuple<string, string, Color>> PlayerColors
		{
			get
			{
				return Game.world.WorldActor.Info.Traits.WithInterface<PlayerColorPaletteInfo>()
					.Where(p => p.Playable)
					.Select(p => Tuple.New(p.Name, p.DisplayName, p.Color))
					.ToList();
			}
		}

		public Player( World world, Session.Client client )
		{
			World = world;
			Shroud = new ShroudRenderer(this, world.Map);

			PlayerActor = world.CreateActor("Player", new int2(int.MaxValue, int.MaxValue), this);

			if (client != null)
			{
				Index = client.Index;
				Palette = PlayerColors[client.PaletteIndex % PlayerColors.Count()].a;
				Color = PlayerColors[client.PaletteIndex % PlayerColors.Count()].c;
				PlayerName = client.Name;
				InternalName = "Multi{0}".F(client.Index);
			}
			else
			{
				Index = -1;
				PlayerName = InternalName = "Neutral";
				Palette = "neutral";
				Color = Color.Gray;	// HACK HACK
			}

			Country = world.GetCountries()
				.FirstOrDefault(c => client != null && client.Country == c.Name)
				?? world.GetCountries().Random(world.SharedRandom);
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

		public Dictionary<Player, Stance> Stances = new Dictionary<Player, Stance>();
	}
}
