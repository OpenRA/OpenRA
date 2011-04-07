#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class SubCellInit : IActorInit<SubCell>
	{
		[FieldFromYamlKey]
		public readonly int value = 0;

		public SubCellInit() { }

		public SubCellInit(int init)
		{
			value = init;
		}

		public SubCell Value(World world)
		{
			return (SubCell)value;
		}
	}
}
