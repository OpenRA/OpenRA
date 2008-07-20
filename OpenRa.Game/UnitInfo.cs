using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class UnitInfo
	{
		public readonly int Speed;
		public readonly SupportedMissions supportedMissions;

		public UnitInfo(string unitName, IniSection ini)
		{
			Speed = int.Parse(ini.GetValue("Speed", "0"));

			supportedMissions = SupportedMissions.Stop;
			if (unitName == "MCV")
				supportedMissions |= SupportedMissions.Deploy;
			if (unitName == "HARV")
				supportedMissions |= SupportedMissions.Harvest;
		}
	}
}
