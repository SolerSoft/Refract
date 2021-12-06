using LookingGlass.Menu;
using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("The MenuController that controls the main menu.")]
        private MenuController menuController;


        [Header("Settings")]
        [SerializeField]
        [Tooltip("Whether to show the running scene while the menu is open.")]
        private bool showSceneInMenu;
        #endregion // Unity Inspector Variables

        #region Overrides / Event Handlers
        /// <summary>
        /// Occurs when the menu is hidden.
        /// </summary>
        private void MainMenu_Hidden()
        {
            // Make sure the scene is visible again
            holoController.Projector.gameObject.SetActive(true);
        }

        /// <summary>
        /// Occurs when the menu is shown.
        /// </summary>
        private void MainMenu_Shown()
        {
            // Hide the scene while the menu is open?
            if (!showSceneInMenu)
            {
                holoController.Projector.gameObject.SetActive(false);
            }
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