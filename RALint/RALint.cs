using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace RALint
{
	static class RALint
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
			Console.WriteLine("RALint(1,1): Error: {0}", e);
			++errors;
		}

		static Dictionary<string, int> ValidPrereqs;

		static int Main(string[] args)
		{
			InitializeEngineWithMods(args);

			// all the @something names which actually EXIST.
			var psuedoPrereqs = Rules.Info.Values.Select(a => a.Traits.GetOrDefault<BuildableInfo>()).Where(b => b != null)
				.Select(b => b.AlternateName).Where(n => n != null).SelectMany(a => a).Select(a => a.ToLowerInvariant()).Distinct();

			ValidPrereqs = Rules.Info.Keys.Concat(psuedoPrereqs).ToDictionary(a => a, a => 0);

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
					CheckReference(actorInfo, traitInfo, field, ValidPrereqs, "actor");
				if (field.HasAttribute<WeaponReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Weapons, "weapon");
				if (field.HasAttribute<VoiceReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Voices, "voice");
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
	}
}
