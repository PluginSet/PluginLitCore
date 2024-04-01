using System;
using UnityEngine;

namespace PluginLit.Core
{
    public class EventBridge<T>
    {
        protected event Action Callback0;
        protected event Action<T> Callback1;

        public bool IsEmpty => Callback0 == null && Callback1 == null;

        public void Add(Action callback)
        {
            Callback0 -= callback;
            Callback0 += callback;
        }

        public void Remove(Action callback)
        {
            Callback0 -= callback;
        }

        public void Add(Action<T> callback)
        {
            Callback1 -= callback;
            Callback1 += callback;
        }

        public void Remove(Action<T> callback)
        {
            Callback1 -= callback;
        }

        public void Clear()
        {
            Callback0 = null;
            Callback1 = null;
        }

        public void InvokeAll(T param)
        {
            try
            {
                Callback0?.Invoke();
                Callback1?.Invoke(param);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public bool InvokeOne(T param)
        {
            if (InvokeOne(Callback1, param))
                return true;

            return InvokeOne(Callback0);
        }

        protected bool InvokeOne(Delegate del, params object[] args)
        {
            if (del == null)
                return false;

            try
            {
                var delegates = del.GetInvocationList();
                delegates[0].DynamicInvoke(args);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return true;
        }
        
    }
}