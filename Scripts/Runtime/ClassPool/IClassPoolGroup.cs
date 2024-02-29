namespace Engine.Scripts.Runtime.ClassPool
{
    public interface IClassPoolGroup
    {
        void Clear();

        void SetCapacity(int capacity);
        
        int GetCapacity();
    }
}