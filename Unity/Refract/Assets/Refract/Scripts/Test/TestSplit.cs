using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    public class TestSplit : MonoBehaviour
    {
        #region Member Variables
        protected Texture2D colorTex;
        protected Texture2D depthTex;
        protected Texture2D inputTex;
        #endregion // Member Variables

        #region Unity Inspector Variables
        [SerializeField]
        private MeshRenderer inputMesh;

        [SerializeField]
        private MeshRenderer depthMesh;

        [SerializeField]
        private MeshRenderer colorMesh;
        #endregion // Unity Inspector Variables

        private void CreateTextures()
        {
            // Get input texture
            inputTex = inputMesh.sharedMaterial.mainTexture as Texture2D;

            // Create depth and color textures
            inputTex.SplitCopy(ref depthTex, ref colorTex);
        }

        private void SetAspects()
        {
            bool inputWide = (inputTex.width > inputTex.height);

            if (inputWide)
            {
                inputMesh.transform.localScale = new Vector3(inputTex.width / inputTex.height, 1, 1);
                depthMesh.transform.localScale = new Vector3(inputMesh.transform.localScale.x / 2, 1, 1);
                colorMesh.transform.localScale = depthMesh.transform.localScale;
            }
            else
            {
                inputMesh.transform.localScale = new Vector3(1, inputTex.height / inputTex.width, 1);
                depthMesh.transform.localScale = new Vector3(1, inputMesh.transform.localScale.y / 2, 1);
                colorMesh.transform.localScale = depthMesh.transform.localScale;
            }
        }

        private void SetMaterials()
        {
            depthMesh.material.mainTexture = depthTex;
            colorMesh.material.mainTexture = colorTex;
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            CreateTextures();
            SetMaterials();
            SetAspects();
        }

        // Update is called once per frame
        protected virtual void Update()
        {
        }
    }
}