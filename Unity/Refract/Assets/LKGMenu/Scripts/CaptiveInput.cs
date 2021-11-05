using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Base class for a behavior that handles captured input.
    /// </summary>
    public abstract class CaptiveInput : MonoBehaviour
    {
        #region Overrides / Event Handlers
        /// <summary>
        /// Called when input has been captured.
        /// </summary>
        protected virtual void OnCaptured() { }

        /// <summary>
        /// Called when input has been lost.
        /// </summary>
        protected virtual void OnLost() { }

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
        /// Notify the behavior that it has captured input.
        /// </summary>
        public void NotifyCaptured()
        {
            OnCaptured();
        }

        /// <summary>
        /// Notify the behavior that it has input has been lost.
        /// </summary>
        public void NotifyLost()
        {
            OnLost();
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