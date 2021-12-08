using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    /// <summary>
    /// Controls aspects of the holographic display.
    /// </summary>
    public class HoloController : MonoBehaviour
    {
        #region Constants
        private const float DEPTHINESS_DEFAULT = 10.0f;
        private const float DEPTHINESS_MAX = 20.0f;
        private const float DEPTHINESS_MIN = 0.0f;
        private const float FOCUS_DEFAULT = 0.5f;
        private const float FOCUS_MAX = 10;
        private const float FOCUS_MIN = -10;
        #endregion // Constants

        #region Member Variables
        private float lastDepthiness = float.NaN;
        private float lastFocus = float.NaN;
        private Material projectorMaterial;
        private float renderDepthiness = DEPTHINESS_DEFAULT;
        private float renderFocus = FOCUS_DEFAULT;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("How much displacement is caused by the depth map.")]
        [Range(0, 1)]
        private float depthiness = 0.5f;

        [SerializeField]
        [Tooltip("Which part of the projector is currently focused. Zero is middle.")]
        [Range(0, 1)]
        private float focus = 0.5f;

        [SerializeField]
        [Tooltip("The displaced and colored virtual scene projector.")]
        private MeshRenderer projector;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Converts a percentage to a range value.
        /// </summary>
        /// <param name="min">
        /// The minimum value in a range.
        /// </param>
        /// <param name="max">
        /// The maximum value in a range.
        /// </param>
        /// <param name="percent">
        /// The percentage to convert.
        /// </param>
        /// <returns>
        /// The ranged value.
        /// </returns>
        static private float PercentToRange(float min, float max, float percent)
        {
            float r = (max - min);
            return (r * percent) + min;
        }

        /// <summary>
        /// Converts a ranged value to a percentage
        /// </summary>
        /// <param name="min">
        /// The minimum value in a range.
        /// </param>
        /// <param name="max">
        /// The maximum value in a range.
        /// </param>
        /// <param name="range">
        /// The ranged value to convert.
        /// </param>
        /// <returns>
        /// The percentage.
        /// </returns>
        static private float RangeToPercent(float min, float max, float range)
        {
            float r = (max - min);
            float mr = range + min;
            return mr / r;
        }

        /// <summary>
        /// Applies the current depthiness.
        /// </summary>
        private void ApplyDepthiness()
        {
            // Save as last updated
            lastDepthiness = depthiness;

            // Convert to render value
            renderDepthiness = PercentToRange(DEPTHINESS_MIN, DEPTHINESS_MAX, depthiness);

            // Update the displacement shader
            projectorMaterial.SetFloat("_DispFactor", renderDepthiness);

            // Move projector
            ApplyProjectorPosition();
        }

        /// <summary>
        /// Applies the current focus.
        /// </summary>
        private void ApplyFocus()
        {
            // Save as last updated
            lastFocus = focus;

            // Convert to render value
            renderFocus = PercentToRange(HoloController.FOCUS_MIN, HoloController.FOCUS_MAX, focus);

            // Move projector
            ApplyProjectorPosition();
        }

        /// <summary>
        /// Applies the projector position based on depthiness and focus.
        /// </summary>
        private void ApplyProjectorPosition()
        {
            // Get the current position
            Vector3 pos = projector.transform.position;

            // Update z based on render depthiness and focus
            pos.z = (renderDepthiness / 2f) + renderFocus;

            // Move the projector
            projector.transform.position = pos;
        }
        #endregion // Internal Methods

        #region Unity Overrides
        /// <summary>
        /// Called when the behavior begins.
        /// </summary>
        protected virtual void Start()
        {
            // Make sure we have a projector
            if (projector == null)
            {
                Debug.LogError($"{nameof(HoloController)} - projector isn't set.");
                enabled = false;
                return;
            }

            // Turn off V-Sync
            Application.targetFrameRate = -1;

            // Get the projector material
            projectorMaterial = projector.sharedMaterial;
        }

        /// <summary>
        /// Called once per frame.
        /// </summary>
        protected virtual void Update()
        {
            if (depthiness != lastDepthiness) { ApplyDepthiness(); }
            if (focus != lastFocus) { ApplyFocus(); }
        }
        #endregion // Unity Overrides

        #region Public Properties
        /// <summary>
        /// Gets or sets how much displacement is caused by the depth map.
        /// </summary>
        public float Depthiness
        {
            get => depthiness;
            set
            {
                depthiness = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets which part of the projector is currently focused. Zero is middle.
        /// </summary>
        public float Focus
        {
            get => focus;
            set
            {
                focus = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the displaced and colored virtual scene projector.
        /// </summary>
        public MeshRenderer Projector { get => projector; set => projector = value; }
        #endregion // Public Properties
    }
}