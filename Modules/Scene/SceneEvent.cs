using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scene
{
    public class SceneEvent : EventArgs
    {
        public string Scene;

        public SceneEvent(string scene)
        {
            Scene = scene;
        }
    }
}
