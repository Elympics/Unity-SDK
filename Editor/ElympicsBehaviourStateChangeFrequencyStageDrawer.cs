using System;
using UnityEditor;
using UnityEngine;

namespace Elympics
{
	[CustomPropertyDrawer(typeof(ElympicsBehaviourStateChangeFrequencyStage))]
	public class ElympicsBehaviourStateChangeFrequencyStageDrawer : PropertyDrawer
	{
		private const int MilisecondsLabelWidth = 20;
		private const int MilisecondsConvertedToTicksLabelWidth = 70;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			//By default Unity adds empty space at the tops that grows with number of elements in array
			return -2.0f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUILayout.BeginVertical(GUI.skin.GetStyle("HelpBox"));

			EditorGUILayout.LabelField("State Change Frequency Stage");

			var maxStageDurationInMiliseconds = property.FindPropertyRelative("maxStageDurationInMiliseconds");
			var stageDurationInMiliseconds = property.FindPropertyRelative("stageDurationInMiliseconds");
			stageDurationInMiliseconds.intValue = IntSliderWithUnit(new GUIContent("Stage Duration:"), stageDurationInMiliseconds.intValue, 1, maxStageDurationInMiliseconds.intValue, "ms");

			var frequencyInMiliseconds = property.FindPropertyRelative("frequencyInMiliseconds");
			frequencyInMiliseconds.intValue = IntSliderWithUnit(new GUIContent("Frequency:"), frequencyInMiliseconds.intValue, 1, stageDurationInMiliseconds.intValue, "ms");

			EditorGUI.EndProperty();
			EditorGUILayout.EndVertical();

			property.serializedObject.ApplyModifiedProperties();
		}

		private int IntSliderWithUnit(GUIContent content, int value, int left, int right, string unit)
		{
			EditorGUILayout.BeginHorizontal();
			var newValue = EditorGUILayout.IntSlider(content, value, left, right, GUILayout.MaxWidth(float.MaxValue));
			EditorGUILayout.LabelField(unit, GUILayout.Width(MilisecondsLabelWidth));
			EditorGUILayout.LabelField("(" + MsToTicks(value) + " ticks)", GUILayout.Width(MilisecondsConvertedToTicksLabelWidth));
			EditorGUILayout.EndHorizontal();
			return newValue;
		}

		private int MsToTicks(int milliseconds)
		{
			return (int)Math.Round(ElympicsConfig.LoadCurrentElympicsGameConfig().TicksPerSecond * milliseconds / 1000.0);
		}
	}
}
