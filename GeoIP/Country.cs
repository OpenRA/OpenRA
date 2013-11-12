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
	public class Country
	{
		String code;
		String name;

		/*
	 * Creates a new Country.
	 *
	 * @param code the country code.
	 * @param name the country name.
	 */
		public Country(String code, String name)
		{
			this.code = code;
			this.name = name;
		}

		/*
	 * Returns the ISO two-letter country code of this country.
	 *
	 * @return the country code.
	 */
		public String getCode()
		{
			return code;
		}

		/*
     * Returns the name of this country.
     *
     * @return the country name.
     */
		public String getName()
		{
			return name;
		}
	}
}