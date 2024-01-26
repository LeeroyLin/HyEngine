namespace Engine.Scripts.Runtime.FSM
{
    public class FSMParameter
    {
        public string Name { get; private set; }
        public EFSMParameterType Type { get; private set; }
        public int ValInt { get; set; }
        public float ValFloat { get; set; }
        public bool ValBool { get; set; }

        public FSMParameter(string name, int initVal)
        {
            Name = name;
            Type = EFSMParameterType.Int;
            ValInt = initVal;
        }

        public FSMParameter(string name, float initVal)
        {
            Name = name;
            Type = EFSMParameterType.Float;
            ValFloat = initVal;
        }

        public FSMParameter(string name, bool initVal)
        {
            Name = name;
            Type = EFSMParameterType.Bool;
            ValBool = initVal;
        }
    }
}