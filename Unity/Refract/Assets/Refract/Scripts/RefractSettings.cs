using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refract
{
    /// <summary>
    /// The settings that will be saved to disk and reloaded between application runs.
    /// </summary>
    [Serializable]
    public class RefractSettings
    {
        public float Depthiness;
        public float Focus;
        public float Interpolation;
        public bool ShowSceneInMenu;
        public float Tessellation;
    }
}
