using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Base class for a UI control.
    /// </summary>
    public abstract class UIControl : MonoBehaviour
    {
        #region Overrides / Event Handlers
        /// <summary>
        /// Called when the element has been activated.
        /// </summary>
        /// <returns>
        /// A <see cref="UIControl"/> that wishes to be captured; otherwise <see langword = "null" />.
        /// </returns>
        protected virtual UIControl OnActivate()
        {
            return null;
        }

        /*
        /// <summary>
        /// Called when element has captured input.
        /// </summary>
        protected virtual void OnGotCapture() { }
        */

        /// <summary>
        /// Called when the element has received focus.
        /// </summary>
        protected virtual void OnGotFocus() { }

        /*
        /// <summary>
        /// Called when element has lost captured input.
        /// </summary>
        protected virtual void OnLostCapture() { }
        */

        /// <summary>
        /// Called when the element has lost focus.
        /// </summary>
        protected virtual void OnLostFocus() { }

        /// <summary>
        /// Called when the 'Next' command is requested.
        /// </summary>
        protected virtual void OnNext() { }

        /// <summary>
        /// Called when the 'Previous' command is requested.
        /// </summary>
        protected virtual void OnPrevious() { }
        #endregion // Overrides / Event Handlers

        #region Public Methods
        /// <summary>
        /// Activates the element.
        /// </summary>
        /// <returns>
        /// A <see cref="UIControl"/> that wishes to be captured; otherwise <see langword = "null" />.
        /// </returns>
        public UIControl Activate()
        {
            return OnActivate();
        }

        /*
        /// <summary>
        /// Notify the element that it has captured input.
        /// </summary>
        public void NotifyGotCapture()
        {
            OnGotCapture();
        }
        */

        /// <summary>
        /// Notify the element that it has received focus.
        /// </summary>
        public void NotifyGotFocus()
        {
            OnGotFocus();
        }

        /*
        /// <summary>
        /// Notify the element that it has lost captured input.
        /// </summary>
        public void NotifyLostCapture()
        {
            OnLostCapture();
        }
        */

        /// <summary>
        /// Notify the element that it has lost focus.
        /// </summary>
        public void NotifyLostFocus()
        {
            OnLostFocus();
        }

        /// <summary>
        /// Instruct the behavior to perform the "Next" command.
        /// </summary>
        public void Next()
        {
            OnNext();
        }

        /// <summary>
        /// Instruct the behavior to perform the "Previous" command.
        /// </summary>
        public void Previous()
        {
            OnPrevious();
        }
        #endregion // Public Methods
    }
}