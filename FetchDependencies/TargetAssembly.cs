using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using static FetchDependencies.FetchDependencies;

namespace FetchDependencies;

public class TargetAssembly : IDisposable {
	public TargetAssembly(string assemblyPath) {
		AssemblyPath = assemblyPath;
		var resolver = new DefaultAssemblyResolver();
		resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
		Assembly = AssemblyDefinition.ReadAssembly(AssemblyPath,
			new ReaderParameters { AssemblyResolver = resolver });
	}

	public AssemblyDefinition Assembly { get; }
	private string AssemblyPath { get; }

	public Version Version => Assembly.MainModule.Assembly.Name.Version;

	public void Dispose() {
		Assembly.Dispose();
	}

	public MethodDefinition GetMethod(string name) {
		return GetAllTypes()
			.Where(o => o.IsClass)
			.SelectMany(type => type.Methods)
			.First(o => o.FullName.Contains(name));
	}

	public void MakePublic() {
		static bool CheckCompilerGeneratedAttribute(ICustomAttributeProvider member) {
			return member.CustomAttributes.Any(x =>
				x.AttributeType.FullName ==
				"System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}

		foreach (var type in GetAllTypes()) {
			if (CheckCompilerGeneratedAttribute(type))
				continue;

			if (type.IsNested)
				type.IsNestedPublic = true;
			else
				type.IsPublic = true;

			foreach (var method in type.Methods.Where(method =>
				         !CheckCompilerGeneratedAttribute(method) &&
				         !method.IsCompilerControlled))
				method.IsPublic = true;

			foreach (var field in type.Fields.Where(field =>
				         !CheckCompilerGeneratedAttribute(field) &&
				         !field.IsCompilerControlled))
				field.IsPublic = true;
		}
	}

	public void RemoveStrongNaming() {
		var name = Assembly.Name;
		name.HasPublicKey = false;
		name.PublicKey = [];
		foreach (var module in Assembly.Modules) {
			module.Attributes &= ~ModuleAttributes.StrongNameSigned;
			string[] coreLibs = ["netstandard", "mscorlib", "System"];
			foreach (var reference in module.AssemblyReferences
				         .Where(reference => coreLibs.All(coreLib => reference.Name != coreLib))) {
				reference.HasPublicKey = false;
				reference.PublicKey = [];
			}
		}
	}

	private IEnumerable<TypeDefinition> GetAllTypes() {
		var types = new Queue<TypeDefinition>(Assembly.MainModule.Types);
		while (types.Count > 0) {
			var type = types.Dequeue();
			yield return type;
			foreach (var nestedType in type.NestedTypes)
				types.Enqueue(nestedType);
		}
	}

	public bool ApiVersionMatches() => Assembly.MainModule.Types.Any(type => type.Namespace == ApiVersion.NamespaceIdentifier && type.Name == "WasHere");


	public string? GetDieMoeBuildVersion() => Assembly.MainModule.Types
		.SelectMany(type => type.Fields, (type, field) => new { type, field })
		.Where(t => t.field.Name == "DieMoeBuildVersion")
		.Select(t => t.field)
		.Select(field => field.Constant.ToString() ?? null)
		.FirstOrDefault();

	public void WriteOut(string? outp = null) {
		if (!ApiVersionMatches()) {
			var wasHere = new TypeDefinition(ApiVersion.NamespaceIdentifier, "WasHere", TypeAttributes.Public | TypeAttributes.Class) {
				BaseType = Assembly.MainModule.TypeSystem.Object
			};
			Assembly.MainModule.Types.Add(wasHere);
		}
		if (!string.IsNullOrEmpty(RemoteDieMoeBuildVersion)) {
			var field = new FieldDefinition("DieMoeBuildVersion", FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly, Assembly.MainModule.TypeSystem.String) {
				Constant = RemoteDieMoeBuildVersion
			};
			Log.Info($"WriteOut Version {AssemblyPath}->{RemoteDieMoeBuildVersion}");
			var d = GetDieMoeBuildVersion();
			if (string.IsNullOrEmpty(GetDieMoeBuildVersion())) {
				Assembly.MainModule.Types.First().Fields.Add(field);
				Log.Info($"Added DieMoeBuildVersion {d}");
			} else
				Log.Info($"Detected DieMoeBuildVersion {d}");
		} else
			Log.Warning("RemoteDieMoeBuildVersion is Empty");
		var patchedPath = AssemblyPath + ".patched";

		Assembly.Write(patchedPath);
		Assembly.Dispose();
		var dst = outp ?? AssemblyPath;
		Log.Warning($"Moving {patchedPath}->{dst}");
		File.Move(patchedPath, dst, true);
	}
}