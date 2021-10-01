//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;
using UnityEditor;
using System;

namespace LookingGlass {
    [CustomEditor(typeof(Holoplay))]
	[CanEditMultipleObjects]
	public class HoloplayEditor : Editor {

		struct Section {
			string startProp;
			string endProp;
			string title;
			public bool foldout;
			public bool current;
			public Section(string startProp, string endProp, string title) {
				this.startProp = startProp;
				this.endProp = endProp;
				this.title = title;
				foldout = false;
				current = false;
			}
			// will return true if folded out
			public bool DoSection(SerializedProperty prop) {
				if (prop.name == startProp) {
					foldout = EditorGUILayout.Foldout(foldout, title, true);
					current = true;
					EditorGUI.indentLevel++;
				}
				if (prop.name == endProp) {
					current = false;
					EditorGUI.indentLevel--;
				}
				return !current || foldout;
			}
			// force an end if it's the last section
			public void ForceEnd() {
				current = false;
				EditorGUI.indentLevel--;
			}
		}

		static Section advanced = new Section("fov", "quiltPreset", "Advanced Camera Settings");
		static Section quilt = new Section("quiltPreset", "frustumColor", "Quilt Settings");
		static Section gizmo = new Section("frustumColor", "onHoloplayReady", "Gizmo");
		static Section events = new Section("onHoloplayReady", "viewInterpolation", "Events");
		static Section optimization = new Section("viewInterpolation", "", "Optimization");

		void OnEnable () {

			Holoplay hp = (Holoplay)target;
			hp.targetDisplay = (int)hp.displayTarget;			
		}

		public override void OnInspectorGUI() {
			
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
 	     	serializedObject.Update();
			
			// psuedo custom inspector
			SerializedProperty prop = serializedObject.GetIterator();
			Holoplay hp = (Holoplay)target;
			
			// account for the first prop being the script
			bool firstProp = true;
			GUI.enabled = false;
			if (prop.NextVisible(true)) {
				do {
					// sections
					if (!advanced.DoSection(prop)) continue;
					if (!quilt.DoSection(prop)) continue;
					if (!gizmo.DoSection(prop)) continue;
					if (!events.DoSection(prop)) continue;
					if (!optimization.DoSection(prop)) continue;
				
					// skip custom quilt settings if preset not set to custom
					if (prop.name == "customQuiltSettings" && hp.GetQuiltPreset() != Quilt.Preset.Custom) {
						var qs = Quilt.GetPreset(hp.GetQuiltPreset(), hp.cal); 
						
						EditorGUILayout.LabelField("Quilt Size: ", qs.quiltWidth + " x " + qs.quiltHeight);
						EditorGUILayout.LabelField("View Size: ", qs.viewWidth + " x " + qs.viewHeight);
						EditorGUILayout.LabelField("Tiling: ", qs.viewColumns + " x " + qs.viewRows);
						EditorGUILayout.LabelField("Views Total: ", ""+qs.numViews);

						continue;
					}

					// don't let quiltRT be editable
					if (prop.name == "quiltRT") {
						GUI.enabled = false;
						EditorGUILayout.PropertyField(prop, true);
						GUI.enabled = true;
						continue;
					}

					// target display
					if (prop.name == "targetDisplay") {
						
						EditorGUI.BeginChangeCheck();
						hp.displayTarget = (Holoplay.DisplayTarget)EditorGUILayout.EnumPopup("Target Display", hp.displayTarget);
						if (EditorGUI.EndChangeCheck()) {

							Undo.RecordObject(hp, "Change Target Display");

							// Debug.Log("changed:"+hp.targetDisplay + hp.displayTarget);
							hp.targetDisplay = (int)hp.displayTarget;

							Preview.HandlePreview(false);
						}  
						continue;
					}

					if (prop.name == "emulatedDevice") {
						// hide when calibration is found
						if(hp.loadResults.calibrationFound)
							continue;
						
						EditorGUI.BeginChangeCheck();
						HoloplayDevice.Type emulatedDevice = hp.emulatedDevice;
						emulatedDevice = (HoloplayDevice.Type)EditorGUILayout.EnumPopup("Emulated Device", emulatedDevice);
						if (EditorGUI.EndChangeCheck()) {
							Undo.RecordObject(hp, "Change Emulated Device");
							hp.emulatedDevice = emulatedDevice;
							// force near clip to change
							hp.nearClipFactor = HoloplayDevice.GetSettings(emulatedDevice).nearFlip;
							// force the quilt preset changed to the default preset
							hp.SetQuiltPreset(Quilt.Preset.Automatic);

							Preview.HandlePreview(false);
						}  
						continue;
					}
					
					if (prop.name == "quiltPreset") {						
						// Quilt.Preset quiltPreset = hp.GetQuiltPreset();
						
						EditorGUI.BeginChangeCheck();
						hp.quiltPreset = (Quilt.Preset)EditorGUILayout.EnumPopup("Quilt Preset", hp.quiltPreset);
						if (EditorGUI.EndChangeCheck()) {
							// TODO: Not sure how it should be handled here bc we changed quilt preset directly 
							// Undo.RecordObject(hp, "Change Quilt Preset");

							hp.SetupQuilt();
							
							Preview.HandlePreview(false);
						}  
						continue;
					}

					// if all's normal, just draw the property like normal
					EditorGUILayout.PropertyField(prop, true);					

					// after script name, re-enable GUI
					if (firstProp) {
						// version
						EditorGUILayout.LabelField("Version", Holoplay.version.ToString() + Holoplay.versionLabel, EditorStyles.miniLabel);
						// re-enable gui and continue
						GUI.enabled = true;
						firstProp = false;
					}
				}
				while (prop.NextVisible(false));
			}
			// because it's the last section and doesn't get closed out automatically, force this section to end
			optimization.ForceEnd();

			serializedObject.ApplyModifiedProperties();

			// otherwise it won't save
			if(GUI.changed)
			{
				EditorUtility.SetDirty(hp);
			}

			// toggle preview button
			if(GUILayout.Button(Preview.togglePreviewShortcut)){
				Preview.HandlePreview();
			}
			// reload calibration button
			if (GUILayout.Button("Reload Calibration")){
              	bool isToggling = hp.loadResults.calibrationFound != hp.ReloadCalibration().calibrationFound;
				Preview.HandlePreview(isToggling);
				
			}
        }


		protected virtual void OnSceneGUI() {
			var hp = (Holoplay)target;
			if (!hp.enabled) return;
			if (!hp.drawHandles) return;
			// for some reason, doesn't need the gamma conversion like gizmos do
			Handles.color = hp.handleColor;
            // handle matrix
            Matrix4x4 originalMatrix = Handles.matrix;
            Matrix4x4 hpMatrix = Matrix4x4.TRS(
				hp.transform.position, 
				hp.transform.rotation, 
				new Vector3(hp.cam.aspect, 1f, 1f));
            Handles.matrix = hpMatrix;
			// set the new size
			Vector3[] dirs = new Vector3[] {
				new Vector3(-hp.size, 0f),
				new Vector3( hp.size, 0f),
				new Vector3(0f, -hp.size),
				new Vector3(0f,  hp.size),
			};
			float newSize = hp.size;
			foreach (var d in dirs) {
				EditorGUI.BeginChangeCheck();
				var newDir = Handles.Slider(d, d, HandleUtility.GetHandleSize(d) * 0.03f, Handles.DotHandleCap, 0f);
				newSize = Vector3.Dot(newDir, d.normalized);
				if (EditorGUI.EndChangeCheck()) {
					Undo.RecordObject(hp, "Holoplay Size");
					hp.size = Mathf.Clamp(newSize, 0.01f, Mathf.Infinity);
					hp.ResetCamera();
				}
			}
            Handles.matrix = originalMatrix;
        }

        [MenuItem("GameObject/Holoplay Capture", false, 10)]
        public static void CreateHoloPlay() {
            var asset = (GameObject)AssetDatabase.LoadAssetAtPath(
				"Assets/Holoplay/Prefabs/Holoplay Capture.prefab", typeof(GameObject));
            if (asset == null) {
                Debug.LogWarning("[Holoplay] Couldn't find the holoplay capture folder or prefab.");
                return;
            }
            var hp = Instantiate(asset, Vector3.zero, Quaternion.identity);
            hp.name = asset.name;
			Undo.RegisterCreatedObjectUndo(hp, "Create Holoplay Capture");
        }

		[MenuItem("Holoplay/Setup Player Settings", false, 1)]	
		public static void OpenPlayerSettingsEditor() {

		}
	}
}