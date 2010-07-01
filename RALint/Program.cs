using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Traits;
using System.Reflection;

namespace RALint
{
	static class Program
	{
		/* todo: move this into the engine? dpstool, seqed, editor, etc all need it (or something similar) */
		static void InitializeEngineWithMods(string[] mods)
		{
			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;
			var manifest = new Manifest(mods);
			Game.LoadModAssemblies(manifest);

			FileSystem.UnmountAll();
			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			Rules.LoadRules(manifest, new Map());
		}

		static int errors = 0;

		static void EmitError(string e)
		{
			Console.WriteLine(e);
			++errors;
		}

		static int Main(string[] args)
		{
			InitializeEngineWithMods(args);

			foreach (var actorInfo in Rules.Info)
				foreach (var traitInfo in actorInfo.Value.Traits.WithInterface<ITraitInfo>())
					CheckTrait(actorInfo.Value, traitInfo);

			if (errors > 0)
			{
				Console.WriteLine("Errors: {0}", errors);
				return 1;
			}

			return 0;
		}

		static void CheckTrait(ActorInfo actorInfo, ITraitInfo traitInfo)
		{
			var actualType = traitInfo.GetType();
			foreach (var field in actualType.GetFields())
			{
				if (field.HasAttribute<ActorReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Info, "actor");
				if (field.HasAttribute<WeaponReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Weapons, "weapon");
			}
		}

		static string[] GetFieldValues(ITraitInfo traitInfo, FieldInfo fieldInfo)
		{
			var type = fieldInfo.FieldType;
			if (type == typeof(string))
				return new string[] { (string)fieldInfo.GetValue(traitInfo) };
			if (type == typeof(string[]))
				return (string[])fieldInfo.GetValue(traitInfo);

			EmitError("Bad type for reference on {0}.{1}. Supported types: string, string[]"
				.F(traitInfo.GetType().Name, fieldInfo.Name));

			return new string[] { };
		}

		static void CheckReference<T>(ActorInfo actorInfo, ITraitInfo traitInfo, FieldInfo fieldInfo,
			Dictionary<string, T> dict, string type)
		{
			var values = GetFieldValues(traitInfo, fieldInfo);
			foreach (var v in values)
				if (v != null && !dict.ContainsKey(v.ToLowerInvariant()))
					EmitError("{0}.{1}.{2}: Missing {3} `{4}`."
						.F(actorInfo.Name, traitInfo.GetType().Name, fieldInfo.Name, type, v));
		}

		static bool HasAttribute<T>(this MemberInfo mi)
		{
			return mi.GetCustomAttributes(typeof(T), true).Length != 0;
		}
	}
}
