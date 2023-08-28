using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEditor;
using UnityEditor.Compilation;

namespace Elympics.Weaver
{
    public class WeaverAssemblyResolver : DefaultAssemblyResolver
    {
        public WeaverAssemblyResolver(string assemblyPath)
        {
            var compiledAssemblies = CompilationPipeline.GetAssemblies();
            var asm = compiledAssemblies.FirstOrDefault(x => FileUtility.Normalize(x.outputPath) == FileUtility.Normalize(assemblyPath))
                ?? compiledAssemblies.FirstOrDefault(x => Path.GetFileName(x.outputPath) == Path.GetFileName(assemblyPath));
            var dependencies = new HashSet<string> { Path.GetDirectoryName(assemblyPath) };
            if (asm != null)
            {
                _ = dependencies.Add(Path.GetDirectoryName(asm.outputPath));
                foreach (var refer in asm.compiledAssemblyReferences)
                    _ = dependencies.Add(Path.GetDirectoryName(refer));
            }
            foreach (var str in dependencies)
                AddSearchDirectory(str);
            AddSearchDirectory(Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data", "Managed"));
            var apiLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
            foreach (var systemDir in CompilationPipeline.GetSystemAssemblyDirectories(apiLevel))
                AddSearchDirectory(systemDir);
        }
    }
}
