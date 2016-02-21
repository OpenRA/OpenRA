#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using MaxMind.GeoIP2;

namespace OpenRA.Network
{
	public class GeoIP
	{
		static DatabaseReader database;

		public static void Initialize()
		{
			try
			{
				using (var fileStream = new FileStream("GeoLite2-Country.mmdb.gz", FileMode.Open, FileAccess.Read))
					using (var gzipStream = new GZipInputStream(fileStream))
						database = new DatabaseReader(gzipStream);
			}
			catch (Exception e)
			{
				Log.Write("geoip", "DatabaseReader failed: {0}", e);
			}
		}

		public static string LookupCountry(string ip)
		{
			const string Unknown = "Unknown Location";

			try
			{
				return database.Country(ip).Country.Name ?? Unknown;
			}
			catch (Exception e)
			{
				Log.Write("geoip", "LookupCountry failed: {0}", e);
				return Unknown;
			}
		}
	}
}