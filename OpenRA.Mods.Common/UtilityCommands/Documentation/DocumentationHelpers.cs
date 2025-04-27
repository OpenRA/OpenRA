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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using OpenRA.Mods.Common.UtilityCommands.Documentation.Objects;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands.Documentation
{
	public static class DocumentationHelpers
	{
		// CustomDebugInformation specification.
		// https://github.com/dotnet/roslyn/blob/main/src/Dependencies/CodeAnalysis.Debugging/PortableCustomDebugInfoKinds.cs
		static readonly Guid TypeDefinitionDocumentGuid = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");

		public static IEnumerable<ExtractedClassFieldInfo> GetClassFieldInfos(Type type, IEnumerable<FieldLoader.FieldLoadInfo> fields,
			HashSet<Type> relatedEnumTypes, ObjectCreator objectCreator)
		{
			return fields
				.Select(fi =>
				{
					if (fi.Field.FieldType.IsEnum)
						relatedEnumTypes.Add(fi.Field.FieldType);

					return new ExtractedClassFieldInfo
					{
						PropertyName = fi.YamlName,
						DefaultValue = FieldSaver.SaveField(objectCreator.CreateBasic(type), fi.Field.Name).Value.Value,
						InternalType = Util.InternalTypeName(fi.Field.FieldType),
						UserFriendlyType = Util.FriendlyTypeName(fi.Field.FieldType),
						Description = string.Join(" ", Utility.GetCustomAttributes<DescAttribute>(fi.Field, true).SelectMany(d => d.Lines)),
						OtherAttributes = fi.Field.CustomAttributes
							.Where(a => a.AttributeType.Name != nameof(DescAttribute) && a.AttributeType.Name != nameof(FieldLoader.LoadUsingAttribute))
							.Select(a =>
							{
								var name = a.AttributeType.Name;
								name = name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^9] : name;

								return new ExtractedClassFieldAttributeInfo
								{
									Name = name,
									Parameters = a.Constructor.GetParameters()
										.Select(pi => new ExtractedClassFieldAttributeInfo.Parameter
										{
											Name = pi.Name,
											Value = Util.GetAttributeParameterValue(a.ConstructorArguments[pi.Position])
										})
								};
							})
					};
				});
		}

		public static IEnumerable<ExtractedEnumInfo> GetRelatedEnumInfos(
			HashSet<Type> relatedEnumTypes, Cache<string, IReadOnlyDictionary<string, ImmutableArray<string>>> pdbTypesCache)
		{
			return relatedEnumTypes.OrderBy(t => t.Name).Select(type => new ExtractedEnumInfo
			{
				Namespace = type.Namespace,
				Name = type.Name,
				Filename = GetSourceFilenameForType(type, pdbTypesCache),
				Values = Enum.GetNames(type).ToDictionary(x => Convert.ToInt32(Enum.Parse(type, x), NumberFormatInfo.InvariantInfo), y => y)
			});
		}

		public static Cache<string, IReadOnlyDictionary<string, ImmutableArray<string>>> CreatePdbTypesCache()
		{
			return new Cache<string, IReadOnlyDictionary<string, ImmutableArray<string>>>(BuildTypeMap);
		}

		public static string GetSourceFilenameForType(Type type,
			Cache<string, IReadOnlyDictionary<string, ImmutableArray<string>>> pdbTypesCache)
		{
			foreach (var file in pdbTypesCache[type.Assembly.Location])
				foreach (var t in file.Value)
					if (t.EndsWith($".{type.Name}", StringComparison.InvariantCultureIgnoreCase))
						return file.Key;

			return "(unknown)";
		}

		/// <summary>
		/// Builds a map of document → contained types for the given assembly.
		/// </summary>
		static ReadOnlyDictionary<string, ImmutableArray<string>> BuildTypeMap(string assemblyPath)
		{
			// Open the PE (DLL/EXE) and get a MetadataReader for the TypeDefinitions.
			var dllBytes = File.ReadAllBytes(assemblyPath).ToImmutableArray();
			var pe = new PEReader(dllBytes);
			var peReader = pe.GetMetadataReader();

			// Open the PDB and get a MetadataReader for the Documents.
			var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
			if (!Path.Exists(pdbPath))
				return ReadOnlyDictionary<string, ImmutableArray<string>>.Empty;

			var pdbBytes = File.ReadAllBytes(pdbPath).ToImmutableArray();
			var pdbProvider = MetadataReaderProvider.FromPortablePdbImage(pdbBytes);
			var pdbReader = pdbProvider.GetMetadataReader();

			var typesPerFile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			foreach (var typeDefinitionHandle in peReader.TypeDefinitions)
			{
				var typeDefinition = peReader.GetTypeDefinition(typeDefinitionHandle);
				var typeName = $"{peReader.GetString(typeDefinition.Namespace)}.{peReader.GetString(typeDefinition.Name)}";
				var documents = GetDocumentsForType(typeDefinition, typeDefinitionHandle, pdbReader);

				foreach (var documentHandle in documents)
				{
					var filePath = pdbReader.GetString(pdbReader.GetDocument(documentHandle).Name);

					// Remove the common path prefix to give a path relative to the repository root.
					for (var i = 0; i < filePath.Length; i++)
					{
						if (filePath[i] != assemblyPath[i])
						{
							filePath = filePath[i..];
							break;
						}
					}

					if (!typesPerFile.TryGetValue(filePath, out var list))
						typesPerFile[filePath] = list = [];

					list.Add(typeName);
				}
			}

			return new ReadOnlyDictionary<string, ImmutableArray<string>>(typesPerFile.ToDictionary(x => x.Key, y => y.Value.ToImmutableArray()));
		}

		/// <summary>
		/// <para>Collects all source documents that can be associated with a given type.</para>
		/// <para>
		/// A document may be associated with a type in multiple ways:
		///   1. Via methods declared on the type (method-level debug info).
		///   2. Via sequence points inside those methods (IL-to-source mappings).
		///   3. Via a type-level fallback document stored in custom debug information
		///      when the type has no debuggable methods.
		/// </para>
		/// </summary>
		/// <returns>A set of unique document handles.</returns>
		static HashSet<DocumentHandle> GetDocumentsForType(TypeDefinition typeDefinition, TypeDefinitionHandle typeDefinitionHandle, MetadataReader pdbReader)
		{
			var documents = new HashSet<DocumentHandle>();

			// Collect documents referenced by methods declared on the type.
			// This includes:
			//   - The primary document associated with the method itself
			//   - Any additional documents referenced by sequence points within the method body
			//
			// Sequence points are required because a single method can map to multiple
			// source files (e.g. partial methods, generated code, or inlined logic).
			foreach (var methodDefinitionHandle in typeDefinition.GetMethods())
			{
				var methodDebugInformation = pdbReader.GetMethodDebugInformation(methodDefinitionHandle);
				if (!methodDebugInformation.Document.IsNil)
					documents.Add(methodDebugInformation.Document);

				foreach (var sequencePoint in methodDebugInformation.GetSequencePoints())
					if (!sequencePoint.Document.IsNil)
						documents.Add(sequencePoint.Document);
			}

			// Fallback for types with no method-level debug information.
			//
			// Some types (e.g. empty types, marker interfaces, or types stripped of methods)
			// still have an associated source document recorded at the type level.
			// This information is stored as custom debug information (CDI) on the type.
			//
			// We scan the type's custom debug records and extract the document only if the
			// CDI kind matches the well-known TypeDefinitionDocument GUID.
			foreach (var customDebugInformationHandle in pdbReader.GetCustomDebugInformation(typeDefinitionHandle))
			{
				var customDebugInformation = pdbReader.GetCustomDebugInformation(customDebugInformationHandle);
				if (pdbReader.GetGuid(customDebugInformation.Kind) != TypeDefinitionDocumentGuid)
					continue;

				var blobReader = pdbReader.GetBlobReader(customDebugInformation.Value);
				while (blobReader.Offset < blobReader.Length)
					documents.Add(MetadataTokens.DocumentHandle(blobReader.ReadCompressedInteger()));
			}

			return documents;
		}
	}
}
