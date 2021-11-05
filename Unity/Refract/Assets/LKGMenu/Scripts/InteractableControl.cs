using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Allows a MRTK <see cref="Interactable"/> to function as a <see cref="UIControl"/>.
    /// </summary>
    public class InteractableControl : UIControl
    {
        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The Interactable that will be controlled when input is captive.")]
        private Interactable interactable;
        #endregion // Unity Inspector Variables

        #region Unity Overrides
        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        protected virtual void Start()
        {
            // If there is no control specified, look for one on the same GameObject
            if (interactable == null)
            {
                interactable = GetComponent<Interactable>();
            }
        }
        #endregion // Unity Overrides

        #region Overrides / Event Handlers
        /// <inheritdoc/>
        protected override UIControl OnActivate()
        {
            // Activate the interactable
            interactable.TriggerOnClick();

            // Pass on to base
            return base.OnActivate();
        }

        /// <inheritdoc/>
        protected override void OnGotFocus()
        {
            // Pass to base first
            base.OnGotFocus();

            // Notify interactable that it has received focus
            interactable.HasFocus = true;
        }

        /// <inheritdoc/>
        protected override void OnLostFocus()
        {
            // Pass to base first
            base.OnLostFocus();

            // Notify interactable that focus has been lost
            interactable.HasFocus = false;
        }
        #endregion // Overrides / Event Handlers
    }
}