using System;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass;

#if UNITY_EDITOR
using UnityEditor;
#endif

//TODO: Possibly use a custom namespace for this custom code?
namespace UnityEngine.Rendering.PostProcessing {
    /// <summary>
    /// Contains extensions to <see cref="Holoplay"/> to support post-processing, by implementing callbacks during onto OnEnable and OnDisable.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class HoloplayPostProcessSetup {
#if UNITY_EDITOR
        static HoloplayPostProcessSetup() {
            RegisterCallbacks();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCallbacks() {
#if UNITY_POST_PROCESSING_STACK_V2
            Holoplay.postProcessSetup = OnPostProcessSetup;
            Holoplay.postProcessDispose = OnPostProcessDispose;
#endif
        }

        private static HoloplayPostProcessSetupData OnPostProcessSetup(Holoplay behaviour) {
            HoloplayPostProcessSetupData output = new HoloplayPostProcessSetupData();

            PostProcessLayer postLayer = behaviour.GetComponent<PostProcessLayer>();
            if (postLayer != null && postLayer.enabled) {
                output.postProcessCamera = behaviour.GetComponent<Camera>();
                output.postProcessCamera.hideFlags = HideFlags.HideInInspector;

                GameObject rawCamera = new GameObject(Holoplay.SingleViewCameraName);
                rawCamera.hideFlags = HideFlags.HideAndDontSave;
                rawCamera.transform.SetParent(behaviour.transform);
                rawCamera.transform.localPosition = Vector3.zero;
                rawCamera.transform.localRotation = Quaternion.identity;
                output.camera = rawCamera.AddComponent<Camera>();
                output.camera.CopyFrom(output.postProcessCamera);
            }

            return output;
        }

        private static void OnPostProcessDispose(Holoplay behaviour) {
            if (behaviour.PostProcessCamera != null) {
                if (behaviour.SingleViewCamera.gameObject == behaviour.gameObject) {
                    Debug.LogWarning("Something is very wrong");
                } else {
                    Object.DestroyImmediate(behaviour.SingleViewCamera.gameObject);
                }
            }
        }
    }
}
