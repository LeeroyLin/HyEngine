using FairyGUI;

namespace Engine.Scripts.Runtime.View
{
    public class ViewExtensionData
    {
        public string Url { get; private set; }
        public UIObjectFactory.GComponentCreator Creator { get; private set; }

        public ViewExtensionData(string url, UIObjectFactory.GComponentCreator creator)
        {
            Url = url;
            Creator = creator;
        }
    }
}