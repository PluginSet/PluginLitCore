using System;
using System.Collections.Generic;

namespace PluginLit.Core
{
    public class EventDispatcher<T>
    {
        private Dictionary<string, EventBridge<T>> _bridges;

        public void AddEventListener(string eventName, Action callback)
        {
            var bridge = TryGetOrNewBridge(eventName);
            bridge.Add(callback);
        }
        
        public void AddEventListener(string eventName, Action<T> callback)
        {
            var bridge = TryGetOrNewBridge(eventName);
            bridge.Add(callback);
        }

        public void RemoveEventListener(string eventName, Action callback)
        {
            TryGetBridge(eventName)?.Remove(callback);
        }
        

        public void RemoveEventListener(string eventName, Action<T> callback)
        {
            TryGetBridge(eventName)?.Remove(callback);
        }

        public void RemoveEventListeners(string eventName)
        {
            TryGetBridge(eventName)?.Clear();
        }
        
        public void RemoveEventListeners()
        {
            if (_bridges == null)
                return;

            foreach (var bridge in _bridges.Values)
            {
                bridge.Clear();
            }
        }

        public bool HasEventListener(string eventName)
        {
            var bridge = TryGetBridge(eventName);
            if (bridge == null)
                return false;

            return !bridge.IsEmpty;
        }

        public bool DispatchEvent(string eventName, T param)
        {
            var bridge = TryGetBridge(eventName);
            if (bridge == null)
                return false;
            
            bridge.InvokeAll(param);
            return true;
        }

        public bool DispatchOne(string eventName, T param)
        {
            var bridge = TryGetBridge(eventName);
            if (bridge == null)
                return false;
            
            return bridge.InvokeOne(param);
        }

        private EventBridge<T> TryGetBridge(string eventName)
        {
            if (_bridges == null)
                return null;
            
            if (_bridges.TryGetValue(eventName, out var bridge))
                return bridge;

            return null;
        }

        private EventBridge<T> TryGetOrNewBridge(string eventName)
        {
            if (_bridges == null)
                _bridges = new Dictionary<string, EventBridge<T>>();

            if (_bridges.TryGetValue(eventName, out var bridge))
                return bridge;
            
            bridge = new EventBridge<T>();
            _bridges.Add(eventName, bridge);

            return bridge;
        }
    }
}