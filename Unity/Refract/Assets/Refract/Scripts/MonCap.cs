using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonCap : MonoBehaviour
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
    [Tooltip("The material where the captured textures will be set.")]
    private Material material;

    [SerializeField]
    [Tooltip("The desktop application texture.")]
    private uDesktopDuplication.Texture uddTexture;
    #endregion // Unity Inspector Variables

    private void CreateTexturesIfNeeded()
    {
        // Create pixel array
        if (pixels == null || pixels.Length != w * h)
        {
            pixels = new Color32[w * h];
        }

        // Create color texture
        if (color == null || color.width != w || color.height != h)
        {
            color = new Texture2D(w, h, TextureFormat.ARGB32, false);
        }

        // Create height texture
        if (height == null || height.width != w || height.height != h)
        {
            height = new Texture2D(w, h, TextureFormat.ARGB32, false);
        }

        // Update shader
        material.SetTexture("_MainTex", color);
        material.SetTexture("_ParallaxMap", height);
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

        var monitor = uddTexture.monitor;
        if (!monitor.hasBeenUpdated) return;

        // Get dimensions
        w = monitor.width / 2;
        h = monitor.height;

        CreateTexturesIfNeeded();

        // Get height
        if (monitor.GetPixels(pixels, 0, 0, w, h))
        {
            height.SetPixels32(pixels);
            height.Apply();
        }

        // Get color
        if (monitor.GetPixels(pixels, w, 0, w, h))
        {
            color.SetPixels32(pixels);
            color.Apply();
        }
    }
}
