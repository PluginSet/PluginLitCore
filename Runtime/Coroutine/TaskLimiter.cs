using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PluginLit.Core
{
    public interface IAsyncOperationTask
    {
        AsyncOperationHandle Start();
    }


    public interface IStopableAsyncOperationTask : IAsyncOperationTask
    {
        void Stop();
    }

    public class TaskLimiter : CustomYieldInstruction
    {
        private int _maxTaskCount;
        private int _currentTaskCount;
        private int _totalTasksPushed;

        private bool _running;
        private bool _isPause;

        public override bool keepWaiting => _running;
        public int TaskCount => taskQueue.Count;

        public event Action<int, int> OnTaskProgressUpdate;

        public bool IsPause
        {
            get => _isPause;

            set
            {
                _isPause = value;
                if (!_isPause)
                    NextTask();
            }
        }

        public IEnumerable<IAsyncOperationTask> Iter => taskQueue;

        private Queue<IAsyncOperationTask> taskQueue = new Queue<IAsyncOperationTask>();

        public TaskLimiter(int limit)
        {
            _maxTaskCount = limit;
            _currentTaskCount = 0;
            _totalTasksPushed = 0;

            _running = false;
        }

        public void PushTask(IAsyncOperationTask task, bool autoStart = true)
        {
            taskQueue.Enqueue(task);
            _totalTasksPushed++;
            if (!_isPause && autoStart && _currentTaskCount < _maxTaskCount)
                NextTask();
        }

        public IEnumerator Start()
        {
            _running = true;
            if (!_isPause && _currentTaskCount < _maxTaskCount)
                NextTask();
            yield return this;
        }

        protected int NextTask()
        {
            if (taskQueue.Count <= 0)
            {
                _running = _currentTaskCount > 0;
                return 0;
            }

            _running = true;
            var task = taskQueue.Dequeue();
            var operation = task.Start();
            OnTaskProgressUpdate?.Invoke(_totalTasksPushed - taskQueue.Count, _totalTasksPushed);
            if (operation != null)
            {
                _currentTaskCount++;
                operation.completed += OnTaskComplete;
                return 1;
            }

            return NextTask();
        }

        protected void OnTaskComplete(AsyncOperationHandle operation)
        {
            operation.completed -= OnTaskComplete;
            _currentTaskCount--;
            if (!_isPause)
                NextTask();
        }

        public void Clear()
        {
            foreach (var task in taskQueue)
            {
                if (task is IStopableAsyncOperationTask t)
                    t.Stop();
            }

            taskQueue.Clear();
        }
    }
}
