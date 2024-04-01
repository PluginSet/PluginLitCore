using System;
using System.Linq;
using UnityEngine;

namespace PluginLit.Core
{
    public class AsyncOperationHandle: CustomYieldInstruction
    {
        public static AsyncOperationHandle Create(AsyncOperation operation)
        {
            var handler = new AsyncOperationHandle();
            operation.completed += handler.OnAsyncOperationCompleted;
            return handler;
        }
            
        private bool _isDone = false;
        private bool _keepWaiting = true;
        
        public override bool keepWaiting => _keepWaiting;
        
        protected object _result { get; set; }

        public bool isDone
        {
            get => _isDone;
            
            protected set
            {
                _isDone = value;
                _keepWaiting = !value;
            }
        }

        public virtual float progress { get; }

        public event Action<AsyncOperationHandle> completed
        {
            add
            {
                if (isDone)
                    value(this);
                else
                    m_completeCallback += value;
            }
            remove
            {
                m_completeCallback -= value;
            }
        }

        private Action<AsyncOperationHandle> m_completeCallback;

        protected virtual void InvokeCompletionEvent()
        {
            if (m_completeCallback == null)
                return;
            m_completeCallback(this);
            m_completeCallback = null;
        }

        private void OnAsyncOperationCompleted(AsyncOperation operation)
        {
            operation.completed -= OnAsyncOperationCompleted;
            isDone = true;
            _result = operation;
            InvokeCompletionEvent();
        }
    }

    public class MultiAsyncOperations : AsyncOperationHandle
    {
        private readonly AsyncOperationHandle[] _handles;
        public MultiAsyncOperations(params AsyncOperationHandle[] handles)
        {
            _handles = handles;
            foreach (var handle in handles)
            {
                handle.completed += CheckDone;
            }
            CheckDone();
        }

        private void CheckDone(AsyncOperationHandle current)
        {
            current.completed -= CheckDone;
            CheckDone();
        }

        private void CheckDone()
        {
            if (_handles.Any(handle => !handle.isDone))
            {
                return;
            }

            _result = null;
            isDone = true;
            InvokeCompletionEvent();
        }
    }

    public class AsyncOperationHandle<T> : AsyncOperationHandle
    {
        public T result
        {
            get => (T) _result;
            set => _result = value;
        }
        
        public event Action<T> OnGetResult
        {
            add
            {
                if (isDone)
                    value(result);
                else
                    m_onGetResultCallback += value;
            }
            remove
            {
                m_onGetResultCallback -= value;
            }
        }

        private Action<T> m_onGetResultCallback;
        
        protected override void InvokeCompletionEvent()
        {
            base.InvokeCompletionEvent();
            
            if (m_onGetResultCallback == null)
                return;
            m_onGetResultCallback(result);
            m_onGetResultCallback = null;
        }
    }
}