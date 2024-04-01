using System;
using System.Collections;
using UnityEngine;

namespace PluginLit.Core
{
    public class AsyncMonoCoroutineHandle<T>: AsyncOperationHandle<T>
    {
        public static AsyncMonoCoroutineHandle<T> Create(MonoBehaviour behaviour, Func<Action<T>, IEnumerator> iter)
        {
            var handle = new AsyncMonoCoroutineHandle<T>();
            handle.Start(behaviour, iter);
            return handle;
        }

        public override float progress => isDone ? 1 : 0;

        private void Start(MonoBehaviour behaviour, Func<Action<T>, IEnumerator> iter)
        {
            behaviour.StartCoroutine(iter.Invoke(OnCompleted));
        }

        private void OnCompleted(T ret)
        {
            isDone = true;
            result = ret;
            InvokeCompletionEvent();
        }
    }
}