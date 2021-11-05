using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    public class DisplayCapture : MonoBehaviour
    {
        #region Member Variables
        private Texture2D colorMap;
        private Texture2D heightMap;
        private Color32[] pixels;
        private int width;
        private int height;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The mesh where displacement should be applied.")]
        private MeshRenderer projector;

        [SerializeField]
        [Tooltip("The UDD capture source.")]
        private uDesktopDuplication.Texture uddSource;
        #endregion // Unity Inspector Variables

        /// <summary>
        /// If the resolution has changed, recreate textures.
        /// </summary>
        private void CreateTexturesIfNeeded()
        {
            // Create pixel array
            if (pixels == null || pixels.Length != width * height)
            {
                pixels = new Color32[width * height];
            }

            // Create height texture
            if (heightMap == null || heightMap.width != width || heightMap.height != height)
            {
                // Create the hight map
                heightMap = new Texture2D(width, height, TextureFormat.ARGB32, false);

                // Update mesh shader
                projector.sharedMaterial.SetTexture("_DispTex", heightMap);

                // Scale the mesh to match the aspect ratio of the new texture
                projector.gameObject.transform.localScale = new Vector3(1, 1, (float)height / (float)width);
            }
        }

        /// <summary>
        /// Scales the height of the projector to maintain the proper aspect ratio of the screen.
        /// </summary>
        private void UpdateScale()
        {

        }



        //// Start is called before the first frame update
        //void Start()
        //{

        //}

        // Update is called once per frame
        void Update()
        {
            // must be called (performance will be slightly down).
            uDesktopDuplication.Manager.primary.useGetPixels = true;

            // Get the monitor
            var monitor = uddSource.monitor;

            // If there's no monitor the projector is probably hidden, skip the rest of processing
            if (monitor == null) { return; }

            // If no updates since last frame, skip the rest of processing
            if (!monitor.hasBeenUpdated) { return; }

            // Get current dimensions of monitor
            width = monitor.width / 2;
            height = monitor.height;

            // Create or recreate textures as needed
            CreateTexturesIfNeeded();

            // Get height map
            if (monitor.GetPixels(pixels, width, 0, width, height))
            {
                heightMap.SetPixels32(pixels);
                heightMap.Apply();
            }
        }
    }
}