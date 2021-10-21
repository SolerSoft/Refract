//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// A <see cref="MonoBehaviour"/> component that blits quilts and 2D previews to the screen using <see cref="OnRenderImage(RenderTexture, RenderTexture)"/>.
    /// </summary>
    [ExecuteInEditMode]
    public class HoloplayScreenBlitter : MonoBehaviour {
        public Holoplay holoplay;

        public event Action<RenderTexture> onAfterScreenBlit;

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if (holoplay.Preview2D) {
                holoplay.RenderPreview2D();
                Graphics.Blit(holoplay.Preview2DRT, destination);
            } else {
                holoplay.RenderQuilt();
                Graphics.Blit(holoplay.quiltRT, destination, holoplay.lightfieldMat);
            }

            if (onAfterScreenBlit != null) {
                RenderTexture screenTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
                try {
                    Graphics.Blit(destination, screenTexture);
                    onAfterScreenBlit(screenTexture);
				} finally {
                    RenderTexture.ReleaseTemporary(screenTexture);
				}
			}
        }
    }
}
