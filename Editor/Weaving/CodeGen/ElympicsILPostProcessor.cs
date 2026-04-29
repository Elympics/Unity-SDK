using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Elympics.Editor.Weaving.Components;
using Elympics.Editor.Weaving.Components.Elympics;
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

        // String constants avoid assembly references to Elympics.asmdef / Elympics.Weaving.asmdef,
        // which can't be called as Unity APIs are unavailable in ILPostProcessor context.
        private const string ProcessedByElympicsAttributeFullName = "Elympics.Weaving.ProcessedByElympicsAttribute";
        private const string ElympicsRpcAttributeFullName = "Elympics.ElympicsRpcAttribute";

        public override ILPostProcessor GetInstance() => new ElympicsILPostProcessor();

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.References.Any(r =>
                Path.GetFileNameWithoutExtension(r) == ElympicsAssemblyName);

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            var assemblyDefinition = ReadAssembly(compiledAssembly);

            if (IsAlreadyProcessed(assemblyDefinition) || !HasAnyRpcMethods(assemblyDefinition))
                return new ILPostProcessResult(null);

            var diagnostics = new List<DiagnosticMessage>();
            var components = new ComponentController(new ElympicsRpcComponent());

            try
            {
                components.VisitModule(assemblyDefinition.MainModule);
            }
            catch (AggregateException ex)
            {
                diagnostics.AddRange(ex.InnerExceptions.Select(inner => Error(inner.Message, inner.StackTrace)));
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.Add(Error($"[Elympics Weaver] Error processing {compiledAssembly.Name}: {ex.Message}", ex.StackTrace));
                return new ILPostProcessResult(compiledAssembly.InMemoryAssembly, diagnostics);
            }

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

        private static AssemblyDefinition ReadAssembly(ICompiledAssembly compiledAssembly)
        {
            var resolver = new ILPostProcessorAssemblyResolver(compiledAssembly);
            var pdbData = compiledAssembly.InMemoryAssembly.PdbData;
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
                // Retry without symbols if portable PDB reading fails
                peStream.Position = 0;
                return AssemblyDefinition.ReadAssembly(peStream, new ReaderParameters
                {
                    ReadingMode = ReadingMode.Immediate,
                    InMemory = true,
                    AssemblyResolver = resolver,
                });
            }
            // Neither peStream nor pdbStream are disposed here.
            // When InMemory = true, Cecil stores the exact MemoryStream reference (not a copy) in
            // Image.MemoryStream and reads method bodies from it lazily. Disposing peStream causes
            // "Cannot access a closed Stream" when VisitMethod accesses methodDefinition.Body.
            // pdbStream is similarly held by PortablePdbReader through the Write phase.
            // Both are MemoryStream over managed byte arrays — no unmanaged resources, GC handles cleanup.
        }

        private static bool IsAlreadyProcessed(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.CustomAttributes.Any(a =>
                a.AttributeType.FullName == ProcessedByElympicsAttributeFullName);

        private static bool HasAnyRpcMethods(AssemblyDefinition assemblyDefinition) =>
            assemblyDefinition.MainModule.Types
                .SelectMany(t => t.Methods)
                .Any(m => m.HasCustomAttributes &&
                          m.CustomAttributes.Any(a =>
                              a.AttributeType.FullName == ElympicsRpcAttributeFullName));

        private static DiagnosticMessage Error(string message, string stackTrace, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new()
        {
            DiagnosticType = DiagnosticType.Error,
            MessageData = message + Environment.NewLine + stackTrace,
            File = filePath,
            Line = lineNumber,
        };
    }
}
