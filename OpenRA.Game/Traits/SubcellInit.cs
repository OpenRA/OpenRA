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
