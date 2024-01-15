using UnityEngine;

namespace EngineRuntimeAOT
{
    public interface IEntrance
    {
        void DoStart(MonoBehaviour behaviour);
        void DoUpdate();
        void DoDispose();
        void DoLateUpdate();
        void DoFixedUpdate();
    }
}