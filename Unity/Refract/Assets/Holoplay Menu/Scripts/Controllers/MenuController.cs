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
        /// Ensures that the menu button is the first child.
        /// </summary>
        private void EnsureMenuButtonFirstChild()
        {
            // Do we have a control collection and menu button?
            if ((controlCollection != null) && (menuButton != null))
            {
                // Get the index of the menu button
                int index = controlCollection.Controls.IndexOf(menuButton);

                // What we do now is based on the index
                if (index == -1)
                {
                    // if it's -1 then it's not in the list and we just need to insert it.
                    controlCollection.Controls.Insert(0, menuButton);
                }
                else if (index == 0)
                {
                    // It's already the first child. Nothing to do.
                }
                else
                {
                    // It's at the wrong index. We need to move it to the beginning
                    controlCollection.Controls.RemoveAt(index);
                    controlCollection.Controls.Insert(0, menuButton);
                }

                // If there's currently no focus, go ahead and focus on the menu button
                if (controlCollection.FocusedControl == null) { controlCollection.FocusedControl = menuButton; }
            }
        }

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
            // Create our events
            if (Hidden == null) { Hidden = new UnityEvent(); }
            if (Shown == null) { Shown = new UnityEvent(); }

            // Pass to base first
            base.Start();

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
                // Yes, assign it as the current control
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
            // Every time we're about to show, make sure the menu button is the first child
            EnsureMenuButtonFirstChild();

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
        /// The menu button hides the menu.
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
