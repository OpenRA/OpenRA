#region Copyright & License Information
/*
 * Copyright (C) 2008 MaxMind Inc.  All Rights Reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
#endregion

using System;

namespace GeoIP
{
	public class Location
	{
		public String countryCode;
		public String countryName;
		public String region;
		public String city;
		public String postalCode;
		public double latitude;
		public double longitude;
		public int dma_code;
		public int area_code;
		public String regionName;
		public int metro_code;

		private static double EARTH_DIAMETER = 2 * 6378.2;
		private static double PI = 3.14159265;
		private static double RAD_CONVERT = PI / 180;

		public double distance (Location loc)
		{
			double delta_lat, delta_lon;
			double temp;

			double lat1 = latitude;
			double lon1 = longitude;
			double lat2 = loc.latitude;
			double lon2 = loc.longitude;

			// convert degrees to radians
			lat1 *= RAD_CONVERT;
			lat2 *= RAD_CONVERT;

			// find the deltas
			delta_lat = lat2 - lat1;
			delta_lon = (lon2 - lon1) * RAD_CONVERT;

			// Find the great circle distance
			temp = Math.Pow(Math.Sin(delta_lat/2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(delta_lon/2), 2);
			return EARTH_DIAMETER * Math.Atan2(Math.Sqrt(temp), Math.Sqrt(1-temp));
		}
	}
}