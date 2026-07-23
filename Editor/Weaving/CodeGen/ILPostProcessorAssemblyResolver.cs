#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Elympics.Editor.Weaving.Components;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Elympics.Editor.CodeGen
{
    /// <summary>
    /// Resolves assemblies strictly from compiledAssembly.References - no directory scanning.
    /// <remarks>
    /// <see cref="DefaultAssemblyResolver"/>searches all reference directories (including Library/ScriptAssemblies),
    /// which can make Cecil accidentally import references to editor-only assemblies (e.g. Unity.Elympics.Editor.CodeGen)
    /// into the processed runtime assembly. Burst then fails to resolve those editor references.
    /// </remarks>
    /// </summary>
    internal sealed class ILPostProcessorAssemblyResolver : IAssemblyResolver
    {
        private readonly ICompiledAssembly _compiledAssembly;
        private readonly Dictionary<string, string> _referencesByName;
        private readonly Dictionary<string, AssemblyDefinition> _cache = new();
        private AssemblyDefinition? _selfAssembly;
        private readonly ILogger? _logger;

        public ILPostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly, ILogger? logger = null)
        {
            _logger = logger;
            _compiledAssembly = compiledAssembly;
            _referencesByName = new Dictionary<string, string>();
            foreach (var reference in compiledAssembly.References)
            {
                var name = Path.GetFileNameWithoutExtension(reference);
                if (!string.IsNullOrEmpty(name))
                    _referencesByName[name] = reference;
            }
        }

        public AssemblyDefinition? Resolve(AssemblyNameReference name) => Resolve(name, null);

        public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters? parameters)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (name.Name == _compiledAssembly.Name)
                return _selfAssembly ??= AssemblyDefinition.ReadAssembly(
                    new MemoryStream(_compiledAssembly.InMemoryAssembly.PeData),
                    new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true, AssemblyResolver = this });

            if (_cache.TryGetValue(name.Name, out var cached))
                return cached;

            if (!_referencesByName.TryGetValue(name.Name, out var path) || !File.Exists(path))
            {
                _logger?.LogInfo("Cannot find assembly: " + name.Name + "| cache: " + string.Join(", ", _cache.Keys) + "| references: " + string.Join(", ", _referencesByName.Keys));
                return null;
            }

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(path,
                new ReaderParameters { ReadingMode = ReadingMode.Immediate, InMemory = true, AssemblyResolver = this });
            _cache[name.Name] = assemblyDefinition;
            return assemblyDefinition;
        }

        public void Dispose()
        {
            foreach (var asm in _cache.Values)
                asm?.Dispose();
            _selfAssembly?.Dispose();
        }
    }
}
