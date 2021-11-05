using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Allows one or more <see cref="Interactable"/> elements to be controlled by hardware buttons.
    /// </summary>
    public class HardwareInteractableController : MonoBehaviour
    {
        #region Member Variables
        private int currentIndex = -1;
        private Interactable currentInteractable;
        private CaptiveInput captiveInput;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The list of Interactable objects that will be controlled.")]
        private Interactable[] interactables;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Handles the <see cref="Interactables"/> list changing.
        /// </summary>
        private void HandleListChange()
        {
            // Set focus to the first object
            SetFocus(0);
        }

        /// <summary>
        /// Sets focus to the specified Interactable.
        /// </summary>
        /// <param name="interactable">
        /// The Interactable to focus.
        /// </param>
        private void SetFocus(Interactable interactable)
        {
            // Validate the list
            ValidateInteractables();

            // If already focused, ignore
            if (interactable == currentInteractable) { return; }

            // Placeholder index
            int index = -1;

            // If receiving a valid Interactable, make sure it's in the list
            if (interactable != null)
            {
                // Get the index
                index = Array.IndexOf(interactables, interactable);

                // Make sure it's in the list
                if (index < 0) { throw new InvalidOperationException($"{nameof(interactable)} was not found in {nameof(Interactables)}."); }
            }

            // If existing, lose focus
            if (currentInteractable != null)
            {
                currentInteractable.HasFocus = false;
            }

            // Store the new index
            currentIndex = index;

            // Store the new Interactable
            currentInteractable = interactable;
            captiveInput = null;

            // If valid, focus it and capture if already selected
            if (currentInteractable != null)
            {
                // Focus
                currentInteractable.HasFocus = true;

                // Capture input?
                if (currentInteractable.IsToggled)
                {
                    captiveInput = currentInteractable.GetComponent<CaptiveInput>();
                }
            }
        }

        /// <summary>
        /// Sets focus to the Interactable at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the Interactable to focus.
        /// </param>
        private void SetFocus(int index)
        {
            // Validate the list
            ValidateInteractables();

            // Validate the index
            if ((index < -1) || (index > interactables.Length - 1)) { throw new ArgumentOutOfRangeException(nameof(index)); }

            // Set focus by reference
            if (index == -1)
            {
                SetFocus(null);
            }
            else
            {
                SetFocus(interactables[index]);
            }
        }

        /// <summary>
        /// Validates the specified interactable list.
        /// </summary>
        private void ValidateInteractables(Interactable[] list)
        {
            if (list == null || list.Length < 1)
            {
                enabled = false;
                throw new InvalidOperationException($"{nameof(Interactables)} list can't be null or empty.");
            }
        }

        /// <summary>
        /// Validates the interactable list.
        /// </summary>
        private void ValidateInteractables()
        {
            ValidateInteractables(interactables);
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        protected virtual void Start()
        {
            // Set focus to the first object
            SetFocus(0);
        }

        protected virtual void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightBracket))
            {
                Next();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftBracket))
            {
                Previous();
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Backslash))
            {
                Action();
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Activates the currently focused <see cref="Interactable"/>.
        /// </summary>
        public void Action()
        {
            // Validate
            if (currentInteractable == null) throw new InvalidOperationException($"{nameof(CurrentInteractable)} is currently null.");

            // Acivate
            currentInteractable.TriggerOnClick();

            // Capture input?
            if (currentInteractable.IsToggled)
            {
                captiveInput = currentInteractable.GetComponent<CaptiveInput>();
            }
            else
            {
                captiveInput = null;
            }
        }

        /// <summary>
        /// Performs the 'Next' command.
        /// </summary>
        /// <remarks>
        /// If the Interactable has captured input, the command is passed to the Interactable. Otherwise, focus 
        /// moves on to the next Interactable. If the last Interactable is selected, focus moves to the first.
        /// </remarks>
        public void Next()
        {
            // Validate the list
            ValidateInteractables();

            // Captive?
            if (IsCaptive)
            {
                captiveInput.Next();
            }
            else
            {
                // Calculate next index
                int nextIndex = currentIndex + 1;

                // Check for loop
                if (nextIndex >= interactables.Length) { nextIndex = 0; }

                // Go!
                SetFocus(nextIndex);
            }
        }

        /// <summary>
        /// Performs the 'Previous' command.
        /// </summary>
        /// <remarks>
        /// If the Interactable has captured input, the command is passed to the Interactable. Otherwise, focus 
        /// moves on to the previous Interactable. If the first Interactable is selected, focus moves to the last.
        /// </remarks>
        public void Previous()
        {
            // Validate the list
            ValidateInteractables();

            // Captive?
            if (IsCaptive)
            {
                captiveInput.Previous();
            }
            else
            {
                // Calculate next index
                int nextIndex = currentIndex - 1;

                // Check for loop
                if (nextIndex == -1) { nextIndex = interactables.Length - 1; }

                // Go!
                SetFocus(nextIndex);
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the index of the currently focused <see cref="Interactable"/>.
        /// </summary>
        public int CurrentIndex
        {
            get => currentIndex;
            set
            {
                SetFocus(value);
            }
        }

        /// <summary>
        /// Gets or sets the currently focused <see cref="Interactable"/>.
        /// </summary>
        public Interactable CurrentInteractable
        {
            get => currentInteractable;
            set
            {
                SetFocus(value);
            }
        }

        /// <summary>
        /// Gets or sets the list of Interactable objects that will be controlled.
        /// </summary>
        public Interactable[] Interactables
        {
            get => interactables;
            set
            {
                if (value != interactables)
                {
                    // Validate
                    ValidateInteractables(value);

                    // Store
                    interactables = value;

                    // Handle changes
                    HandleListChange();
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates if the current Interactable has captive input.
        /// </summary>
        public bool IsCaptive
        {
            get
            {
                return captiveInput != null;
            }
        }
        #endregion // Public Properties
    }
}