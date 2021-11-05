using Microsoft.MixedReality.Toolkit.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LKGMenu
{
    /// <summary>
    /// Allows a MRTK <see cref="PinchSlider"/> to function as a <see cref="UIControl"/>.
    /// </summary>
    public class SliderControl : UIControl
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