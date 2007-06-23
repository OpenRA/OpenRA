using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Core
{
	public class ResourceBindingAttribute : Attribute
	{
		internal readonly string[] Extensions;

		public ResourceBindingAttribute(params string[] extensions)
		{
			Extensions = extensions;
		}
	}
}
