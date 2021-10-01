using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refract
{
    public class TestCap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The material where the captured textures will be set.")]
        private MeshRenderer mesh;

        public Texture2D depth;
        public Texture2D color;


        // Start is called before the first frame update
        void Start()
        {
            Material material = mesh.sharedMaterial;

            material.SetTexture("_MainTex", color);
            material.SetTexture("_ParallaxMap", depth);
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}