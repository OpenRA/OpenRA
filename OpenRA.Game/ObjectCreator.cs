using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using System.Reflection;
using System.IO;

namespace OpenRA
{
	public class ObjectCreator
	{
		Pair<Assembly, string>[] ModAssemblies;

		public ObjectCreator( Manifest manifest )
		{
			// All the core namespaces
			var asms = typeof(Game).Assembly.GetNamespaces()
				.Select(c => Pair.New(typeof(Game).Assembly, c))
				.ToList();

			// Namespaces from each mod assembly
			foreach (var a in manifest.Assemblies)
			{
				var asm = Assembly.LoadFile(Path.GetFullPath(a));
				asms.AddRange(asm.GetNamespaces().Select(ns => Pair.New(asm, ns)));
			}

			ModAssemblies = asms.ToArray();
		}

		public static Action<string> MissingTypeAction = 
			s => { throw new InvalidOperationException("Cannot locate type: {0}".F(s)); };

		public T CreateObject<T>(string classname)
		{
			foreach (var mod in ModAssemblies)
			{
				var fullTypeName = mod.Second + "." + classname;
				var obj = mod.First.CreateInstance(fullTypeName);
				if (obj != null)
					return (T)obj;
			}

			MissingTypeAction(classname);
			return default(T);
		}

	}
}
