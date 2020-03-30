#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.GZip;
using MaxMind.Db;

namespace OpenRA.Network
{
	public class GeoIP
	{
		public class GeoIP2Record
		{
			[Constructor]
			public GeoIP2Record(GeoIP2Country country)
			{
				Country = country;
			}

			public GeoIP2Country Country { get; set; }
		}

		public class GeoIP2Country
		{
			[Constructor]
			public GeoIP2Country(GeoIP2CountryNames names)
			{
				Names = names;
			}

			public GeoIP2CountryNames Names { get; set; }
		}

		public class GeoIP2CountryNames
		{
			[Constructor]
			public GeoIP2CountryNames(string en)
			{
				English = en;
			}

			public string English { get; set; }
		}

		static Reader database;

		public static void Initialize()
		{
			try
			{
				using (var fileStream = new FileStream("GeoLite2-Country.mmdb.gz", FileMode.Open, FileAccess.Read))
					using (var gzipStream = new GZipInputStream(fileStream))
						database = new Reader(gzipStream);
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
				var record = database.Find<GeoIP2Record>(IPAddress.Parse(ip));
				if (record != null)
					return record.Country.Names.English;
				else
					return Unknown;
			}
			catch (Exception e)
			{
				Log.Write("geoip", "LookupCountry failed: {0}", e);
				return Unknown;
			}
		}
	}
}
