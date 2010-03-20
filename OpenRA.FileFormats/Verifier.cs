#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace OpenRA.FileFormats
{
	public static class Verifier
	{
		static readonly string[] AllowedPatterns = {
		   "System.Collections.Generic.*",
		   "System.Linq.*",
		   "OpenRA.*",
		   "System.Collections.*",
		   "System.Func*",
		   "System.String:*",
		   "System.IDisposable:*",
		   "System.Action*",
		   "System.Object:*",
		   "System.Math:*",
		   "System.Predicate*",
		   "System.NotSupportedException:.ctor",
		   "System.Threading.Thread:get_CurrentThread",
		   "System.Threading.Thread:get_ManagedThreadId",
			
		   // Fixes to let the game run: should be checked by someone knowledgeable
		   "System.Threading.Interlocked:CompareExchange",
		   "System.Drawing.Color:*",
	   };

		public static bool IsSafe(string filename, List<string> failures)
		{
			Log.Write("Start verification: {0}", filename);

			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (s, a) => Assembly.ReflectionOnlyLoad(a.Name);
			var flags = BindingFlags.Instance | BindingFlags.Static |
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			var assembly = Assembly.ReflectionOnlyLoadFrom(filename);

			var pinvokes = assembly.GetTypes()
				.SelectMany(t => t.GetMethods(flags))
				.Where(m => (m.Attributes & MethodAttributes.PinvokeImpl) != 0)
				.Select(m => m.Name).ToArray();

			foreach (var pi in pinvokes)
				failures.Add("P/Invoke: {0}".F(pi));

			foreach (var fn in assembly
				.GetTypes()
				.SelectMany(x => x.GetMembers(flags))
				.SelectMany(x => FunctionsUsedBy(x))
				.Where(x => x.DeclaringType.Assembly != assembly)
				.Select(x => string.Format("{0}:{1}", x.DeclaringType.FullName, x.Name))
				.OrderBy(x => x)
				.Distinct())
				if (!IsAllowed(fn))
					failures.Add("Unsafe function: {0}".F(fn));

			return failures.Count == 0;
		}

		static bool IsAllowed(string fn)
		{
			foreach (var p in AllowedPatterns)
				if (p.EndsWith("*"))
				{
					if (fn.StartsWith(p.Substring(0, p.Length - 1))) return true;
				}
				else
				{
					if (fn == p) return true;
				}

			Log.Write(fn);
			
			return false;
		}

		static IEnumerable<MethodBase> FunctionsUsedBy( MemberInfo x )
		{
			if( x is MethodInfo )
			{
				var method = x as MethodInfo;
				if (method.GetMethodBody() != null)
					foreach( var fn in CheckIL( method.GetMethodBody().GetILAsByteArray(), x.Module, x.DeclaringType.GetGenericArguments(), method.GetGenericArguments() ) )
						yield return fn;
			}
			else if( x is ConstructorInfo )
			{
				var method = x as ConstructorInfo;
				if (method.GetMethodBody() != null)
					foreach( var fn in CheckIL( method.GetMethodBody().GetILAsByteArray(), x.Module, x.DeclaringType.GetGenericArguments(), new Type[ 0 ] ) )
						yield return fn;
			}
			else if( x is FieldInfo )
			{
				// ignore it
			}
			else if( x is PropertyInfo )
			{
				var prop = x as PropertyInfo;
				foreach( var method in prop.GetAccessors() )
					if (method.GetMethodBody() != null)
						foreach( var fn in CheckIL( method.GetMethodBody().GetILAsByteArray(), x.Module, x.DeclaringType.GetGenericArguments(), new Type[ 0 ] ) )
							yield return fn;
			}
			else if( x is Type )
			{
				// ... shouldn't happen, but does.... :O
			}
			else
				throw new NotImplementedException();
		}

		static IEnumerable<MethodBase> CheckIL( byte[] p, Module a, Type[] classGenerics, Type[] functionGenerics )
		{
			var position = 0;
			var ret = new List<int>();
			while( position < p.Length )
			{
				var opcode = OpCodeMap.GetOpCode( p, position );
				position += opcode.Size;
				if( opcode.OperandType == OperandType.InlineMethod )
					ret.Add( BitConverter.ToInt32( p, position ));
				position += OperandSize( opcode, p, position );
			}
			return ret.Select( t => a.ResolveMethod( t, classGenerics, functionGenerics ) );
		}

		static int OperandSize( OpCode opcode, byte[] p, int position )
		{
			switch( opcode.OperandType )
			{
			case OperandType.InlineNone:
				return 0;
			case OperandType.InlineMethod:
			case OperandType.InlineField:
			case OperandType.InlineType:
			case OperandType.InlineTok:
			case OperandType.InlineString:
			case OperandType.InlineI:
			case OperandType.InlineBrTarget:
				return 4;
			case OperandType.ShortInlineBrTarget:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				return 1;
			case OperandType.InlineSwitch:
				var numSwitchArgs = BitConverter.ToUInt32( p, position );
				return (int)( 4 + 4 * numSwitchArgs );
			case OperandType.ShortInlineR:
				return 4;
			default:
				throw new NotImplementedException("Unsupported: {0}".F(opcode.OperandType));
			}
		}
	}

	static class OpCodeMap
	{
		static readonly Dictionary<byte, OpCode> simpleOps = new Dictionary<byte, OpCode>();
		static readonly Dictionary<byte, OpCode> feOps = new Dictionary<byte, OpCode>();

		static OpCodeMap()
		{
			foreach( var o in typeof( OpCodes ).GetFields( BindingFlags.Static | BindingFlags.Public ).Select( f => (OpCode)f.GetValue( null ) ) )
			{
				if( o.Size == 1 )
					simpleOps.Add( (byte)o.Value, o );
				else if( o.Size == 2 )
					feOps.Add( (byte)( o.Value & 0xFF ), o );
				else
					throw new NotImplementedException();
			}
		}

		public static OpCode GetOpCode( byte[] input, int position )
		{
			if( input[ position ] != 0xFE )
				return simpleOps[ input[ position ] ];
			else
				return feOps[ input[ position + 1 ] ];
		}
	}
}
