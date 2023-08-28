using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Elympics.Editor;
using Elympics.Weaving;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Elympics.Weaver
{
    public class WeaverSettings : ScriptableObject, ILogable
    {
        public const string VERSION = "3.3.0";

        private const string WeavedOnEnableKey = "ELYMPICS_WEAVER_WeavedOnEnable";

        [SerializeField]
        [Tooltip("This is evaluated before Weaver runs to check if it should execute. The symbol expression must come out to be true")]
        private ScriptingSymbols m_RequiredScriptingSymbols;

        [SerializeField]
        private List<WeavedAssembly> m_WeavedAssemblies;

        [UsedImplicitly]
        private ComponentController m_Components = new(new ElympicsRpcComponent());

        [SerializeField]
        [UsedImplicitly]
        private bool m_IsEnabled = true; // m_Enabled is used by Unity and throws errors (even if scriptable objects don't have that field)

        [UsedImplicitly]
        private Log m_Log;

        private Stopwatch m_Timer;

        public ComponentController componentController => m_Components;

        string ILogable.label => nameof(WeaverSettings);

        [UsedImplicitly]
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _ = Instance();
        }

        /// <summary>
        /// Gets the instance of our Settings if it exists. Returns null
        /// if no instance was created.
        /// </summary>
        public static WeaverSettings Instance()
        {
            WeaverSettings settings = null;
            // Find all settings
            var guids = AssetDatabase.FindAssets("t:Elympics.Weaver.WeaverSettings");
            // Load them all
            for (var i = 0; i < guids.Length; i++)
            {
                // Convert our path
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                // Load it
                settings = AssetDatabase.LoadAssetAtPath<WeaverSettings>(assetPath);
            }
            return settings;
        }

        [PostProcessScene]
        public static void PostprocessScene()
        {
            // Only run this code if we are building the player
            if (BuildPipeline.isBuildingPlayer)
            {
                // Get our current scene
                var scene = SceneManager.GetActiveScene();
                // If we are the first scene (we only want to run once)
                if (scene.IsValid() && scene.buildIndex == 0)
                {
                    // Find all settings
                    var guids = AssetDatabase.FindAssets("t:Elympics.Weaver.WeaverSettings");
                    // Load them all
                    if (guids.Length > 0)
                    {
                        // Convert our path
                        var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                        // Load it
                        var settings = AssetDatabase.LoadAssetAtPath<WeaverSettings>(assetPath);
                        // Invoke
                        settings.WeaveModifiedAssemblies();
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when our module is first created and turned on
        /// </summary>
        [UsedImplicitly]
        private void OnEnable()
        {
            m_Log ??= new Log(this);
            m_WeavedAssemblies ??= new List<WeavedAssembly>();

            m_RequiredScriptingSymbols.ValidateSymbols();

            // Enable all our components
            foreach (var assembly in m_WeavedAssemblies)
                assembly.OnEnable();
            m_Timer = new Stopwatch();
            m_Log.context = this;

            AssemblyUtility.PopulateAssemblyCache();
            if (!SessionState.GetBool(WeavedOnEnableKey, false))
            {
                WeaveModifiedAssemblies();
                SessionState.SetBool(WeavedOnEnableKey, true);
            }

            // Subscribe to the before reload event so we can modify the assemblies!
            m_Log.Info("Weaver Settings", "Subscribing to next assembly reload.", false);
            CompilationPipeline.assemblyCompilationFinished += ComplicationComplete;
        }

        /// <summary>
        /// Invoked whenever one of our assemblies has compelted compliling.
        /// </summary>
        private void ComplicationComplete(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            WeaveAssembly(assemblyPath);
        }

        /// <summary>
        /// Loops over all changed assemblies and starts the weaving process for each.
        /// </summary>
        private void WeaveModifiedAssemblies()
        {
            foreach (var assembly in m_WeavedAssemblies)
                WeaveAssembly(assembly.relativePath);
        }


        /// <summary>
        /// Returns back an instance of our symbol reader for
        /// </summary>
        /// <returns></returns>
        internal static ReaderParameters GetReaderParameters(string assemblyPath)
        {
            return new ReaderParameters()
            {
                ReadingMode = ReadingMode.Immediate,
                ReadWrite = true,
                AssemblyResolver = new WeaverAssemblyResolver(assemblyPath),
                ReadSymbols = true,
                SymbolReaderProvider = new PdbReaderProvider()
            };
        }

        /// <summary>
        /// Returns back the instance of the symbol writer provide.
        /// </summary>
        private static WriterParameters GetWriterParameters()
        {
            return new WriterParameters()
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PdbWriterProvider()
            };
        }

        private bool HasBeenAlreadyWeaved(string assemblyPath)
        {
            using var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
            using var moduleDefinition = ModuleDefinition.ReadModule(assemblyStream,
                GetReaderParameters(assemblyPath));
            var soughtAttributeType = moduleDefinition.ImportReference(typeof(ProcessedByElympicsAttribute));
            return moduleDefinition.Assembly.CustomAttributes
                .Any(attribute => attribute.AttributeType.FullName == soughtAttributeType.FullName);
        }

        /// <summary>
        /// Invoked for each assemby that has been compiled.
        /// </summary>
        private void WeaveAssembly(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                return;
            var weavedAssembly = m_WeavedAssemblies
                .FirstOrDefault(a => Path.GetFileName(a.GetSystemPath()) == Path.GetFileName(assemblyPath));
            if (!(weavedAssembly?.IsActive ?? false))
                return;
            if (!File.Exists(assemblyPath))
            {
                if (weavedAssembly.ShouldThrowIfNotFound)
                    throw new FileNotFoundException($"Weaved assembly file missing: {assemblyPath}");
                return;
            }
            if (HasBeenAlreadyWeaved(assemblyPath))
                return;

            var name = Path.GetFileNameWithoutExtension(assemblyPath);

            m_Log.Info(name, "Starting", false);
            if (!m_IsEnabled)
            {
                m_Log.Info(name, "Aborted due to weaving being disabled.", false);
                return;
            }
            if (!m_RequiredScriptingSymbols.isActive)
            {
                m_Log.Info(name, "Aborted due to non-matching script symbols.", false);
                return;
            }

            var filePath = Path.Combine(Constants.ProjectRoot, assemblyPath);

            if (!File.Exists(filePath))
            {
                m_Log.Error(name, "Unable to find assembly at path '" + filePath + "'.", true);
                return;
            }

            using (var assemblyStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
            {
                using var moduleDefinition = ModuleDefinition.ReadModule(assemblyStream, GetReaderParameters(assemblyPath));
                m_Components.VisitModule(moduleDefinition, m_Log);

                // Save
                var writerParameters = new WriterParameters()
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new NativePdbWriterProvider()
                };

                moduleDefinition.Write(GetWriterParameters());
            }

            m_Log.Info("Weaver Settings", "Weaving Successfully Completed", false);

            // Stats
            m_Log.Info(name, "Time ms: " + m_Timer.ElapsedMilliseconds, false);
            m_Log.Info(name, "Types: " + m_Components.totalTypesVisited, false);
            m_Log.Info(name, "Methods: " + m_Components.totalMethodsVisited, false);
            m_Log.Info(name, "Fields: " + m_Components.totalFieldsVisited, false);
            m_Log.Info(name, "Properties: " + m_Components.totalPropertiesVisited, false);
            m_Log.Info(name, "Complete", false);
        }

        [UsedImplicitly]
        private void OnValidate()
        {
            m_RequiredScriptingSymbols.ValidateSymbols();
        }
    }
}

