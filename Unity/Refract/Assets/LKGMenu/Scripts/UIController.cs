using LookingGlass;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Routes input to one or more <see cref="UIControl"/>s.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        #region Member Variables
        private Stack<UIControl> controlStack = new Stack<UIControl>();
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Looking Glass Input")]
        [SerializeField]
        [Tooltip("The Looking Glass button that will activate the control with focus.")]
        private HardwareButton activateButton = HardwareButton.PlayPause;

        [SerializeField]
        [Tooltip("The Looking Glass button that will perform the 'Next' command.")]
        private HardwareButton nextButton = HardwareButton.Forward;

        [SerializeField]
        [Tooltip("The Looking Glass button that will perform the 'Previous' command.")]
        private HardwareButton previousButton = HardwareButton.Back;


        [Header("UI")]
        [SerializeField]
        [Tooltip("The current control that has focus.")]
        private UIControl currentControl;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Sets the current control to the specified control.
        /// </summary>
        /// <param name="newControl">
        /// The new control to set as the current control.
        /// </param>
        private void SetCurrentControl(UIControl newControl)
        {
            // Make sure changing
            if (newControl == currentControl) { return; }

            // Notify the current control that it's losing capture and focus
            currentControl?.NotifyLostCapture();
            currentControl?.NotifyLostFocus();

            // Store the new control
            currentControl = newControl;

            // Notify the new control that it's got focus and capture
            currentControl?.NotifyGotFocus();
            currentControl?.NotifyGotCapture();
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        protected virtual void Start()
        {
            // Was a control provided on start?
            if (currentControl == null)
            {
                // No, search for one
                CurrentControl = GetComponent<UIControl>();
            }
            else
            {
                // Yes, get it into the right starting state
                controlStack.Push(currentControl);
                currentControl.NotifyGotFocus();
                currentControl.NotifyGotCapture();
            }
        }

        protected virtual void Update()
        {
            // Check keys and buttons
            if (InputManager.GetButtonDown(nextButton))
            {
                Next();
            }
            else if (InputManager.GetButtonDown(previousButton))
            {
                Previous();
            }
            else if (InputManager.GetButtonDown(activateButton))
            {
                Activate();
            }
        }
        #endregion // Unity Overrides

        #region Public Methods
        /// <summary>
        /// Activates the currently focused <see cref="UIControl"/>.
        /// </summary>
        public void Activate()
        {
            // Validate
            if (currentControl == null) throw new InvalidOperationException($"{nameof(CurrentControl)} is currently null.");

            // Activate the control and see if it provides a new one
            UIControl newControl = currentControl.Activate();

            // Is the new control different from the current control?
            if (newControl != currentControl)
            {
                // New control means to push the stack
                if (newControl != null)
                {
                    // Put in history
                    controlStack.Push(currentControl);

                    // Store new control
                    SetCurrentControl(newControl);
                }
                // No control means to pop the stack
                else
                {
                    // Is there even another control to go back to?
                    if (controlStack.Count > 0)
                    {
                        // Pop to previous control
                        SetCurrentControl(controlStack.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// Performs the 'Next' command.
        /// </summary>
        public void Next()
        {
            // Validate
            if (currentControl == null) throw new InvalidOperationException($"{nameof(CurrentControl)} is currently null.");

            // Pass to control
            currentControl.Next();
        }

        /// <summary>
        /// Performs the 'Previous' command.
        /// </summary>
        public void Previous()
        {
            // Validate
            if (currentControl == null) throw new InvalidOperationException($"{nameof(CurrentControl)} is currently null.");

            // Pass to control
            currentControl.Previous();
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the currently focused <see cref="UIControl"/>.
        /// </summary>
        public UIControl CurrentControl
        {
            get => currentControl;
            set
            {
                SetCurrentControl(value);
            }
        }
        #endregion // Public Properties
    }
}