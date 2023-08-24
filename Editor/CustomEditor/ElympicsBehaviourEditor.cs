using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
    [CustomEditor(typeof(ElympicsBehaviour))]
    internal partial class ElympicsBehaviourEditor : UnityEditor.Editor
    {
        [Flags]
        private enum ElympicsComponentsToSync
        {
            None = 0x0,
            GameObject_Active = 0x1,
            Transform = 0x2,
            Rigidbody = 0x4,
            Rigidbody2D = 0x8,
            Animator = 0x16,
        }

        private ElympicsBehaviour _behaviour;

        private SerializedProperty _networkId;
        private SerializedProperty _forceNetworkId;
        private bool _useAutoId = true;

        private SerializedProperty _predictableToPlayers;

        private SerializedProperty _isUpdatableForNonOwners;

        private SerializedProperty _visibleToPlayers;

        private SerializedProperty _stateUpdateFrequencyStages;

        private StringBuilder _stringBuilder;

        private GUIStyle summaryLabelStyle;

        private GUIStyle _warningStyle;

        private ElympicsComponentsToSync _selectedComponentsToSync;

        private static string MakeWarning(string text) => $"<color=yellow>{text}</color>";
        private bool IsPredictableForAnyone => ((ElympicsPlayer)_predictableToPlayers.GetValue() == ElympicsPlayer.Invalid || (ElympicsPlayer)_predictableToPlayers.GetValue() == ElympicsPlayer.World) && !IsPredictableForEveryone;
        private bool IsPredictableForEveryone => (ElympicsPlayer)_predictableToPlayers.GetValue() == ElympicsPlayer.All || (bool)_isUpdatableForNonOwners.GetValue();

        private void OnEnable()
        {
            _behaviour = serializedObject.targetObject as ElympicsBehaviour;
            _networkId = serializedObject.FindProperty(nameof(_behaviour.networkId));
            _forceNetworkId = serializedObject.FindProperty(nameof(_behaviour.forceNetworkId));
            _useAutoId = !_forceNetworkId.boolValue;

            _predictableToPlayers = serializedObject.FindProperty(nameof(_behaviour.predictableFor));
            _isUpdatableForNonOwners = serializedObject.FindProperty(nameof(_behaviour.isUpdatableForNonOwners));
            _visibleToPlayers = serializedObject.FindProperty(nameof(_behaviour.visibleFor));
            _stateUpdateFrequencyStages = serializedObject.FindProperty(nameof(_behaviour.stateFrequencyStages));

            _stringBuilder = new StringBuilder();
        }

        public override void OnInspectorGUI()
        {
            summaryLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
            _warningStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Normal, wordWrap = true, richText = true };
            EditorStyles.label.wordWrap = true;
            serializedObject.Update();
            if (IgnoreComponent(_behaviour))
            {
                EditorGUILayout.LabelField(Label_BehaviourNotModifiable);
                return;
            }

            DrawNetworkId();
            DrawUseAutoId();
            DrawPredictability();
            DrawVisibility();
            DrawStateChangeFrequencyStages();
            DrawObservedMonoBehaviours();

            _ = serializedObject.ApplyModifiedProperties();

            DrawSynchronizationButtons();
        }

        private void DrawNetworkId()
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Label_NetworkId);
            GUI.enabled = !_useAutoId;
            _ = EditorGUILayout.PropertyField(_networkId, GUIContent.none);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawUseAutoId()
        {
            _useAutoId = EditorGUILayout.Toggle(new GUIContent(Label_AutoId, Label_AutoIdTooltip), _useAutoId);
            _forceNetworkId.boolValue = !_useAutoId;
            EditorGUILayout.LabelField(Label_AutoIdSummary, summaryLabelStyle);
            EditorGUILayout.Space();
        }

        private void DrawPredictability()
        {
            _ = EditorGUILayout.PropertyField(_predictableToPlayers, new GUIContent(Label_PredictableFor, Label_PredictabilityTooltip), true);
            _ = EditorGUILayout.PropertyField(_isUpdatableForNonOwners, new GUIContent(Label_UpdatableForNonOwners, Label_UpdatableForNonOwnersTooltip), true);
            EditorGUILayout.LabelField(Label_PredictabilitySummary, summaryLabelStyle);
            EditorGUILayout.Space();
        }

        private void DrawVisibility()
        {
            _ = EditorGUILayout.PropertyField(_visibleToPlayers, new GUIContent(Label_VisibleFor, Label_VisibilityTooltip), true);
            EditorGUILayout.LabelField(Label_VisibilitySummary, summaryLabelStyle);
            EditorGUILayout.Space();
        }

        private void DrawStateChangeFrequencyStages()
        {
            _ = EditorGUILayout.PropertyField(_stateUpdateFrequencyStages, new GUIContent(Label_StateUpdateFrequency, Label_StateUpdateFrequencyTooltip), true);
            EditorGUILayout.LabelField(Label_StateUpdateFrequencySummary, summaryLabelStyle);
            EditorGUILayout.Space();
        }

        private void DrawObservedMonoBehaviours()
        {
            var allMonos = _behaviour.GetComponents<MonoBehaviour>();
            _ = EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(20, 0, 0, 0) });
            EditorGUILayout.LabelField(Label_ObservedMonoBehaviours);
            foreach (var mono in allMonos)
            {
                if (mono is IObservable)
                    EditorGUILayout.LabelField($"- {mono.GetType().Name} ({GetInterfaceNames(mono)})", new GUIStyle(GUI.skin.label) { padding = new RectOffset(20, 0, 0, 0), richText = true });
                if (mono is IUpdatable)
                    if (IsPredictableForAnyone)
                        EditorGUILayout.LabelField($"{MakeWarning("Warning!")} Elympics Update doesn't execute on clients for non-predictable objects.", new GUIStyle(GUI.skin.label) { padding = new RectOffset(40, 0, 0, 0), fontSize = 11, richText = true });
                    else
                    {
                        var markedPase = IsPredictableForEveryone ? "every client" : "client " + _predictableToPlayers.GetValue().ToString();
                        EditorGUILayout.LabelField($"Elympics Update for this object executes on {MakeWarning(markedPase)}.", new GUIStyle(GUI.skin.label) { padding = new RectOffset(40, 0, 0, 0), fontSize = 11, richText = true });
                    }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private string GetInterfaceNames(MonoBehaviour mono)
        {
            _ = _stringBuilder.Clear();
            var singleName = true;
            singleName = AddInterfaceNameIfPresent<IInputHandler>(mono, _stringBuilder, singleName);
            singleName = AddInterfaceNameIfPresent<IUpdatable>(mono, _stringBuilder, singleName, IsPredictableForAnyone);
            singleName = AddInterfaceNameIfPresent<IInitializable>(mono, _stringBuilder, singleName);
            singleName = AddInterfaceNameIfPresent<IStateSerializationHandler>(mono, _stringBuilder, singleName);
            if (singleName)
                _ = AddInterfaceNameIfPresent<IObservable>(mono, _stringBuilder, singleName);
            return _stringBuilder.ToString();
        }

        private static bool AddInterfaceNameIfPresent<TInterface>(MonoBehaviour mono, StringBuilder sb, bool singleName, bool warning = false)
        {
            if (mono is TInterface)
            {
                if (!singleName)
                    _ = sb.Append(", ");

                var name = typeof(TInterface).Name;

                _ = sb.Append(warning ? MakeWarning(name) : name);

                singleName = false;
            }

            return singleName;
        }

        private void DrawSynchronizationButtons()
        {
            AddSynchronizationButton<ElympicsGameObjectActiveSynchronizer>(ElympicsComponentsToSync.GameObject_Active);
            AddSynchronizationButton<ElympicsTransformSynchronizer>(ElympicsComponentsToSync.Transform);

            var hasRigidbody = _behaviour.TryGetComponent<Rigidbody>(out _);
            var hasRigidbody2D = _behaviour.TryGetComponent<Rigidbody2D>(out _);

            if (hasRigidbody)
                AddSynchronizationButton<ElympicsRigidBodySynchronizer>(ElympicsComponentsToSync.Rigidbody);
            if (hasRigidbody2D)
                AddSynchronizationButton<ElympicsRigidBody2DSynchronizer>(ElympicsComponentsToSync.Rigidbody2D);
            if (_behaviour.TryGetComponent<Animator>(out _))
                AddSynchronizationButton<ElympicsAnimatorSynchronizer>(ElympicsComponentsToSync.Animator);

            if (hasRigidbody || hasRigidbody2D)
                AddSynchronizationWarnings();
        }

        private void AddSynchronizationWarnings()
        {
            if (HasComponent(ElympicsComponentsToSync.Transform) && (HasComponent(ElympicsComponentsToSync.Rigidbody) || HasComponent(ElympicsComponentsToSync.Rigidbody2D)))
            {
                AddSynchronizationWarningForComponent(Label_TransformAndRigidBodyExistWarning, ElympicsDocumentationUrls.Link_RigidBodySynchronizerDocumentation);
            }
            else if (HasComponent(ElympicsComponentsToSync.Transform))
            {
                AddSynchronizationWarningForComponent(Label_TransformExistRigidbodyWarning, ElympicsDocumentationUrls.Link_RigidBodySynchronizerDocumentation);
            }
            else if (HasComponent(ElympicsComponentsToSync.Rigidbody) || HasComponent(ElympicsComponentsToSync.Rigidbody2D))
            {
                AddSynchronizationWarningForComponent(Label_RigidBodyExistExistTransformWarning, ElympicsDocumentationUrls.Link_TransfromSynchronizerDocumentation);
            }

        }

        private void AddSynchronizationWarningForComponent(string warning, string documentationUrl)
        {
            EditorGUILayout.Space(10);
            _ = EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(warning, _warningStyle);
            if (GUILayout.Button(ElympicsDocumentationUrls.Label_Documentation, GUILayout.ExpandWidth(false)))
            {
                Application.OpenURL(documentationUrl);
            }
            EditorGUILayout.EndVertical();
        }

        private bool IgnoreThisBehaviour(ElympicsBehaviour behaviour)
            => behaviour.TryGetComponent<ElympicsFactory>(out _)
               || behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _);

        private void AddSynchronizationButton<TComponent>(ElympicsComponentsToSync buttonType) where TComponent : MonoBehaviour
        {
            var height = GUILayout.Height(40);
            ColorizeButton(buttonType);
            if (!_behaviour.TryGetComponent<TComponent>(out _))
            {
                UnregisterComponentForSync(buttonType);
                if (GUILayout.Button($"Add {buttonType} Synchronization", height))
                {
                    _ = _behaviour.gameObject.AddComponent<TComponent>();
                    RegisterComponentForSync(buttonType);
                    EditorUtility.SetDirty(_behaviour.gameObject);
                }
            }
            else
            {
                RegisterComponentForSync(buttonType);
                if (GUILayout.Button($"Remove {buttonType} Synchronization", height))
                {
                    UnregisterComponentForSync(buttonType);
                    DestroyImmediate(_behaviour.gameObject.GetComponent<TComponent>(), true);
                    EditorUtility.SetDirty(_behaviour.gameObject);
                }
            }
            GUI.color = Color.white;
        }

        private void ColorizeButton(ElympicsComponentsToSync buttonType)
        {
            var changeColor = ((buttonType == ElympicsComponentsToSync.Rigidbody2D || buttonType == ElympicsComponentsToSync.Rigidbody) && HasComponent(ElympicsComponentsToSync.Transform)) ||
                (buttonType == ElympicsComponentsToSync.Transform && (HasComponent(ElympicsComponentsToSync.Rigidbody) || HasComponent(ElympicsComponentsToSync.Rigidbody2D)));

            if (changeColor)
                GUI.color = Color.yellow;
        }

        private void RegisterComponentForSync(ElympicsComponentsToSync component)
        {
            if ((_selectedComponentsToSync & component) == 0)
            {
                _selectedComponentsToSync |= component;
            }
        }

        private void UnregisterComponentForSync(ElympicsComponentsToSync component)
        {
            if ((_selectedComponentsToSync & component) != 0)
            {
                _selectedComponentsToSync &= ~component;
            }
        }

        private bool HasComponent(ElympicsComponentsToSync component)
        {
            return (_selectedComponentsToSync & component) == component;
        }
    }
}
