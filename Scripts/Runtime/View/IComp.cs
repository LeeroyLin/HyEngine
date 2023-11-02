namespace Engine.Scripts.Runtime.View
{
    public interface IComp
    {
        void DoInit();
        
        void DoClose();
        
        void DoDispose();
    }
}