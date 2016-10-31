using Logging;
using Module;
using System;

namespace Scene
{
    public class SceneService : ServiceBase
    {
        public event EventHandler<SceneEvent> OnSceneEvent;

        public SceneService(ServiceCreationInfo info)
            : base("scene", info)
        {
        }

        [ServicePutContract("{scene}")]
        public void OnSceneRequest(string scene)
        {
            Log.Info("Scene requested: " + scene);
            if (OnSceneEvent != null)
                OnSceneEvent(this, new SceneEvent(scene));
        }
    }
}
