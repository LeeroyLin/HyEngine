using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Engine.Scripts.Runtime.Utils
{
    public static class AsyncEx
    {
        /// <summary>
        /// await异步操作
        /// </summary>
        /// <param name="asyncOp"></param>
        /// <returns></returns>
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<object>();
            asyncOp.completed += obj => { tcs.SetResult(null); };
            return ((Task) tcs.Task).GetAwaiter();
        }
    }
}