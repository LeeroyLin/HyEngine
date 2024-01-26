namespace Engine.Scripts.Runtime.FSM
{
    public struct FSMTransition
    {
        public string ParameterName { get; private set; }
        public EFSMConditionType Type { get; private set; }
        public EFSMConditionCompare Compare { get; private set; }
        public int TargetInt { get; private set; }
        public float TargetFloat { get; private set; }
        public bool TargetBool { get; private set; }
        
        public FSMTransition(string parameterName, EFSMConditionCompare compare, int target)
        {
            ParameterName = parameterName;
            Type = EFSMConditionType.Int;
            Compare = compare;
            TargetInt = target;
            TargetFloat = 0;
            TargetBool = false;
        }
        
        public FSMTransition(string parameterName, EFSMConditionCompare compare, float target)
        {
            ParameterName = parameterName;
            Type = EFSMConditionType.Float;
            Compare = compare;
            TargetInt = 0;
            TargetFloat = target;
            TargetBool = false;
        }
        
        public FSMTransition(string parameterName, EFSMConditionCompare compare, bool target)
        {
            ParameterName = parameterName;
            Type = EFSMConditionType.Bool;
            Compare = compare;
            TargetInt = 0;
            TargetFloat = 0;
            TargetBool = target;
        }
    }
}