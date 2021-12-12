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
        private float lastFPSUpdate;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Controllers")]
        [SerializeField]
        [Tooltip("The main controller for the application.")]
        private RefractController appController;

        [SerializeField]
        [Tooltip("The MenuController that controls the main menu.")]
        private MenuController menuController;


        [Header("Controls")]
        [SerializeField]
        [Tooltip("The slider that adjusts the depthiness of the projector.")]
        private Interactable showSceneInMenuBox;

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
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Sets the menu controls to match the scene.
        /// </summary>
        private void SceneToControls()
        {
            // Check boxes
            showSceneInMenuBox.IsToggled = appController.ShowSceneInMenu;

            // Sliders
            depthinessSlider.SliderValue = appController.Depthiness;
            focusSlider.SliderValue = appController.Focus;
            tessellationSlider.SliderValue = appController.Tessellation;
            interpolationSlider.SliderValue = appController.Interpolation;
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        /// <summary>
        /// Occurs when the menu is hidden.
        /// </summary>
        private void MainMenu_Hidden()
        {
            // Unsubscribe from check box events
            showSceneInMenuBox.OnClick.RemoveListener(ShowSceneInMenuBox_Toggled);

            // Unsubscribe from slider events
            depthinessSlider.OnValueUpdated.RemoveListener(Depthiness_SliderChanged);
            focusSlider.OnValueUpdated.RemoveListener(Focus_SliderChanged);
            interpolationSlider.OnValueUpdated.RemoveListener(Interpolation_SliderChanged);
            tessellationSlider.OnValueUpdated.RemoveListener(Tessellation_SliderChanged);
        }

        /// <summary>
        /// Occurs when the menu is shown.
        /// </summary>
        private void MainMenu_Shown()
        {
            // Load controls
            SceneToControls();

            // Subscribe to check box events
            showSceneInMenuBox.OnClick.AddListener(ShowSceneInMenuBox_Toggled);

            // Subscribe to slider events
            depthinessSlider.OnValueUpdated.AddListener(Depthiness_SliderChanged);
            focusSlider.OnValueUpdated.AddListener(Focus_SliderChanged);
            interpolationSlider.OnValueUpdated.AddListener(Interpolation_SliderChanged);
            tessellationSlider.OnValueUpdated.AddListener(Tessellation_SliderChanged);
        }

        /// <summary>
        /// Called when the value of the Depthiness slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Depthiness_SliderChanged(SliderEventData data)
        {
            appController.Depthiness = data.NewValue;
        }

        /// <summary>
        /// Called when the value of the Focus slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Focus_SliderChanged(SliderEventData data)
        {
            appController.Focus = data.NewValue;
        }

        /// <summary>
        /// Called when the value of the Interpolation slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Interpolation_SliderChanged(SliderEventData data)
        {
            appController.Interpolation = data.NewValue;
        }

        /// <summary>
        /// Called when the value of the Tessellation slider has changed.
        /// </summary>
        /// <param name="data">
        /// Event data from the slider.
        /// </param>
        private void Tessellation_SliderChanged(SliderEventData data)
        {
            appController.Tessellation = data.NewValue;
        }

        /// <summary>
        /// Called when the ShowSceneInMenu box is checked or unchecked.
        /// </summary>
        private void ShowSceneInMenuBox_Toggled()
        {
            appController.ShowSceneInMenu = showSceneInMenuBox.IsToggled;
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
            if ((menuController.IsShown) && (fpsText != null) && ((Time.unscaledTime - lastFPSUpdate) > FPS_UPDATE_RATE))
            {
                lastFPSUpdate = Time.unscaledTime;
                int fps = (int)(1f / Time.unscaledDeltaTime);
                fpsText.text = $"{fps} FPS";
            }
        }
        #endregion // Unity Overrides
    }
}