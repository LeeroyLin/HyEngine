namespace Engine.Scripts.Runtime.FSM
{
    public interface IFSMParameterOp
    {
        void SetIntParameter(string name, int val, int fromStateId = -1);
        void SetFloatParameter(string name, float val, int fromStateId = -1);
        void SetBoolParameter(string name, bool val, int fromStateId = -1);
        FSMParameter GetParameter(string name);
    }
}