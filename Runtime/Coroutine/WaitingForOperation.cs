using System;
using System.Collections;
using UnityEngine;
using Object = System.Object;

namespace PluginLit.Core
{
    public class WaitingForOperation: CustomYieldInstruction
    {
        private static ObjectPool<WaitingForOperation> _pool = new ObjectPool<WaitingForOperation>(10);

        public static WaitingForOperation Get(Func<IEnumerator> confirm, Func<IEnumerator> cancel)
        {
            var operation = _pool.Get();
            operation._confirm = confirm;
            operation._cancel = cancel;
            operation.ResetStatus();
            return operation;
        }

        public static void Return(WaitingForOperation operation)
        {
            operation.Clear();
            _pool.Put(operation);
        }
        
        protected bool _running = false;

        public override bool keepWaiting => _running;

        protected Func<IEnumerator> _confirm;
        protected Func<IEnumerator> _cancel;
        protected Func<IEnumerator> _select;
        
        public Action Confirm => delegate
        {
            _select = _confirm;
            Stop();
        };
        
        public Action Cancel => delegate
        {
            _select = _cancel;
            Stop();
        };

        ~WaitingForOperation()
        {
            _cancel = null;
            _confirm = null;
            _select = null;
        }
        
        public IEnumerator Wait()
        {
            yield return this;
            yield return _select?.Invoke();
        }

        protected void ResetStatus()
        {
            _select = null;
            _running = true;
        }

        protected void Stop()
        {
            _running = false;
        }

        protected void Clear()
        {
            _select = null;
            _running = false;
            _confirm = null;
            _cancel = null;
        }
    }
}