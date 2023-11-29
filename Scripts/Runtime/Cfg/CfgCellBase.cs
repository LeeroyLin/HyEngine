namespace Engine.Scripts.Runtime.Cfg
{
    public abstract class CfgCellBase<K>
    {
        public K Id { get; protected set; }

        public string GetI18nVal(string key)
        {
            return CfgMgr.Ins.GetI18nVal(key);
        }
        
        public string[] GetI18nValArr(string[] keyArr)
        {
            var arr = new string[keyArr.Length];

            for (int i = 0; i < keyArr.Length; i++)
            {
                arr[i] = CfgMgr.Ins.GetI18nVal(keyArr[i]);
            }
            return arr;
        }
        
        public string[][] GetI18nValArr2(string[][] keyArr)
        {
            var arr = new string[keyArr.Length][];

            for (int i = 0; i < keyArr.Length; i++)
            {
                var kArr = keyArr[i];
                
                for (int t = 0; t < kArr.Length; t++)
                {
                    arr[i][t] = CfgMgr.Ins.GetI18nVal(keyArr[i][t]);
                }
            }
            return arr;
        }
    }
}