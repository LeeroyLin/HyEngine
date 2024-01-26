namespace Engine.Scripts.Runtime.FSM
{
    public interface IFSM
    {
        void AddParameter(string name, FSMParameter param);
        void RemoveParameter(string name, FSMParameter param);
        void ClearParameters();
        
        void DoUpdate();
        void DoLateUpdate();
        void DoFixedUpdate();
    }
}