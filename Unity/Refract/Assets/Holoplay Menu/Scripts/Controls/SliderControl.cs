using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Menu
{
    /// <summary>
    /// Allows a MRTK <see cref="PinchSlider"/> to function as a <see cref="UIControl"/>.
    /// </summary>
    public class SliderControl : InteractableControl
    {
        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The amount the slider changes on each input event.")]
        [Range(0.01f, 0.5f)]
        private float changeAmount = 0.1f;

        [SerializeField]
        [Tooltip("The pinch slider that will be controlled when input is captive.")]
        private PinchSlider slider;
        #endregion // Unity Inspector Variables

        /// <inheritdoc/>
        protected override UIControl OnActivate()
        {
            // NOTE: Intentionally do not pass on to base

            // Are we captured?
            if (HasCapture)
            {
                // Release capture
                return null;
            }
            else
            {
                // Try to capture
                return this;
            }
        }

        /// <inheritdoc/>
        protected override void OnGotCapture()
        {
            // Use "Toggled" to show capture
            if (Interactable != null)
            {
                Interactable.IsToggled = true;
            }
            base.OnGotCapture();
        }

        /// <inheritdoc/>
        protected override void OnLostCapture()
        {
            // Use "Not Toggled" to show loss of capture
            if (Interactable != null)
            {
                Interactable.IsToggled = false;
            }
            base.OnGotCapture();
        }

        /// <inheritdoc/>
        protected override void OnNext()
        {
            float newVal = Mathf.Clamp(slider.SliderValue + changeAmount, 0, 1);
            slider.SliderValue = newVal;
            base.OnNext();
        }

        /// <inheritdoc/>
        protected override void OnPrevious()
        {
            float newVal = Mathf.Clamp(slider.SliderValue - changeAmount, 0, 1);
            slider.SliderValue = newVal;
            base.OnPrevious();
        }
    }
}