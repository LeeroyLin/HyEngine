using UnityEngine;

namespace EngineRuntimeAOT
{
    public interface IEntrance
    {
        void DoStart(MonoBehaviour behaviour, params object[] args);
        void DoUpdate();
        void DoDispose();
        void DoLateUpdate();
        void DoFixedUpdate();
    }
}