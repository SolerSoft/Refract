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
        private const float FOCUS_DEFAULT = 0;
        private const float FOCUS_MAX = 10;
        private const float FOCUS_MIN = -10;
        #endregion // Constants

        #region Member Variables
        private float last_deptiness = float.NaN;
        private float last_focus = float.NaN;
        private Material projectorMaterial;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("How much displacement is caused by the depth map.")]
        [Range(DEPTHINESS_MIN, DEPTHINESS_MAX)]
        private float depthiness = DEPTHINESS_DEFAULT;

        [SerializeField]
        [Tooltip("Which part of the projector is currently focused. Zero is middle.")]
        [Range(FOCUS_MIN, FOCUS_MAX)]
        private float focus = FOCUS_DEFAULT;

        [SerializeField]
        [Tooltip("The displaced and colored virtual scene projector.")]
        private MeshRenderer projector;
        #endregion // Unity Inspector Variables

        #region Internal Methods
        /// <summary>
        /// Applies the current depthiness.
        /// </summary>
        private void ApplyDepthiness()
        {
            // Save as last updated
            last_deptiness = depthiness;

            // Update the displacement shader
            projectorMaterial.SetFloat("_DispFactor", depthiness);

            // Move projector
            ApplyProjectorPosition();
        }

        /// <summary>
        /// Applies the current focus.
        /// </summary>
        private void ApplyFocus()
        {
            // Save as last updated
            last_focus = focus;

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

            // Update z based on depthiness and focus
            pos.z = (depthiness / 2f) + focus;

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

            // Get the projector material
            projectorMaterial = projector.sharedMaterial;
        }

        /// <summary>
        /// Called once per frame.
        /// </summary>
        protected virtual void Update()
        {
            if (depthiness != last_deptiness) { ApplyDepthiness(); }
            if (focus != last_focus) { ApplyFocus(); }
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
                depthiness = Mathf.Clamp(value, DEPTHINESS_MIN, DEPTHINESS_MAX);
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
                focus = Mathf.Clamp(value, FOCUS_MIN, FOCUS_MAX);
            }
        }
        #endregion // Public Properties
    }
}