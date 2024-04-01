using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.Experimental;
using UnityEngine;

namespace PluginLit.Core.Editor
{
    public class BuildTaskHandler
    {
        private List<BuildProcessorTask> _tasks = new List<BuildProcessorTask>();
        private int _currentTaskIndex = 0;
        private BuildProcessorContext _context;
        private long _startTime;
        
        public BuildProcessorTask AddNextTask(BuildProcessorTask task, BuildProcessorTask place = null)
        {
            if (place == null)
            {
                _tasks.Add(task);
            }
            else
            {
                var index = _tasks.IndexOf(place);
                if (index < 0)
                    throw new Exception($"Cannot find task with {place}");
                _tasks.Insert(index + 1, task);
            }

            task.Handler = this;
            return task;
        }

        public BuildProcessorTask AddTaskAfter(BuildProcessorTask task, int index = -1)
        {
            if (index < 0)
                index = _currentTaskIndex - 1;
            
            _tasks.Insert(index + 1, task);
            
            task.Handler = this;
            return task;
        }

        public void Execute(BuildProcessorContext context)
        {
            _context = context;
            _currentTaskIndex = 0;
            
            ExecuteNext();
        }

        private void OnBuildStopWaiting()
        {
            _context.OnBuildStopWaiting -= OnBuildStopWaiting;
            Debug.Log($"Task async ended <<<<<<<<<<<<<<<<<<< used time: {(DateTime.Now.Ticks - _startTime)/10000000}s");
            ExecuteNext();
        }
        
        private int WaitBuildStop()
        {
            _context.OnBuildStopWaiting += OnBuildStopWaiting;
            return 3;
        }
        
#if UNITY_2020_1_OR_NEWER
        private void OnCompilationFinished(object any)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            ExecuteNext();
        }
#else
        private void OnCompilationFinished(string any, CompilerMessage[] msg)
        {
            CompilationPipeline.assemblyCompilationFinished -= OnCompilationFinished;
            ExecuteNext();
        }
#endif

        private int WaitCompiling()
        {
#if UNITY_2020_1_OR_NEWER
            CompilationPipeline.compilationFinished += OnCompilationFinished;
#else
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
#endif
            return 1;
        }

        private void OnImportPackageItemsCompleted(string any)
        {
            if (EditorApplication.isUpdating)
                return;
            
            AssetDatabase.importPackageCompleted -= OnImportPackageItemsCompleted;
            ExecuteNext();
        }

        private int WaitUpdating()
        {
            AssetDatabase.importPackageCompleted += OnImportPackageItemsCompleted;
            return 2;
        }

        private int ExecuteNext()
        {
            if (_context == null)
                return -1;

            if (_currentTaskIndex >= _tasks.Count)
            {
                return Completed();
            }

            if (EditorApplication.isCompiling)
            {
                Debug.Log("execute task WaitCompiling");
                return WaitCompiling();
            }

            if (EditorApplication.isUpdating)
            {
                Debug.Log("execute task isUpdating");
                return WaitUpdating();
            }
            
            if (_context.IsWaiting)
            {
                Debug.Log("Build context isWaiting");
                return WaitBuildStop();
            }

            var task = _tasks[_currentTaskIndex++];
            _startTime = DateTime.Now.Ticks;
            Debug.Log($"Task {task.GetType().Name} begin >>>>>>>>>>>>>>>>>>>");
            task.Execute(_context);
            if (_context.IsWaiting)
            {
                return WaitBuildStop();
            }
            Debug.Log($"Task {task.GetType().Name} ended <<<<<<<<<<<<<<<<<<< used time: {(DateTime.Now.Ticks - _startTime)/10000000}s");

            return ExecuteNext();
        }

        private int Completed()
        {
            if (_context == null)
                return -1;
            
            _context = null;
            
            return 0;
        }
    }
}