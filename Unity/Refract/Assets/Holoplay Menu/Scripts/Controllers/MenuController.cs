using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

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

        [SerializeField]
        [Tooltip("Indicates if the menu will be shown when the app is started.")]
        private bool showOnStart = false;


        [Header("Controls")]
        [SerializeField]
        [Tooltip("The collection of controls displayed in the menu.")]
        private ControlCollection controlCollection;

        [SerializeField]
        [Tooltip("The UI Control that represents the menu button.")]
        private UIControl menuButton;

        [SerializeField]
        [Tooltip("The control that represents the title.")]
        private TextMesh titleControl;

        [SerializeField]
        [Tooltip("The control that represents the subtitle.")]
        private TextMesh subtitleControl;


        [Header("Text")]
        [SerializeField]
        [Tooltip("The title of the menu.")]
        private string title = "Title";

        [SerializeField]
        [Tooltip("The subtitle of the menu.")]
        private string subtitle = "Subtitle";
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Updates controls, if available.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SyncControls()
        {
            if (titleControl != null) { titleControl.text = title; }
            if (subtitleControl != null) { subtitleControl.text = subtitle; }
            if (menuRoot != null) { menuRoot.SetActive(showOnStart); }
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <inheritdoc/>
        protected override void Start()
        {
            // Pass to base first
            base.Start();

            // Create our events
            Hidden = new UnityEvent();
            Shown = new UnityEvent();

            // Update controls
            SyncControls();

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

            // Show or hide on start
            if (showOnStart)
            {
                // Show
                Show();
            }
            else
            {
                // Hide
                Hide();
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <inheritdoc/>
        public override void Activate()
        {
            // Make sure the menu root visible or pass it on?
            if ((menuRoot != null) && (!menuRoot.activeSelf))
            {
                // Show the menu
                Show();
            }
            else
            {
                // Pass on to base
                base.Activate();
            }
        }

        /// <summary>
        /// Causes the menu to be hidden.
        /// </summary>
        public void Hide()
        {
            // Show menu
            if (menuRoot != null) { menuRoot.SetActive(false); }

            // Notify
            Hidden.Invoke();
        }

        /// <summary>
        /// Causes the menu to be shown.
        /// </summary>
        public void Show()
        {
            // Show menu
            if (menuRoot != null) { menuRoot.SetActive(true); }

            // Notify
            Shown.Invoke();
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

        /// <summary>
        /// Gets or sets a value that indicates if the menu will be shown when the app is started.
        /// </summary>
        public bool ShowOnStart { get => showOnStart; set => showOnStart = value; }

        /// <summary>
        /// Gets or sets the subtitle of the menu.
        /// </summary>
        public string Subtitle
        {
            get => subtitle;
            set
            {
                subtitle = value;
                if (subtitleControl != null) { subtitleControl.text = value; }
            }
        }

        /// <summary>
        /// Gets or sets the title of the menu.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                if (titleControl != null) { titleControl.text = value; }
            }
        }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Raised when the menu is hidden.
        /// </summary>
        [Tooltip("Raised when the menu is hidden.")]
        public UnityEvent Hidden;

        /// <summary>
        /// Raised when the menu is shown.
        /// </summary>
        [Tooltip("Raised when the menu is shown.")]
        public UnityEvent Shown;
        #endregion // Public Events
    }
}
