using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.GameRules
{
	public class SupportPowerInfo
	{
		public readonly bool Powered = true;
		public readonly bool OneShot = false;
		public readonly float ChargeTime = 0;
		public readonly string Image;
		public readonly string Description = "";
		public readonly string LongDesc = "";
		public readonly string[] Prerequisite = { };
		public readonly int TechLevel = -1;
		public readonly bool GivenAuto = true;
		public readonly string Impl = null;
	}
}
