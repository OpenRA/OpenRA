using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa
{
	// Put globally-useful delegate types here, particularly if
	// they are generic.

	public delegate T Provider<T>();
	public delegate T Provider<T,U>( U u );
	public delegate T Provider<T,U,V>( U u, V v );
	public delegate void Action();
}
