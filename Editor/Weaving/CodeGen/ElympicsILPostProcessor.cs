using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elympics.Editor.Weaving.Components;
using Elympics.Editor.Weaving.Components.Elympics;
using Elympics.Weaving;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

#nullable enable

namespace Elympics.Editor.CodeGen
{
    [UsedImplicitly]
    internal class ElympicsILPostProcessor : ILPostProcessor
    {
        private const string ElympicsAssemblyName = "Elympics";

        public override ILPostProcessor GetInstance() => new ElympicsILPostProcessor();

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.References.Any(r => Path.GetFileNameWithoutExtension(r) == ElympicsAssemblyName);

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var diagnostics = new List<DiagnosticMessage>();
            var diagnosticsLogger = new DiagnosticsLogger(diagnostics, $"[Elympics Weaver] Error processing {compiledAssembly.Name}: ");
            try
            {
                using var resolver = new ILPostProcessorAssemblyResolver(compiledAssembly);
                var assemblyDefinition = ReadAssembly(compiledAssembly, resolver);

                if (IsAlreadyProcessed(assemblyDefinition) || !HasAnyRpcMethods(assemblyDefinition))
                    return new ILPostProcessResult(null);

                var components = new ComponentController(new ElympicsRpcComponent());
                components.VisitModule(assemblyDefinition.MainModule);

                var outPe = new MemoryStream();
                var outPdb = new MemoryStream();
                assemblyDefinition.Write(outPe, new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    SymbolStream = outPdb,
                });

                return new ILPostProcessResult(
                    new InMemoryAssembly(outPe.ToArray(), outPdb.ToArray()),
                    diagnostics);
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                    diagnosticsLogger.LogException(inner);
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
            }
            catch (Exception ex)
            {
                diagnosticsLogger.LogException(ex);
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
            }
        }

        private static AssemblyDefinition ReadAssembly(ICompiledAssembly compiledAssembly, ILPostProcessorAssemblyResolver resolver)
        {
            var pdbData = compiledAssembly.InMemoryAssembly.PdbData;
            // not using var to prevent the "Cannot access a closed Stream" exception
            // instead, InMemory = true is used in ReaderParameters
            var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
            var pdbStream = pdbData != null ? new MemoryStream(pdbData) : null;

            try
            {
                var readerParameters = new ReaderParameters
                {
                    ReadingMode = ReadingMode.Immediate,
                    InMemory = true,
                    AssemblyResolver = resolver,
                    ReadSymbols = pdbStream != null,
                    SymbolReaderProvider = pdbStream != null ? new PortablePdbReaderProvider() : null,
                    SymbolStream = pdbStream,
                };
                return AssemblyDefinition.ReadAssembly(peStream, readerParameters);
            }
            catch
            {
                // retry without symbols if portable PDB reading fails
                peStream.Position = 0;
                return AssemblyDefinition.ReadAssembly(peStream, new ReaderParameters
                {
                    ReadingMode = ReadingMode.Immediate,
                    InMemory = true,
                    AssemblyResolver = resolver,
                });
            }
        }

        private static bool IsAlreadyProcessed(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.CustomAttributes.Any(a =>
                a.AttributeType.FullName == typeof(ProcessedByElympicsAttribute).FullName);

        /// <remarks>
        /// Only checks top-level types (skips nested classes).
        /// </remarks>
        /// <param name="assemblyDefinition">Assembly to check.</param>
        /// <returns><c>true</c> if the assembly has RPC methods in its top-level classes, <c>false</c> otherwise.</returns>
        private static bool HasAnyRpcMethods(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.MainModule.Types
                .SelectMany(t => t.Methods)
                .Any(m => m.HasCustomAttributes &&
                          m.CustomAttributes.Any(a =>
                              a.AttributeType.FullName == typeof(ElympicsRpcAttribute).FullName));
    }
}
