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

public class Region
{
	public String countryCode;
	public String countryName;
	public String region;

	public Region() { }

	public Region(String countryCode,String countryName,String region)
	{
		this.countryCode = countryCode;
		this.countryName = countryName;
		this.region = region;
	}

	public String getcountryCode()
	{
		return countryCode;
	}

	public String getcountryName()
	{
		return countryName;
	}

	public String getregion()
	{
		return region;
	}
}