using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa
{
	public class DisposableAction : IDisposable
	{
		public DisposableAction(Action a) { this.a = a; }

		Action a;
		bool disposed;

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			a();
			GC.SuppressFinalize(this);
		}

		~DisposableAction()
		{
			Dispose();
		}
	}
}
