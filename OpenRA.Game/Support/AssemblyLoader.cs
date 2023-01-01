#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

// Not used/usable on Mono. Only used for Dotnet Core.
// Based on https://github.com/natemcmaster/DotNetCorePlugins and used under the terms of the Apache 2.0 license
#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;

namespace OpenRA.Support
{
	public class AssemblyLoader
	{
		readonly string mainAssembly;
		readonly AssemblyLoadContext context;

		public Assembly LoadDefaultAssembly() => context.LoadFromAssemblyPath(mainAssembly);

		public AssemblyLoader(string assemblyFile)
		{
			mainAssembly = assemblyFile;
			var baseDir = Path.GetDirectoryName(assemblyFile);

			context = CreateLoadContext(baseDir, assemblyFile);
		}

		static AssemblyLoadContext CreateLoadContext(string baseDir, string assemblyFile)
		{
			var depsJsonFile = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(assemblyFile) + ".deps.json");

			var builder = new AssemblyLoadContextBuilder();

			builder.TryAddDependencyContext(depsJsonFile, out _);
			builder.SetBaseDirectory(baseDir);

			return builder.Build();
		}
	}

	public class AssemblyLoadContextBuilder
	{
		readonly Dictionary<string, ManagedLibrary> managedLibraries = new Dictionary<string, ManagedLibrary>(StringComparer.Ordinal);
		readonly Dictionary<string, NativeLibrary> nativeLibraries = new Dictionary<string, NativeLibrary>(StringComparer.Ordinal);
		string basePath;

		public AssemblyLoadContext Build()
		{
			return new ManagedLoadContext(basePath, managedLibraries, nativeLibraries);
		}

		public AssemblyLoadContextBuilder SetBaseDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentException("Argument must not be null or empty.", nameof(path));

			if (!Path.IsPathRooted(path))
				throw new ArgumentException("Argument must be a full path.", nameof(path));

			basePath = path;
			return this;
		}

		public AssemblyLoadContextBuilder AddManagedLibrary(ManagedLibrary library)
		{
			managedLibraries.Add(library.Name.Name, library);
			return this;
		}

		public AssemblyLoadContextBuilder AddNativeLibrary(NativeLibrary library)
		{
			ValidateRelativePath(library.AppLocalPath);
			nativeLibraries.Add(library.Name, library);
			return this;
		}

		static void ValidateRelativePath(string probingPath)
		{
			if (string.IsNullOrEmpty(probingPath))
				throw new ArgumentException("Value must not be null or empty.", nameof(probingPath));

			if (Path.IsPathRooted(probingPath))
				throw new ArgumentException("Argument must be a relative path.", nameof(probingPath));
		}
	}

	class ManagedLoadContext : AssemblyLoadContext
	{
		readonly string basePath;
		readonly Dictionary<string, ManagedLibrary> managedAssemblies;
		readonly Dictionary<string, NativeLibrary> nativeLibraries;

		static readonly string[] NativeLibraryExtensions;
		static readonly string[] NativeLibraryPrefixes;

		static readonly string[] ManagedAssemblyExtensions =
		{
			".dll",
			".ni.dll",
			".exe",
			".ni.exe"
		};

		static ManagedLoadContext()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				NativeLibraryPrefixes = new[] { "" };
				NativeLibraryExtensions = new[] { ".dll" };
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				NativeLibraryPrefixes = new[] { "", "lib", };
				NativeLibraryExtensions = new[] { ".dylib" };
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				NativeLibraryPrefixes = new[] { "", "lib" };
				NativeLibraryExtensions = new[] { ".so", ".so.1" };
			}
			else
			{
				NativeLibraryPrefixes = Array.Empty<string>();
				NativeLibraryExtensions = Array.Empty<string>();
			}
		}

		public ManagedLoadContext(string baseDirectory, Dictionary<string, ManagedLibrary> managedAssemblies, Dictionary<string, NativeLibrary> nativeLibraries)
		{
			basePath = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
			this.managedAssemblies = managedAssemblies ?? throw new ArgumentNullException(nameof(managedAssemblies));
			this.nativeLibraries = nativeLibraries ?? throw new ArgumentNullException(nameof(nativeLibraries));
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			// If default context is preferred, check first for types in the default context unless the dependency has been declared as private
			try
			{
				var defaultAssembly = Default.LoadFromAssemblyName(assemblyName);
				if (defaultAssembly != null)
					return null;
			}
			catch
			{
				// Swallow errors in loading from the default context
			}

			if (managedAssemblies.TryGetValue(assemblyName.Name, out var library) && SearchForLibrary(library, out var path))
				return LoadFromAssemblyPath(path);

			return null;
		}

		protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
		{
			foreach (var prefix in NativeLibraryPrefixes)
				if (nativeLibraries.TryGetValue(prefix + unmanagedDllName, out var library) && SearchForLibrary(library, prefix, out var path))
					return LoadUnmanagedDllFromPath(path);

			return base.LoadUnmanagedDll(unmanagedDllName);
		}

		bool SearchForLibrary(ManagedLibrary library, out string path)
		{
			// 1. Search in base path
			foreach (var ext in ManagedAssemblyExtensions)
			{
				var local = Path.Combine(basePath, library.Name.Name + ext);
				if (File.Exists(local))
				{
					path = local;
					return true;
				}
			}

			path = null;
			return false;
		}

		bool SearchForLibrary(NativeLibrary library, string prefix, out string path)
		{
			// 1. Search in base path
			foreach (var ext in NativeLibraryExtensions)
			{
				var candidate = Path.Combine(basePath, $"{prefix}{library.Name}{ext}");
				if (File.Exists(candidate))
				{
					path = candidate;
					return true;
				}
			}

			// 2. Search in base path + app local (for portable deployments of netcoreapp)
			var local = Path.Combine(basePath, library.AppLocalPath);
			if (File.Exists(local))
			{
				path = local;
				return true;
			}

			path = null;
			return false;
		}
	}

	public class ManagedLibrary
	{
		public AssemblyName Name { get; private set; }

		public static ManagedLibrary CreateFromPackage(string assetPath)
		{
			return new ManagedLibrary
			{
				Name = new AssemblyName(Path.GetFileNameWithoutExtension(assetPath))
			};
		}
	}

	public class NativeLibrary
	{
		public string Name { get; private set; }

		public string AppLocalPath { get; private set; }

		public static NativeLibrary CreateFromPackage(string assetPath)
		{
			return new NativeLibrary
			{
				Name = Path.GetFileNameWithoutExtension(assetPath),
				AppLocalPath = assetPath
			};
		}
	}

	public static class DependencyContextExtensions
	{
		public static AssemblyLoadContextBuilder TryAddDependencyContext(this AssemblyLoadContextBuilder builder, string depsFilePath, out Exception error)
		{
			error = null;
			try
			{
				builder.AddDependencyContext(depsFilePath);
			}
			catch (Exception ex)
			{
				error = ex;
			}

			return builder;
		}

		public static AssemblyLoadContextBuilder AddDependencyContext(this AssemblyLoadContextBuilder builder, string depsFilePath)
		{
			using (var reader = new DependencyContextJsonReader())
			{
				using (var file = File.OpenRead(depsFilePath))
				{
					var deps = reader.Read(file);
					builder.SetBaseDirectory(Path.GetDirectoryName(depsFilePath));
					builder.AddDependencyContext(deps);
				}
			}

			return builder;
		}

		static string GetFallbackRid()
		{
			string ridBase;

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				ridBase = "win10";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				ridBase = "linux";
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				ridBase = "osx.10.12";
			else
				return "any";

			switch (RuntimeInformation.OSArchitecture)
			{
				case Architecture.X86:
					return ridBase + "-x86";
				case Architecture.X64:
					return ridBase + "-x64";
				case Architecture.Arm:
					return ridBase + "-arm";
				case Architecture.Arm64:
					return ridBase + "-arm64";
			}

			return ridBase;
		}

		public static AssemblyLoadContextBuilder AddDependencyContext(this AssemblyLoadContextBuilder builder, DependencyContext dependencyContext)
		{
			var ridGraph = dependencyContext.RuntimeGraph.Count > 0
				? dependencyContext.RuntimeGraph
				: DependencyContext.Default.RuntimeGraph;

			var rid = RuntimeInformation.RuntimeIdentifier;
			var fallbackRid = GetFallbackRid();
			var fallbackGraph = ridGraph.FirstOrDefault(g => g.Runtime == rid)
								?? ridGraph.FirstOrDefault(g => g.Runtime == fallbackRid)
								?? new RuntimeFallbacks("any");

			foreach (var managed in dependencyContext.ResolveRuntimeAssemblies(fallbackGraph))
				builder.AddManagedLibrary(managed);

			foreach (var native in dependencyContext.ResolveNativeAssets(fallbackGraph))
				builder.AddNativeLibrary(native);

			return builder;
		}

		static IEnumerable<ManagedLibrary> ResolveRuntimeAssemblies(this DependencyContext depContext, RuntimeFallbacks runtimeGraph)
		{
			var rids = GetRids(runtimeGraph);
			return from library in depContext.RuntimeLibraries
				   from assetPath in SelectAssets(rids, library.RuntimeAssemblyGroups)
				   select ManagedLibrary.CreateFromPackage(assetPath);
		}

		static IEnumerable<NativeLibrary> ResolveNativeAssets(this DependencyContext depContext, RuntimeFallbacks runtimeGraph)
		{
			var rids = GetRids(runtimeGraph);
			return from library in depContext.RuntimeLibraries
				   from assetPath in SelectAssets(rids, library.NativeLibraryGroups)
				   where !assetPath.EndsWith(".a", StringComparison.Ordinal)
				   select NativeLibrary.CreateFromPackage(assetPath);
		}

		static IEnumerable<string> GetRids(RuntimeFallbacks runtimeGraph)
		{
			return Enumerable.Concat(new[] { runtimeGraph.Runtime }, runtimeGraph?.Fallbacks ?? Enumerable.Empty<string>());
		}

		static IEnumerable<string> SelectAssets(IEnumerable<string> rids, IEnumerable<RuntimeAssetGroup> groups)
		{
			foreach (var rid in rids)
			{
				var group = groups.FirstOrDefault(g => g.Runtime == rid);
				if (group != null)
					return group.AssetPaths;
			}

			return groups.GetDefaultAssets();
		}
	}
}
#endif
