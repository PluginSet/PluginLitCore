using System.Collections;
using UnityEngine;

namespace PluginLit.Core
{
    public class EnumeratorLocker: CustomYieldInstruction
    {
        private bool _keepWaiting = false;
        public override bool keepWaiting => _keepWaiting;
        
        public bool Locked => _keepWaiting;

        public void Lock()
        {
            _keepWaiting = true;
        }

        public void Unlock()
        {
            _keepWaiting = false;
        }

        public void UnlockDelay(float seconds)
        {
            CoroutineHelper.Instance.StartCoroutine(UnlockDelayInternal(seconds));
        }

        private IEnumerator UnlockDelayInternal(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Unlock();
        }
    }
}