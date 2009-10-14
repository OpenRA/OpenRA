using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.GameRules
{
	class WarheadInfo
	{
		public readonly int Spread = 1;
		public readonly string Verses = "100%,100%,100%,100%,100%";
		public readonly bool Wall = false;
		public readonly bool Wood = false;
		public readonly bool Ore = false;
		public readonly int Explosion = 0;
		public readonly int InfDeath = 0;
	}
}
