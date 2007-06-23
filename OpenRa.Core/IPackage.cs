using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.Core
{
	public interface IPackage
	{
		Stream GetItem(string filename);
	}
}
