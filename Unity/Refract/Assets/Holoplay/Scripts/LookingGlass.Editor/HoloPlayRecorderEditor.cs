//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    [CustomEditor(typeof(HoloplayRecorder))]
	public class HoloplayRecorderEditor : UnityEditor.Editor {
		SerializedProperty presetProp;
        	HoloplayDevice.Type quiltSettingsPreset = HoloplayDevice.Type.Portrait;

        void OnEnable()
        {
            presetProp = serializedObject.FindProperty("_preset");
        }

        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update ();

            HoloplayRecorder hr = (HoloplayRecorder)target;

            EditorGUILayout.PropertyField(presetProp);
 
            string val = presetProp.enumNames[presetProp.enumValueIndex];
            bool isPresetChanged = !val.Equals(hr.preset.ToString());

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_outputName"));
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Full name" ,hr.AutoCorrectPath);
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_frameRate"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_targetBitrateInMegabits"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_compression"));

            hr.isOverwrite = EditorGUILayout.Toggle("Overwrite Quilt Settings", hr.isOverwrite);

            if (hr.isOverwrite)
            {
                EditorGUI.indentLevel++;

                HoloplayDevice.Type newPreset = (HoloplayDevice.Type)EditorGUILayout.EnumPopup("Quilt Setting Preset", quiltSettingsPreset);
                if(quiltSettingsPreset != newPreset)
                {
                    quiltSettingsPreset = newPreset;
                    hr.overwriteQuiltSettings = Quilt.GetPreset(quiltSettingsPreset);
                    hr.overwriteNearClipFactor = HoloplayDevice.GetSettings(quiltSettingsPreset).nearFlip;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("overwriteQuiltSettings"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("overwriteNearClipFactor"));

                EditorGUI.indentLevel--;
            }

            // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties ();

            // if (isPresetChanged && hr.preset != FFmpegOut.FFmpegPreset.VP8Default)
            // {
            //     Debug.LogWarning("Warning: we recommend using VP8. Using other codecs may cause compatibility or compression issues.");
            // }


            // otherwise it won't save
			if(GUI.changed)
			{
				EditorUtility.SetDirty(hr);
			}

        }
		
	}
}
