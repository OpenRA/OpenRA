#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using ICSharpCode.SharpZipLib.Zip;

namespace OpenRA.Network
{
	public class GeoIP
	{
		class IP2LocationReader
		{
			public readonly DateTime Date;
			readonly Stream stream;
			readonly uint columnCount;
			readonly uint v4Count;
			readonly uint v4Offset;
			readonly uint v6Count;
			readonly uint v6Offset;

			public IP2LocationReader(Stream source)
			{
				// Copy stream data for reuse
				stream = new MemoryStream();
				source.CopyTo(stream);
				stream.Position = 0;

				if (stream.ReadUInt8() != 1)
					throw new InvalidDataException("Only IP2Location type 1 databases are supported.");

				columnCount = stream.ReadUInt8();
				var year = stream.ReadUInt8();
				var month = stream.ReadUInt8();
				var day = stream.ReadUInt8();
				Date = new DateTime(2000 + year, month, day);

				v4Count = stream.ReadUInt32();
				v4Offset = stream.ReadUInt32();
				v6Count = stream.ReadUInt32();
				v6Offset = stream.ReadUInt32();
			}

			BigInteger AddressForIndex(long index, bool isIPv6)
			{
				var start = isIPv6 ? v6Offset : v4Offset;
				var offset = isIPv6 ? 12 : 0;
				stream.Seek(start + index * (4 * columnCount + offset) - 1, SeekOrigin.Begin);
				return new BigInteger(stream.ReadBytes(isIPv6 ? 16 : 4).Append((byte)0).ToArray());
			}

			string CountryForIndex(long index, bool isIPv6)
			{
				// Read file offset for country entry
				var start = isIPv6 ? v6Offset : v4Offset;
				var offset = isIPv6 ? 12 : 0;
				stream.Seek(start + index * (4 * columnCount + offset) + offset + 3, SeekOrigin.Begin);
				var countryOffset = stream.ReadUInt32();

				// Read length-prefixed country name
				stream.Seek(countryOffset + 3, SeekOrigin.Begin);
				var length = stream.ReadUInt8();

				// "-" is used to represent an unknown country in the database
				var country = stream.ReadASCII(length);
				return country != "-" ? country : null;
			}

			public string LookupCountry(IPAddress ip)
			{
				var isIPv6 = ip.AddressFamily == AddressFamily.InterNetworkV6;
				if (!isIPv6 && ip.AddressFamily != AddressFamily.InterNetwork)
					return null;

				// Locate IP using a binary search
				// The IP2Location python parser has an additional
				// optimization that can jump directly to the row, but this adds
				// extra complexity that isn't obviously needed for our limited database size
				long low = 0;
				long high = isIPv6 ? v6Count : v4Count;

				// Append an empty byte to force the data to be treated as unsigned
				var ipNumber = new BigInteger(ip.GetAddressBytes().Reverse().Append((byte)0).ToArray());
				while (low <= high)
				{
					var mid = (low + high) / 2;
					var min = AddressForIndex(mid, isIPv6);
					var max = AddressForIndex(mid + 1, isIPv6);
					if (min <= ipNumber && ipNumber < max)
						return CountryForIndex(mid, isIPv6);

					if (ipNumber < min)
						high = mid - 1;
					else
						low = mid + 1;
				}

				return null;
			}
		}

		static IP2LocationReader database;

		public static void Initialize(string databasePath = "IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP")
		{
			if (!File.Exists(databasePath))
				return;

			try
			{
				using (var z = new ZipFile(databasePath))
				{
					var entry = z.FindEntry("IP2LOCATION-LITE-DB1.IPV6.BIN", false);
					database = new IP2LocationReader(z.GetInputStream(entry));
				}
			}
			catch (Exception e)
			{
				Log.Write("geoip", "DatabaseReader failed: {0}", e);
			}
		}

		public static string LookupCountry(IPAddress ip)
		{
			if (database != null)
			{
				try
				{
					return database.LookupCountry(ip);
				}
				catch (Exception e)
				{
					Log.Write("geoip", "LookupCountry failed: {0}", e);
				}
			}

			return null;
		}
	}
}
