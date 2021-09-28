using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    static public class TextureExtensions
    {
        /// <summary>
        /// Splits a texture, creating two new textures of the same format.
        /// </summary>
        /// <param name="input">
        /// The input texture to split.
        /// </param>
        /// <param name="first">
        /// The first half of the split.
        /// </param>
        /// <param name="second">
        /// The second half of the split.
        /// </param>
        /// <param name="horizontal">
        /// Whether to split the texture horizontally or vertically.
        /// </param>
        static public void SplitCopy(this Texture2D input, ref Texture2D first, ref Texture2D second, bool horizontal = true)
        {
            // Get the split dimensions from the input texture
            int width = (horizontal ? input.width / 2 : input.width);
            int height = (horizontal ? input.height : input.height / 2);

            // Determine where the second half starts
            int x2 = (horizontal ? width : 0);
            int y2 = (horizontal ? 0 : height);

            // Create textures if missing
            if (first == null) { first = new Texture2D(width, height, input.format, input.mipmapCount > 1); }
            if (second == null) { second = new Texture2D(width, height, input.format, input.mipmapCount > 1); }

            // Copy
            // Graphics.CopyTexture(input, 0, 0, 0, 0, width, height, first, 0, 0, width, height);
            // Graphics.CopyTexture(input, 0, 0, x2, y2, width, height, second, 0, 0, width, height);
            if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
            {
                //High GC allocs here
                Color[] pixelBuffer = input.GetPixels(0, 0, width, height);
                first.SetPixels(pixelBuffer);

                pixelBuffer = input.GetPixels(x2, y2, width, height);
                second.SetPixels(pixelBuffer);
            }
            else
            {
                Graphics.CopyTexture(input, 0, 0, 0, 0, width, height, first, 0, 0, 0, 0);
                Graphics.CopyTexture(input, 0, 0, x2, y2, width, height, second, 0, 0, 0, 0);
            }
        }
    }
}