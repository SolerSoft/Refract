using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refract
{
    [Serializable]
    public class RefractSettings
    {
        private bool showSceneInMenu;
        private float depthiness = 0.5f;
        private float focus = 0.5f;
        private float tessellation = 0.5f;
        private float interpolation = 0;
    }
}
