namespace Engine.Scripts.Runtime.Event
{
    struct AsyncInfo
    {
        public EEventGroup Group { get; private set; }
        public string Key { get; private set; }
        public IEventData Data { get; private set; }
        
        public AsyncInfo(EEventGroup group, string key, IEventData data)
        {
            Group = group;
            Key = key;
            Data = data;
        } 
    }
}