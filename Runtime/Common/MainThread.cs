using System;
using System.Collections.Generic;
using UnityEngine;

namespace PluginLit.Core
{
    public class MainThread: MonoBehaviour
    {
        private static Logger Logger = LoggerManager.GetLogger("MainThread");
        
        private static MainThread _instance;

        private static MainThread CreateInstance()
        {
            var gameObject = new GameObject("MainThread");
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<MainThread>();
        }

        private static MainThread Instance
        {
            get
            {
                if (_instance == null)
                    _instance = CreateInstance();

                return _instance;
            }
        }

        public static MainThread Init()
        {
            return Instance;
        }

        public static void Run(Action action)
        {
            if (action == null)
                return;
            
            Instance.AddAction(action);
        }

        public static void Once(Action action)
        {
            if (action == null)
                return;
            
            Instance.AddAction(action, true);
        }

        private readonly List<Action> _actions = new List<Action>();
        private bool _isDispose = false;

        private void AddAction(Action action, bool once = false)
        {
            if (_isDispose) return;
            
#if !UNITY_WEBGL
            lock (_actions)
            {
#endif
                if (once)
                    _actions.Remove(action);
                    
                _actions.Add(action);
#if !UNITY_WEBGL
            }
#endif
        }

        private void Update()
        {
            Action[] actions;
#if !UNITY_WEBGL
                lock (_actions)
                {
#endif
                    actions = _actions.ToArray();
                    _actions.Clear();
#if !UNITY_WEBGL
                }
#endif

            foreach (var action in actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Action invoke error {e.Message}, {e}");
                }
            }
        }

        private void OnDestroy()
        {
            _isDispose = true;
#if !UNITY_WEBGL
            lock (_actions)
            {
#endif
                _actions.Clear();
#if !UNITY_WEBGL
            }
#endif
        }
    }
}