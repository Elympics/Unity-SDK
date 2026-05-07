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

        /// <remarks>In the form of string constant to avoid referencing Elympics.Weaving.dll</remarks>
        private const string ProcessedByElympicsAttributeFullName = "Elympics.Weaving.ProcessedByElympicsAttribute";
        /// <remarks>In the form of string constant to avoid referencing Elympics.dll</remarks>
        private const string ElympicsRpcAttributeFullName = "Elympics.ElympicsRpcAttribute";

        public override ILPostProcessor GetInstance() => new ElympicsILPostProcessor();

        public override bool WillProcess(ICompiledAssembly compiledAssembly) =>
            compiledAssembly.References.Any(r => Path.GetFileNameWithoutExtension(r) == ElympicsAssemblyName);

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            using var resolver = new ILPostProcessorAssemblyResolver(compiledAssembly);
            var assemblyDefinition = ReadAssembly(compiledAssembly, resolver);

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
                a.AttributeType.FullName == ProcessedByElympicsAttributeFullName);

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
                              a.AttributeType.FullName == ElympicsRpcAttributeFullName));

        private static DiagnosticMessage Error(string message, string? stackTrace, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0) => new()
        {
            DiagnosticType = DiagnosticType.Error,
            MessageData = string.IsNullOrEmpty(stackTrace) ? message : message + "|" + stackTrace?.Replace('\n', '|'),
            File = filePath,
            Line = lineNumber,
        };
    }
}
