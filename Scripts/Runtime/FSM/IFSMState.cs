namespace Engine.Scripts.Runtime.FSM
{
    public interface IFSMState
    {
        int GetId();
        void DoEnter();
        void DoExit();
        void DoUpdate();
        void DoLateUpdate();
        void DoFixedUpdate();
        void OnParameterChanged(string parameterName);
    }
}