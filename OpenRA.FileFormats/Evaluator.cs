#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.FileFormats
{
	public static class Evaluator
	{
		public static int Evaluate(string expr)
		{
			return Evaluate(expr, new Dictionary<string, int>());
		}

		public static int Evaluate(string expr, Dictionary<string, int> syms)
		{
			var toks = Tokens(expr, "+-*/()");
			var postfix = ToPostfix(toks, syms);

			var s = new Stack<int>();

			foreach (var t in postfix)
			{
				switch (t[0])
				{
					case '+': ApplyBinop(s, (x, y) => y + x); break;
					case '-': ApplyBinop(s, (x, y) => y - x); break;
					case '*': ApplyBinop(s, (x, y) => y * x); break;
					case '/': ApplyBinop(s, (x, y) => y / x); break;
					default: s.Push(int.Parse(t)); break;
				}
			}
			return s.Pop();
		}

		static void ApplyBinop( Stack<int> s, Func<int,int,int> f )
		{
			var x = s.Pop();
			var y = s.Pop();
			s.Push( f(x,y) );
		}

		static IEnumerable<string> ToPostfix(IEnumerable<string> toks, Dictionary<string, int> syms)
		{
			var s = new Stack<string>();
			foreach (var t in toks)
			{
				if (t[0] == '(') s.Push(t);
				else if (t[0] == ')')
				{
					var temp = "";
					while ((temp = s.Pop()) != "(") yield return temp;
				}
				else if (char.IsNumber(t[0])) yield return t;
				else if (char.IsLetter(t[0])) 
				{
					if (!syms.ContainsKey(t))
						throw new InvalidOperationException("Substitution `{0}` undefined".F(t));
					    
					yield return syms[t].ToString();;
				}
				else
				{
					while (s.Count > 0 && Prec[t] <= Prec[s.Peek()]) yield return s.Pop();
					s.Push(t);
				}
			}
			
			while (s.Count > 0) yield return s.Pop();
		}

		static readonly Dictionary<string, int> Prec
			= new Dictionary<string, int> { { "+", 0 }, { "-", 0 }, { "*", 1 }, { "/", 1 }, { "(", -1 } };

		static IEnumerable<string> Tokens(string expr, string ops)
		{
			var s = "";
			foreach (var c in expr)
			{
				if (char.IsWhiteSpace(c))
				{
					if (s != "") yield return s;
					s = "";
				}
				else if (ops.Contains(c))
				{
					if (s != "") yield return s;
					s = "";
					yield return "" + c;
				}
				else
					s += c;
			}

			if (s != "") yield return s;
		}
	}
}
