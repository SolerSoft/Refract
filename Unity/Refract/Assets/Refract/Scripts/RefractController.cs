using LookingGlass;
using LookingGlass.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViewInterpolationType = LookingGlass.Holoplay.ViewInterpolationType;

namespace Refract
{
    /// <summary>
    /// Main controller for the Refract application.
    /// </summary>
    public class RefractController : MonoBehaviour
    {
        #region Constants
        private const float DEPTHINESS_DEFAULT = 10.0f;
        private const float DEPTHINESS_MAX = 20.0f;
        private const float DEPTHINESS_MIN = 0.0f;
        private const float FOCUS_DEFAULT = 5f;
        private const float FOCUS_MAX = 10;
        private const float FOCUS_MIN = -10;
        private const float TESSELLATION_DEFAULT = 15.0f;
        private const float TESSELLATION_MAX = 30f;
        private const float TESSELLATION_MIN = 0.0f;
        private const ViewInterpolationType INTERPOLATION_DEFAULT = ViewInterpolationType.None;
        private const float INTERPOLATION_MAX = 5.0f;
        private const float INTERPOLATION_MIN = 0.0f;
        #endregion // Constants

        #region Member Variables
        private float lastDepthiness = float.NaN;
        private float lastFocus = float.NaN;
        private float lastInterpolation = float.NaN;
        private float lastTessellation = float.NaN;
        private Material projectorMaterial;
        private float renderDepthiness = DEPTHINESS_DEFAULT;
        private float renderFocus = FOCUS_DEFAULT;
        private ViewInterpolationType renderInterpolation = INTERPOLATION_DEFAULT;
        private float renderTessellation = TESSELLATION_DEFAULT;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Controllers")]
        [SerializeField]
        [Tooltip("The Holoplay object that controls Looking Glass rendering.")]
        private Holoplay holoplay;

        [SerializeField]
        [Tooltip("The displaced and colored virtual scene projector.")]
        private MeshRenderer projector;

        [SerializeField]
        [Tooltip("The camera dedicated to the menu when in shared rendering mode.")]
        private Camera menuCamera;

        [SerializeField]
        [Tooltip("The MenuController that controls the main menu.")]
        private MenuController menuController;

        [SerializeField]
        [Tooltip("The HoloPlay object that controls the display.")]
        private Holoplay holoPlay;


        [Header("Scene Settings")]
        [SerializeField]
        [Tooltip("How much displacement is caused by the depth map.")]
        [Range(0, 1)]
        private float depthiness = 0.5f;

        [SerializeField]
        [Tooltip("Which part of the projector is currently focused. 0.5 is middle.")]
        [Range(0, 1)]
        private float focus = 0.5f;

        [SerializeField]
        [Tooltip("The amount of tessellation (detail) used by the shader.")]
        [Range(0, 1)]
        private float tessellation = 0.5f;

        [SerializeField]
        [Tooltip("The amount of interpolation used by Holoplay. This percentage gets converted to an enum internally.")]
        [Range(0, 1)]
        private float interpolation = 0;


        [Header("UX")]
        [SerializeField]
        [Tooltip("Whether to show the running scene while the menu is open.")]
        private bool showSceneInMenu;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Converts a percentage to a range value.
        /// </summary>
        /// <param name="min">
        /// The minimum value in a range.
        /// </param>
        /// <param name="max">
        /// The maximum value in a range.
        /// </param>
        /// <param name="percent">
        /// The percentage to convert.
        /// </param>
        /// <returns>
        /// The ranged value.
        /// </returns>
        static private float PercentToRange(float min, float max, float percent)
        {
            float r = (max - min);
            return (r * percent) + min;
        }

        /// <summary>
        /// Converts a ranged value to a percentage
        /// </summary>
        /// <param name="min">
        /// The minimum value in a range.
        /// </param>
        /// <param name="max">
        /// The maximum value in a range.
        /// </param>
        /// <param name="range">
        /// The ranged value to convert.
        /// </param>
        /// <returns>
        /// The percentage.
        /// </returns>
        static private float RangeToPercent(float min, float max, float range)
        {
            float r = (max - min);
            float mr = range + min;
            return mr / r;
        }

        /// <summary>
        /// Rounds and clamps the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to round and clamp.
        /// </param>
        /// <param name="min">
        /// The minimum value allowed.
        /// </param>
        /// <param name="max">
        /// The maximum value allowed.
        /// </param>
        /// <returns>
        /// The rounded and clamped value.
        /// </returns>
        static private float RoundClamp(float value, float min, float max)
        {
            value = Mathf.Clamp(value, min, max);
            value = (float)Math.Round(value, 2);
            return value;
        }

        /// <summary>
        /// Applies the current depthiness.
        /// </summary>
        private void ApplyDepthiness()
        {
            // Save as last updated
            lastDepthiness = depthiness;

            // Convert to render value
            renderDepthiness = PercentToRange(DEPTHINESS_MIN, DEPTHINESS_MAX, depthiness);

            // Update the displacement shader displacement factor
            projectorMaterial.SetFloat("_DispFactor", renderDepthiness);

            // Move projector
            ApplyProjectorPosition();
        }

        /// <summary>
        /// Applies the current focus.
        /// </summary>
        private void ApplyFocus()
        {
            // Save as last updated
            lastFocus = focus;

            // Convert to render value
            renderFocus = PercentToRange(RefractController.FOCUS_MIN, RefractController.FOCUS_MAX, focus);

            // Move projector
            ApplyProjectorPosition();
        }

        /// <summary>
        /// Applies the current interpolation.
        /// </summary>
        private void ApplyInterpolation()
        {
            // Save as last updated
            lastInterpolation = interpolation;

            // Convert to render value
            int renderInt = Mathf.RoundToInt(interpolation * (INTERPOLATION_MAX - INTERPOLATION_MIN));
            renderInterpolation = (ViewInterpolationType)renderInt;

            // Update Holoplay
            holoplay.viewInterpolation = renderInterpolation;
        }

        /// <summary>
        /// Applies the projector position based on depthiness and focus.
        /// </summary>
        private void ApplyProjectorPosition()
        {
            // Get the current position
            Vector3 pos = projector.transform.position;

            // Update z based on render depthiness and focus
            pos.z = (renderDepthiness / 2f) + renderFocus;

            // Move the projector
            projector.transform.position = pos;
        }

        /// <summary>
        /// Applies the current tessellation.
        /// </summary>
        private void ApplyTessellation()
        {
            // Save as last updated
            lastTessellation = tessellation;

            // Convert to render value
            renderTessellation = PercentToRange(TESSELLATION_MIN, TESSELLATION_MAX, tessellation);

            // Make sure we don't actually go all the way down to zero since technically the shader doesn't support it
            renderTessellation = Mathf.Clamp(renderTessellation, 0.1f, TESSELLATION_MAX);

            // Update the displacement shader tessellation factor
            projectorMaterial.SetFloat("_TessFactor", renderTessellation);
        }

        /// <summary>
        /// Updates shared scene mode where the scene and the menu render at the same time.
        /// </summary>
        private void UpdateSharedScene()
        {
            // Are we enabling sharing?
            if (showSceneInMenu)
            {
                // Make sure the projector is on
                projector.gameObject.SetActive(true);

                // Turn menu camera on
                menuCamera.enabled = true;

                // Exclude UI in holo camera
                holoPlay.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            }
            else
            {
                // The state of the projector is the opposite of the menu
                projector.gameObject.SetActive(!menuController.IsShown);

                // Turn menu camera off
                menuCamera.enabled = false;

                // Include UI in holo camera
                holoPlay.cullingMask |= 1 << LayerMask.NameToLayer("UI");
            }
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        /// <summary>
        /// Occurs when the menu is hidden.
        /// </summary>
        private void MainMenu_Hidden()
        {
            // No longer sharing
            UpdateSharedScene();
        }

        /// <summary>
        /// Occurs when the menu is shown.
        /// </summary>
        private void MainMenu_Shown()
        {
            // Show the scene while menu is open?
            UpdateSharedScene();
        }
        #endregion // Overrides / Event Handlers

        #region Unity Overrides
        /// <inheritdoc/>
        protected virtual void OnDisable()
        {
            menuController.Hidden.RemoveListener(MainMenu_Hidden);
            menuController.Shown.RemoveListener(MainMenu_Shown);
        }

        /// <inheritdoc/>
        protected virtual void OnEnable()
        {
            menuController.Hidden.AddListener(MainMenu_Hidden);
            menuController.Shown.AddListener(MainMenu_Shown);
        }

        /// <summary>
        /// Called when the behavior begins.
        /// </summary>
        protected virtual void Start()
        {
            // Make sure we have a projector
            if (projector == null)
            {
                Debug.LogError($"{nameof(RefractController)} - projector isn't set.");
                enabled = false;
                return;
            }

            // Turn off V-Sync
            Application.targetFrameRate = -1;

            // Get the projector material
            projectorMaterial = projector.sharedMaterial;

            // Attempt to load settings
            LoadSettings();
        }

        /// <summary>
        /// Called once per frame.
        /// </summary>
        protected virtual void Update()
        {
            if (depthiness != lastDepthiness) { ApplyDepthiness(); }
            if (focus != lastFocus) { ApplyFocus(); }
            if (tessellation != lastTessellation) { ApplyTessellation(); }
            if (interpolation != lastInterpolation) { ApplyInterpolation(); }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Loads application settings from disk.
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                // Attempt to load settings from disk
                var settings = DataStore.LoadObject<RefractSettings>(nameof(RefractSettings));

                // If loaded, apply
                if (settings != null)
                {
                    this.Depthiness = settings.Depthiness;
                    this.Focus = settings.Focus;
                    this.Interpolation = settings.Interpolation;
                    this.ShowSceneInMenu = settings.ShowSceneInMenu;
                    this.Tessellation = settings.Tessellation;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Could not load settings: {ex.Message}.");
            }
        }

        /// <summary>
        /// Causes Refract to exit.
        /// </summary>
        public void Quit()
        {
            // Save settings
            SaveSettings();

            // Exit the app
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Saves application settings to disk.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Copy to settings object
                var settings = new RefractSettings()
                {
                    Depthiness = this.depthiness,
                    Focus = this.focus,
                    Interpolation = this.interpolation,
                    ShowSceneInMenu = this.showSceneInMenu,
                    Tessellation = this.tessellation,
                };

                // Save settings object to disk
                DataStore.SaveObject(settings, nameof(RefractSettings));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Could not save settings: {ex.Message}.");
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets how much displacement is caused by the depth map.
        /// </summary>
        public float Depthiness
        {
            get => depthiness;
            set
            {
                depthiness = RoundClamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets which part of the projector is currently focused. 0.5 is middle.
        /// </summary>
        public float Focus
        {
            get => focus;
            set
            {
                focus = RoundClamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets which the amount of interpolation used by Holoplay.
        /// </summary>
        /// <remarks>
        /// Interpolation determines how many camera angles are estimated rather than fully calculated as Holoplay
        /// generates frames for the Looking Glass. The default value of 0 produces the highest quality picture
        /// possible, but is very demanding on the GPU. This slider will cause a dramatic boost in FPS, but it will
        /// also cause a dramatic decrease in picture quality.
        /// </remarks>
        /// <seealso href="https://docs.lookingglassfactory.com/developer-tools/unity/scripts/holoplay#viewinterpolation"/>
        public float Interpolation
        {
            get => interpolation;
            set
            {
                interpolation = RoundClamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the amount of tessellation used by the shader.
        /// </summary>
        /// <remarks>
        /// Tessellation determines the number of 3D points that are generated from the depth map. Higher tessellation
        /// values represent curves more accurately, but at the cost of performance. The default value of 0.5 should
        /// work well for medium to high-end gaming PCs.
        /// </remarks>
        public float Tessellation
        {
            get => tessellation;
            set
            {
                tessellation = RoundClamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the displaced and colored virtual scene projector.
        /// </summary>
        public MeshRenderer Projector { get => projector; set => projector = value; }

        /// <summary>
        /// Gets or sets whether to show the running scene while the menu is open.
        /// </summary>
        public bool ShowSceneInMenu
        {
            get => showSceneInMenu;
            set
            {
                // Store
                showSceneInMenu = value;

                // Update shared scene
                UpdateSharedScene();
            }
        }
        #endregion // Public Properties
    }
}