namespace Engine.Scripts.Runtime.View
{
    public interface IView
    {
        void Init();
        
        void Open(ViewArgsBase args = null);
        
        void Close();
        
        void Dispose();
    }
}