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
	public class DatabaseInfo
	{
		public static int COUNTRY_EDITION = 1;
		public static int REGION_EDITION_REV0 = 7;
		public static int REGION_EDITION_REV1 = 3;
		public static int CITY_EDITION_REV0 = 6;
		public static int CITY_EDITION_REV1 = 2;
		public static int ORG_EDITION = 5;
		public static int ISP_EDITION = 4;
		public static int PROXY_EDITION = 8;
		public static int ASNUM_EDITION = 9;
		public static int NETSPEED_EDITION = 10;
		public static int  DOMAIN_EDITION = 11;
		public static int COUNTRY_EDITION_V6 = 12;
		public static int ASNUM_EDITION_V6 = 21;
		public static int ISP_EDITION_V6 = 22;
		public static int ORG_EDITION_V6 = 23;
		public static int DOMAIN_EDITION_V6 = 24;
		public static int CITY_EDITION_REV1_V6 = 30;
		public static int CITY_EDITION_REV0_V6 = 31;
		public static int NETSPEED_EDITION_REV1 = 32;
		public static int NETSPEED_EDITION_REV1_V6 = 33;

		private String info;
	   /**
		 * Creates a new DatabaseInfo object given the database info String.
		 * @param info
		 */

		public DatabaseInfo(String info)
		{
			this.info = info;
		}

		public int getType()
		{
			if ((info == null) | (info == ""))
			{
				return COUNTRY_EDITION;
			}
			else
			{
				// Get the type code from the database info string and then
				// subtract 105 from the value to preserve compatability with
				// databases from April 2003 and earlier.
				return Convert.ToInt32(info.Substring(4, 3)) - 105;
			}
		}

		/**
		 * Returns the date of the database.
		 *
		 * @return the date of the database.
		 */
		public DateTime getDate()
		{
			for (int i=0; i<info.Length-9; i++)
			{
				if (Char.IsWhiteSpace(info[i]) == true)
				{
					var dateString = info.Substring(i+1, 8);
					try
					{
						return DateTime.ParseExact(dateString,"yyyyMMdd",null);
					}
					catch (Exception e)
					{
						Console.Write(e.Message);
					}
					break;
				}
			}
			return DateTime.Now;
		}

		public String toString()
		{
			return info;
		}
	}
}