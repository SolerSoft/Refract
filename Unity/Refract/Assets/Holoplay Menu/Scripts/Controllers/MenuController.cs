using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LookingGlass.Menu
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

        [Header("Controls")]
        [SerializeField]
        [Tooltip("The collection of controls displayed in the menu.")]
        private ControlCollection controlCollection;

        [SerializeField]
        [Tooltip("The UI Control that represents the menu button.")]
        private UIControl menuButton;
        #endregion // Unity Inspector Variables

        protected override void Start()
        {
            // Pass to base first
            base.Start();

            // If an control collection wasn't provided, try and find one
            if (controlCollection == null)
            {
                controlCollection = GetComponent<ControlCollection>();
            }

            // Do we have a control collection now?
            if (controlCollection != null)
            {
                // If we have a menu button, make sure it's in the list
                if ((menuButton != null) && (!controlCollection.Controls.Contains(menuButton)))
                {
                    controlCollection.Controls.Add(menuButton);
                }

                // Yes, assign it
                CurrentControl = controlCollection;
            }
        }

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
        /// Gets or sets the collection of controls displayed in the menu.
        /// </summary>
        public ControlCollection ControlCollection { get => controlCollection; set => controlCollection = value; }

        /// <summary>
        /// Gets or sets the <see cref="UIControl"/> that represents the menu button.
        /// </summary>
        /// <remarks>
        /// The menu button hides the menu. It is generally the last control in the tab order.
        /// </remarks>
        public UIControl MenuButton { get => menuButton; set => menuButton = value; }

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
