using LookingGlass;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViewInterpolationType = LookingGlass.Holoplay.ViewInterpolationType;

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
        private const float FOCUS_DEFAULT = 5f;
        private const float FOCUS_MAX = 10;
        private const float FOCUS_MIN = -10;
        private const float TESSELLATION_DEFAULT = 15.0f;
        private const float TESSELLATION_MAX = 30f;
        private const float TESSELLATION_MIN = 0.0f;
        private const ViewInterpolationType INTERPOLATION_DEFAULT = ViewInterpolationType.None;
        private const float INTERPOLATION_MAX = 5.0f;
        private const float INTERPOLATION_MIN = 0.0f;
        #endregion // Constants

        #region Member Variables
        private float lastDepthiness = float.NaN;
        private float lastFocus = float.NaN;
        private float lastInterpolation = float.NaN;
        private float lastTessellation = float.NaN;
        private Material projectorMaterial;
        private float renderDepthiness = DEPTHINESS_DEFAULT;
        private float renderFocus = FOCUS_DEFAULT;
        private ViewInterpolationType renderInterpolation = INTERPOLATION_DEFAULT;
        private float renderTessellation = TESSELLATION_DEFAULT;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [Header("Controllers")]
        [SerializeField]
        [Tooltip("The Holoplay object that controls Looking Glass rendering.")]
        private Holoplay holoplay;

        [SerializeField]
        [Tooltip("The displaced and colored virtual scene projector.")]
        private MeshRenderer projector;

        [Header("Scene Settings")]
        [SerializeField]
        [Tooltip("How much displacement is caused by the depth map.")]
        [Range(0, 1)]
        private float depthiness = 0.5f;

        [SerializeField]
        [Tooltip("Which part of the projector is currently focused. 0.5 is middle.")]
        [Range(0, 1)]
        private float focus = 0.5f;

        [SerializeField]
        [Tooltip("The amount of tessellation (detail) used by the shader.")]
        [Range(0, 1)]
        private float tessellation = 0.5f;

        [SerializeField]
        [Tooltip("The amount of interpolation used by Holoplay. This percentage gets converted to an enum internally.")]
        [Range(0, 1)]
        private float interpolation = 0;
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

            // Update the displacement shader displacement factor
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
        /// Applies the current interpolation.
        /// </summary>
        private void ApplyInterpolation()
        {
            // Save as last updated
            lastInterpolation = interpolation;

            // Convert to render value
            int renderInt = Mathf.RoundToInt(interpolation * (INTERPOLATION_MAX - INTERPOLATION_MIN));
            renderInterpolation = (ViewInterpolationType)renderInt;

            // Update Holoplay
            holoplay.viewInterpolation = renderInterpolation;
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

        /// <summary>
        /// Applies the current tessellation.
        /// </summary>
        private void ApplyTessellation()
        {
            // Save as last updated
            lastTessellation = tessellation;

            // Convert to render value
            renderTessellation = PercentToRange(TESSELLATION_MIN, TESSELLATION_MAX, tessellation);

            // Make sure we don't actually go all the way down to zero since technically the shader doesn't support it
            renderTessellation = Mathf.Clamp(renderTessellation, 0.1f, TESSELLATION_MAX);

            // Update the displacement shader tessellation factor
            projectorMaterial.SetFloat("_TessFactor", renderTessellation);
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
            if (tessellation != lastTessellation) { ApplyTessellation(); }
            if (interpolation != lastInterpolation) { ApplyInterpolation(); }
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
        /// Gets or sets which part of the projector is currently focused. 0.5 is middle.
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
        /// Gets or sets which the amount of interpolation used by Holoplay.
        /// </summary>
        /// <remarks>
        /// Interpolation determines how many camera angles are estimated rather than fully calculated as Holoplay
        /// generates frames for the Looking Glass. The default value of 0 produces the highest quality picture
        /// possible, but is very demanding on the GPU. This slider will cause a dramatic boost in FPS, but it will
        /// also cause a dramatic decrease in picture quality.
        /// </remarks>
        /// <seealso href="https://docs.lookingglassfactory.com/developer-tools/unity/scripts/holoplay#viewinterpolation"/>
        public float Interpolation
        {
            get => interpolation;
            set
            {
                interpolation = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the amount of tessellation used by the shader.
        /// </summary>
        /// <remarks>
        /// Tessellation determines the number of 3D points that are generated from the depth map. Higher tessellation
        /// values represent curves more accurately, but at the cost of performance. The default value of 0.5 should
        /// work well for medium to high-end gaming PCs.
        /// </remarks>
        public float Tessellation
        {
            get => tessellation;
            set
            {
                tessellation = Mathf.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the displaced and colored virtual scene projector.
        /// </summary>
        public MeshRenderer Projector { get => projector; set => projector = value; }
        #endregion // Public Properties
    }
}