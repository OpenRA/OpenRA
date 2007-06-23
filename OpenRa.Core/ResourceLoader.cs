using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace OpenRa.Core
{
	static class ResourceLoader
	{
		static Dictionary<string, Converter<Stream, IResource>> loaders = 
			new Dictionary<string,Converter<Stream,IResource>>();

		static ResourceLoader()
		{
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
				BindTypes(a);

			AppDomain.CurrentDomain.AssemblyLoad +=
				delegate(object unused, AssemblyLoadEventArgs e) { BindTypes(e.LoadedAssembly); };
		}

		static void BindTypes(Assembly a)
		{
			foreach (Type t in a.GetTypes())
				BindType(t);
		}

		static void BindType(Type t)
		{
			ResourceBindingAttribute a = Reflect.GetAttribute<ResourceBindingAttribute>(t);
			if (a == null)
				return;

			ConstructorInfo ctor = t.GetConstructor(new Type[] { typeof(Stream) });
			if (ctor == null)
				return;

			Converter<Stream, IResource> loader = delegate(Stream s)
			{
				return (IResource)ctor.Invoke(new object[] { s });
			};

			foreach (string extension in a.Extensions)
				loaders.Add(extension, loader);
		}

		public static Converter<Stream, IResource> GetLoader(string extension)
		{
			Converter<Stream, IResource> result;
			loaders.TryGetValue(extension.ToLowerInvariant(), out result);
			return result;
		}
	}
}
