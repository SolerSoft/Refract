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
        #region Unity Inspector Variables
        [Header("Controllers")]
        [SerializeField]
        [Tooltip("The HoloController that adjusts settings in the projector.")]
        private HoloController holoController;

        [SerializeField]
        [Tooltip("The HoloPlay object that controls the display.")]
        private Holoplay holoPlay;

        [SerializeField]
        [Tooltip("The MenuController that controls the main menu.")]
        private MenuController menuController;


        [Header("Control")]
        [SerializeField]
        [Tooltip("The slider that adjusts the depthiness of the projector.")]
        private PinchSlider depthinessSlider;


        [Header("Settings")]
        [SerializeField]
        [Tooltip("Whether to show the running scene while the menu is open.")]
        private bool showSceneInMenu;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        static private float PercentToRange(float min, float max, float percent)
        {
            float r = (max - min);
            return (r * percent) + min;
        }
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
            depthinessSlider.SliderValue = RangeToPercent(HoloController.DEPTHINESS_MIN, HoloController.DEPTHINESS_MAX, holoController.Depthiness);
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        /// <summary>
        /// Occurs when the menu is hidden.
        /// </summary>
        private void MainMenu_Hidden()
        {
            // Unsubscribe from control events
            depthinessSlider.OnValueUpdated.RemoveListener(Depthiness_SliderChanged);

            // Make sure the scene is visible again
            holoController.Projector.gameObject.SetActive(true);
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

            // Hide the scene while the menu is open?
            if (!showSceneInMenu)
            {
                holoController.Projector.gameObject.SetActive(false);
            }
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
        #endregion // Overrides / Event Handlers

        #region Unity Overrides
        /// <inheritdoc/>
        protected virtual void Start()
        {

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
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Causes Refract to exit.
        /// </summary>
        public void Exit()
        {
            Application.Quit();
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

                // Show or hide
                holoController.Projector.gameObject.SetActive(showSceneInMenu);
            }
        }
        #endregion // Public Properties
    }
}