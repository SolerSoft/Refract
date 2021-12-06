using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Menu
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
        /// <inheritdoc/>
        protected override void Awake()
        {
            // If there is no control specified, look for one on the same GameObject
            if (interactable == null)
            {
                interactable = GetComponent<Interactable>();
            }

            // Pass to base to complete
            base.Awake();
        }

        protected override void OnEnable()
        {
            // Pass to base first
            base.OnEnable();

            // Make sure the interactable reflects the right state
            if (interactable != null)
            {
                interactable.HasFocus = this.HasFocus;
            }
        }
        #endregion // Unity Overrides

        #region Overrides / Event Handlers
        /// <inheritdoc/>
        protected override UIControl OnActivate()
        {
            // Activate the interactable
            if (interactable != null)
            {
                interactable.TriggerOnClick();
            }

            // Pass on to base
            return base.OnActivate();
        }

        /// <inheritdoc/>
        protected override void OnGotFocus()
        {
            // Pass to base first
            base.OnGotFocus();

            // Notify interactable that it has received focus
            if (interactable != null)
            {
                interactable.HasFocus = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnLostFocus()
        {
            // Pass to base first
            base.OnLostFocus();

            // Notify interactable that focus has been lost
            if (interactable != null)
            {
                interactable.HasFocus = false;
            }
        }
        #endregion // Overrides / Event Handlers

        #region Public Properties
        /// <summary>
        /// Gets or sets the <see cref="Interactable"/> represented by this control.
        /// </summary>
        public Interactable Interactable { get => interactable; set => interactable = value; }
        #endregion // Public Properties
    }
}