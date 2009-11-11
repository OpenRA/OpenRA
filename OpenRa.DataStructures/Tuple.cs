using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa
{
	public class Tuple<A>
	{
		public A a;

		public Tuple(A a) { this.a = a; }
	}

	public class Tuple<A, B>
	{
		public A a;
		public B b;

		public Tuple(A a, B b) { this.a = a; this.b = b; }
	}

	public class Tuple<A, B, C>
	{
		public A a;
		public B b;
		public C c;

		public Tuple(A a, B b, C c) { this.a = a; this.b = b; this.c = c; }
	}

	public class Tuple<A, B, C, D>
	{
		public A a;
		public B b;
		public C c;
		public D d;

		public Tuple(A a, B b, C c, D d) { this.a = a; this.b = b; this.c = c; this.d = d; }
	}

	public static class Tuple
	{
		public static Tuple<A, B, C> New<A, B, C>(A a, B b, C c)
		{
			return new Tuple<A, B, C>(a, b, c);
		}
	}
}
