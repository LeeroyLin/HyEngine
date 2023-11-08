namespace Engine.Scripts.Runtime.View
{
    public interface IView
    {
        void DoOpen(ViewArgsBase args = null);
        
        void DoClose();
        
        void DoDispose();
    }
}