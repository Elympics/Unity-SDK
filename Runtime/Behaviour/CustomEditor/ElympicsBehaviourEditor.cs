#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	[CustomEditor(typeof(ElympicsBehaviour))]
	internal partial class ElympicsBehaviourEditor : Editor
	{
		private ElympicsBehaviour _behaviour;

		private SerializedProperty _networkId;
		private SerializedProperty _forceNetworkId;
		private bool               _useAutoId = true;

		private SerializedProperty _predictableToPlayers;

		private SerializedProperty _isUpdatableForNonOwners;

		private SerializedProperty _visibleToPlayers;

		private SerializedProperty _stateUpdateFrequencyStages;

		private StringBuilder _stringBuilder;

		private GUIStyle summaryLabelStyle;

		void OnEnable()
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

			serializedObject.ApplyModifiedProperties();

			DrawSynchronizationButtons();
		}

		private void DrawNetworkId()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Label_NetworkId);
			GUI.enabled = !_useAutoId;
			EditorGUILayout.PropertyField(_networkId, GUIContent.none);
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
			EditorGUILayout.PropertyField(_predictableToPlayers, new GUIContent(Label_PredictableFor, Label_PredictabilityTooltip), true);
			EditorGUILayout.PropertyField(_isUpdatableForNonOwners, new GUIContent(Label_UpdatableForNonOwners, Label_UpdatableForNonOwnersTooltip), true);
			EditorGUILayout.LabelField(Label_PredictabilitySummary, summaryLabelStyle);
			EditorGUILayout.Space();
		}

		private void DrawVisibility()
		{
			EditorGUILayout.PropertyField(_visibleToPlayers, new GUIContent(Label_VisibleFor, Label_VisibilityTooltip), true);
			EditorGUILayout.LabelField(Label_VisibilitySummary, summaryLabelStyle);
			EditorGUILayout.Space();
		}

		private void DrawStateChangeFrequencyStages()
		{
			EditorGUILayout.PropertyField(_stateUpdateFrequencyStages, new GUIContent(Label_StateUpdateFrequency, Label_StateUpdateFrequencyTooltip), true);
			EditorGUILayout.LabelField(Label_StateUpdateFrequencySummary, summaryLabelStyle);
			EditorGUILayout.Space();
		}

		private void DrawObservedMonoBehaviours()
		{
			var allMonos = _behaviour.GetComponents<MonoBehaviour>();
			EditorGUILayout.BeginVertical(new GUIStyle {padding = new RectOffset(20, 0, 0, 0)});
			EditorGUILayout.LabelField(Label_ObservedMonoBehaviours);
			foreach (var mono in allMonos)
			{
				if (mono is IObservable)
					EditorGUILayout.LabelField($"- {mono.GetType().Name} ({GetInterfaceNames(mono)})", new GUIStyle(GUI.skin.label) {padding = new RectOffset(20, 0, 0, 0)});
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}


		private string GetInterfaceNames(MonoBehaviour mono)
		{
			_stringBuilder.Clear();
			bool singleName = true;
			singleName = AddInterfaceNameIfPresent<IInputHandler>(mono, _stringBuilder, singleName);
			singleName = AddInterfaceNameIfPresent<IUpdatable>(mono, _stringBuilder, singleName);
			singleName = AddInterfaceNameIfPresent<IInitializable>(mono, _stringBuilder, singleName);
			singleName = AddInterfaceNameIfPresent<IStateSerializationHandler>(mono, _stringBuilder, singleName);
			if (singleName)
				AddInterfaceNameIfPresent<IObservable>(mono, _stringBuilder, singleName);
			return _stringBuilder.ToString();
		}

		private static bool AddInterfaceNameIfPresent<TInterface>(MonoBehaviour mono, StringBuilder sb, bool singleName)
		{
			if (mono is TInterface)
			{
				if (!singleName)
					sb.Append(", ");
				sb.Append(typeof(TInterface).Name);
				singleName = false;
			}

			return singleName;
		}

		private void DrawSynchronizationButtons()
		{
			AddSynchronizationButton<ElympicsGameObjectActiveSynchronizer>(Label_GameObjectSynchronizer);
			AddSynchronizationButton<ElympicsTransformSynchronizer>(Label_TransformSynchronizer);
			if (_behaviour.TryGetComponent<Rigidbody>(out _))
				AddSynchronizationButton<ElympicsRigidBodySynchronizer>(Label_RigidbodySynchronizer);
			if (_behaviour.TryGetComponent<Rigidbody2D>(out _))
				AddSynchronizationButton<ElympicsRigidBody2DSynchronizer>(Label_Rigidbody2DSynchronizer);
			if(_behaviour.TryGetComponent<Animator>(out _))
				AddSynchronizationButton<ElympicsAnimatorSynchronizer>(Label_AnimatorSynchronizer);
		}

		private bool IgnoreThisBehaviour(ElympicsBehaviour behaviour)
			=> behaviour.TryGetComponent<ElympicsFactory>(out _)
			   || behaviour.TryGetComponent<ElympicsUnityPhysicsSimulator>(out _);

		private void AddSynchronizationButton<TComponent>(string name) where TComponent : MonoBehaviour
		{
			GUILayoutOption height = GUILayout.Height(40);
			if (!_behaviour.TryGetComponent<TComponent>(out _))
			{
				if (GUILayout.Button($"Add {name} Synchronization", height))
				{
					_behaviour.gameObject.AddComponent<TComponent>();
					EditorUtility.SetDirty(_behaviour.gameObject);
				}
			}
			else
			{
				if (GUILayout.Button($"Remove {name} Synchronization", height))
				{
					DestroyImmediate(_behaviour.gameObject.GetComponent<TComponent>(), true);
					EditorUtility.SetDirty(_behaviour.gameObject);
				}
			}
		}
	}
}
#endif
