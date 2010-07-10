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


namespace OpenRA.Traits
{
	public class EvaAlertsInfo : TraitInfo<EvaAlerts>
	{
		// Sound effects
		public readonly string TabClick = "ramenu1.aud";
		public readonly string RadarUp = "radaron2.aud";
		public readonly string RadarDown = "radardn1.aud";

		public readonly string CashTickUp = "cashup1.aud";
		public readonly string CashTickDown = "cashdn1.aud";
		
		// Build Palette
		public readonly string BuildPaletteOpen = "bleep13.aud";
		public readonly string BuildPaletteClose = "bleep13.aud";
		public readonly string BuildingSelectAudio = "abldgin1.aud";
		public readonly string BuildingReadyAudio = "conscmp1.aud";
		public readonly string BuildingCannotPlaceAudio = "nodeply1.aud";
		public readonly string UnitSelectAudio = "train1.aud";
		public readonly string UnitReadyAudio = "unitrdy1.aud";
		public readonly string OnHoldAudio = "onhold1.aud";
		public readonly string CancelledAudio = "cancld1.aud";
		public readonly string NewOptions = "newopt1.aud";
		
		// For manual powerup/down in ra-ng
		public readonly string DisablePower = "bleep11.aud";
		public readonly string EnablePower = "bleep12.aud";

		// Eva speech
		public readonly string LowPower = "lopower1.aud";
		public readonly string SilosNeeded = "silond1.aud";
		public readonly string UnitLost = "unitlst1.aud";
		public readonly string NavalUnitLost = "navylst1.aud";
		public readonly string PrimaryBuildingSelected = "pribldg1.aud";
		public readonly string CreditsStolen = "credit1.aud";
		
		// Special powers
		public readonly string AbilityInsufficientPower = "nopowr1.aud";
		
		public readonly string LevelUp = "hydrod1.aud";
	}

	public class EvaAlerts {}
}
