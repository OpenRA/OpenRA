using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Core
{
	public interface IMountable
	{
		Stream GetItem(string filename);
	}
}
