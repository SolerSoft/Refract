using LookingGlass;
using LookingGlass.Menu;
using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Refract
{
    /// <summary>
    /// Handles all input and control of the Refract main menu.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        #region Constants
        private float FPS_UPDATE_RATE = 0.5f;
        #endregion // Constants

        #region Member Variables
        private bool isShown;
        private float lastFPSUpdate;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Controllers")]
        [SerializeField]
        [Tooltip("The HoloController that adjusts settings in the projector.")]
        private HoloController holoController;

        [SerializeField]
        [Tooltip("The HoloPlay object that controls the display.")]
        private Holoplay holoPlay;

        [SerializeField]
        [Tooltip("The camera dedicated to the menu when in shared rendering mode.")]
        private Camera menuCamera;

        [SerializeField]
        [Tooltip("The MenuController that controls the main menu.")]
        private MenuController menuController;


        [Header("Controls")]
        [SerializeField]
        [Tooltip("The slider that adjusts the depthiness of the projector.")]
        private PinchSlider depthinessSlider;

        [SerializeField]
        [Tooltip("The slider that adjusts the focus of the projector.")]
        private PinchSlider focusSlider;

        [SerializeField]
        [Tooltip("The slider that adjusts the tessellation of the projector.")]
        private PinchSlider tessellationSlider;

        [SerializeField]
        [Tooltip("The slider that adjusts the interpolation of the Holoplay capture.")]
        private PinchSlider interpolationSlider;

        [SerializeField]
        [Tooltip("The control that displays the frame rate.")]
        private TextMesh fpsText;

        [SerializeField]
        [Tooltip("The control that displays the version.")]
        private TextMesh versionText;


        [Header("Settings")]
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
        /// Sets the menu controls to match the scene.
        /// </summary>
        private void SceneToControls()
        {
            // Sliders
            depthinessSlider.SliderValue = RangeToPercent(HoloController.DEPTHINESS_MIN, HoloController.DEPTHINESS_MAX, holoController.Depthiness);
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
                holoController.Projector.gameObject.SetActive(true);

                // Turn menu camera on
                menuCamera.enabled = true;

                // Exclude UI in holo camera
                holoPlay.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            }
            else
            {
                // The state of the projector is the opposite of the menu
                holoController.Projector.gameObject.SetActive(!isShown);

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
            // No longer shown
            isShown = false;

            // Unsubscribe from control events
            depthinessSlider.OnValueUpdated.RemoveListener(Depthiness_SliderChanged);
            focusSlider.OnValueUpdated.RemoveListener(Focus_SliderChanged);
            interpolationSlider.OnValueUpdated.RemoveListener(Interpolation_SliderChanged);
            tessellationSlider.OnValueUpdated.RemoveListener(Tessellation_SliderChanged);

            // No longer sharing
            UpdateSharedScene();
        }

        /// <summary>
        /// Occurs when the menu is shown.
        /// </summary>
        private void MainMenu_Shown()
        {
            // Load controls
            SceneToControls();

            // Subscribe to control events
            depthinessSlider.OnValueUpdated.AddListener(Depthiness_SliderChanged);
            focusSlider.OnValueUpdated.AddListener(Focus_SliderChanged);
            interpolationSlider.OnValueUpdated.AddListener(Interpolation_SliderChanged);
            tessellationSlider.OnValueUpdated.AddListener(Tessellation_SliderChanged);

            // Shown
            isShown = true;

            // Show the scene while menu is open?
            UpdateSharedScene();
        }

        /// <summary>
        /// Called when the value of the Depthiness slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Depthiness_SliderChanged(SliderEventData data)
        {
            holoController.Depthiness = PercentToRange(HoloController.DEPTHINESS_MIN, HoloController.DEPTHINESS_MAX, data.NewValue);
        }

        /// <summary>
        /// Called when the value of the Focus slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Focus_SliderChanged(SliderEventData data)
        {
            holoController.Focus = PercentToRange(HoloController.FOCUS_MIN, HoloController.FOCUS_MAX, data.NewValue);
        }

        /// <summary>
        /// Called when the value of the Interpolation slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Interpolation_SliderChanged(SliderEventData data)
        {
            // holoController.Focus = PercentToRange(HoloController.FOCUS_MIN, HoloController.FOCUS_MAX, data.NewValue);
        }

        /// <summary>
        /// Called when the value of the Tessellation slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Tessellation_SliderChanged(SliderEventData data)
        {
            // holoController.Focus = PercentToRange(HoloController.FOCUS_MIN, HoloController.FOCUS_MAX, data.NewValue);
        }
        #endregion // Overrides / Event Handlers

        #region Unity Overrides
        /// <inheritdoc/>
        protected virtual void Start()
        {
            menuController.Title = Application.productName;

            if (versionText != null) { versionText.text = $"v{Application.version}"; }
        }

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

        /// <inheritdoc/>
        protected virtual void Update()
        {
            if ((isShown) && (fpsText != null) && ((Time.unscaledTime - lastFPSUpdate) > FPS_UPDATE_RATE))
            {
                lastFPSUpdate = Time.unscaledTime;
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsText.text = $"{fps} FPS";
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Causes Refract to exit.
        /// </summary>
        public void Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion // Public Methods

        #region Public Properties
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