using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public abstract class ViewBase : IView
    {
        public int Key { get; private set; }
        public string Pkg { get; private set; }
        public string Name { get; private set; }
        public GComponent Node { get; set; }
        
        public ViewBase(int key, string pkg, string name)
        {
            Pkg = pkg;
            Name = name;
            Key = key;
        }

        public void Init()
        {
            OnInit();
        }

        public void Open(ViewArgsBase args = null)
        {
            OnOpen(args);
        }

        public void Close()
        {
            OnClose();
        }

        public void Dispose()
        {
            OnDispose();
        }

        protected abstract void OnInit();
        
        protected abstract void OnOpen(ViewArgsBase args = null);
        
        protected abstract void OnClose();
        
        protected abstract void OnDispose();
    }
}