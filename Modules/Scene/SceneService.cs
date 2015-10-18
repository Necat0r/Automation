using Module;
using System;

namespace Scene
{
    public class SceneService : ServiceBase
    {
        public event EventHandler<SceneEvent> OnSceneEvent;

        public SceneService(string name, ServiceCreationInfo info)
            : base("scene", info)
        {
        }

        [ServicePutContract("{scene}")]
        public void OnSceneRequest(dynamic parameters)
        {
            string scene = parameters.scene;

            Console.WriteLine("Scene requested: " + scene);
            if (OnSceneEvent != null)
                OnSceneEvent(this, new SceneEvent(scene));
        }
    }
}
