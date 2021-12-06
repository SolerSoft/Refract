using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// A <see cref="UIController"/> designed to control a menu.
    /// </summary>
    public class MenuController : UIController
    {
        #region Unity Inspector Variables
        [Header("Menu")]
        [SerializeField]
        [Tooltip("The GameObject that represents the root of the menu.")]
        private GameObject menuRoot;
        #endregion // Unity Inspector Variables

        #region Public Methods
        /// <inheritdoc/>
        public override void Activate()
        {
            // Make sure the menu root visible or pass it on?
            if ((menuRoot != null) && (!menuRoot.activeSelf))
            {
                // Show menu
                menuRoot.SetActive(true);
            }
            else
            {
                // Pass on to base
                base.Activate();
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the <see cref="GameObject"/> that represents the root of the menu.
        /// </summary>
        /// <remarks>
        /// The root of the menu will be shown and hidden based on user input.
        /// </remarks>
        public GameObject MenuRoot { get => menuRoot; set => menuRoot = value; }
        #endregion // Public Properties
    }
}
