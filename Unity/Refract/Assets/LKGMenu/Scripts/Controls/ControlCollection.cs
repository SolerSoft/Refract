using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// A <see cref="UIControl"/> that contains other UI controls.
    /// </summary>
    public class ControlCollection : UIControl
    {
        #region Member Variables
        private int currentIndex = -1;
        private UIControl focusedControl;
        private UIControl lastActivated;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The list of controls in the collection.")]
        private UIControl[] controls;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Sets focus to the specified control.
        /// </summary>
        /// <param name="control">
        /// The element to focus.
        /// </param>
        private void SetFocus(UIControl control)
        {
            // Validate the list
            ValidateControls();

            // If already focused, ignore
            if (control == focusedControl) { return; }

            // Placeholder index
            int index = -1;

            // If receiving a valid Interactable, make sure it's in the list
            if (control != null)
            {
                // Get the index
                index = Array.IndexOf(controls, control);

                // Make sure it's in the list
                if (index < 0) { throw new InvalidOperationException($"{nameof(control)} was not found in {nameof(Controls)}."); }
            }

            // If existing, lose focus
            if (focusedControl != null)
            {
                focusedControl.NotifyLostFocus();
            }

            // Store the new index
            currentIndex = index;

            // Store the new input
            focusedControl = control;

            // If valid, focus it and capture if already selected
            if (focusedControl != null)
            {
                // Focus
                focusedControl.NotifyGotFocus();
            }
        }

        /// <summary>
        /// Sets focus to the element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The index of the Interactable to focus.
        /// </param>
        private void SetFocus(int index)
        {
            // Validate the list
            ValidateControls();

            // Validate the index
            if ((index < -1) || (index > controls.Length - 1)) { throw new ArgumentOutOfRangeException(nameof(index)); }

            // Set focus by reference
            if (index == -1)
            {
                SetFocus(null);
            }
            else
            {
                SetFocus(controls[index]);
            }
        }

        /// <summary>
        /// Validates the inputs list.
        /// </summary>
        private void ValidateControls()
        {
            if (controls == null)
            {
                throw new InvalidOperationException($"{nameof(Controls)} list can't be null.");
            }
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        protected virtual void Start()
        {
            // Find new controls
            if ((controls == null) || (controls.Length < 1))
            {
                Controls = this.GetComponentsInDirectChildren<UIControl>();
            }
        }
        #endregion // Unity Overrides

        #region Overrides / Event Handlers
        /// <inheritdoc/>
        protected override UIControl OnActivate()
        {
            // If this collection hasn't captured input yet, request to capture
            if (!HasCapture)
            {
                // Request capture
                return this;
            }

            // We have capture. Is the same control being activated twice in a row?
            // If so, we just need to release control
            if ((lastActivated != null) && (lastActivated == focusedControl))
            {
                // Release capture
                return null;
            }

            // Not gaining or releasing capture, so pass on to the focused control
            return focusedControl?.Activate();
        }

        /// <inheritdoc/>
        protected override void OnGotFocus()
        {
            // Pass to base first
            base.OnGotFocus();

            // If no currently focused control for the group, set to first available control
            if (focusedControl == null)
            {
                if (controls.Length > 0) { SetFocus(0); }
            }
            else
            {
                // Notify current control that it got focus since the group got focus
                focusedControl?.NotifyGotFocus();
            }
        }

        /// <inheritdoc/>
        protected override void OnLostCapture()
        {
            // Pass to base first
            base.OnLostCapture();

            // We're losing capture, so forget the last control
            lastActivated = null;
        }

        /// <inheritdoc/>
        protected override void OnLostFocus()
        {
            // Pass to base first
            base.OnLostFocus();

            // Notify current control that it lost focus
            focusedControl?.NotifyLostFocus();
        }

        /// <inheritdoc/>
        protected override void OnNext()
        {
            // Pass to base first
            base.OnNext();

            // If there are no controls, nothing to do
            if ((controls == null) || (controls.Length < 1)) { return; }

            // Calculate next index
            int nextIndex = currentIndex + 1;

            // Check for loop
            if (nextIndex >= controls.Length) { nextIndex = 0; }

            // Go!
            SetFocus(nextIndex);
        }

        /// <inheritdoc/>
        protected override void OnPrevious()
        {
            // Pass to base first
            base.OnPrevious();

            // If there are no controls, nothing to do
            if ((controls == null) || (controls.Length < 1)) { return; }

            // Calculate next index
            int nextIndex = currentIndex - 1;

            // Check for loop
            if (nextIndex == -1) { nextIndex = controls.Length - 1; }

            // Go!
            SetFocus(nextIndex);
        }
        #endregion // Overrides / Event Handlers

        #region Public Properties
        /// <summary>
        /// Gets or sets the index of the currently focused <see cref="Interactable"/>.
        /// </summary>
        public int FocusedIndex
        {
            get => currentIndex;
            set
            {
                SetFocus(value);
            }
        }

        /// <summary>
        /// Gets or sets the currently focused <see cref="UIControl"/>.
        /// </summary>
        public UIControl FocusedControl
        {
            get => focusedControl;
            set
            {
                SetFocus(value);
            }
        }

        /// <summary>
        /// Gets or sets the list of child controls.
        /// </summary>
        public UIControl[] Controls
        {
            get => controls;
            set
            {
                if (value != controls)
                {
                    // Store
                    controls = value;
                }
            }
        }
        #endregion // Public Properties
    }
}
