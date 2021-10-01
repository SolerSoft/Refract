using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    public class DisplayCapture : MonoBehaviour
    {
        #region Member Variables
        private Texture2D color;
        private Texture2D height;
        private Color32[] pixels;
        private int w;
        private int h;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        [Tooltip("The mesh where displacement should be applied.")]
        private MeshRenderer projector;

        [SerializeField]
        [Tooltip("The UDD capture source.")]
        private uDesktopDuplication.Texture uddSource;
        #endregion // Unity Inspector Variables

        private void CreateTexturesIfNeeded()
        {
            // Create pixel array
            if (pixels == null || pixels.Length != w * h)
            {
                pixels = new Color32[w * h];
            }

            // Create height texture
            if (height == null || height.width != w || height.height != h)
            {
                height = new Texture2D(w, h, TextureFormat.ARGB32, false);

                // Update mesh shader
                projector.sharedMaterial.SetTexture("_DispTex", height);
            }
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

            var monitor = uddSource.monitor;
            if (!monitor.hasBeenUpdated) return;

            // Get dimensions
            // w = monitor.width;
            w = monitor.width / 2;
            h = monitor.height;

            CreateTexturesIfNeeded();

            // Get heightmap
            if (monitor.GetPixels(pixels, w, 0, w, h))
            {
                height.SetPixels32(pixels);
                height.Apply();
            }
        }
    }
}