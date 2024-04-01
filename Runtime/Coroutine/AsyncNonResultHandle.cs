
namespace PluginLit.Core
{
    public class AsyncNonResultHandle<T>: AsyncOperationHandle<T>
    {
        private static AsyncNonResultHandle<T> _default;
        public static AsyncNonResultHandle<T> Default => _default ?? (_default = Create(default(T)));

        public static AsyncNonResultHandle<T> Create(T nonValue)
        {
            var handle = new AsyncNonResultHandle<T>();
            handle.EndWith(nonValue);
            return handle;
        }

        public override float progress => 1f;

        private void EndWith(T nonValue)
        {
            isDone = true;
            result = nonValue;
            InvokeCompletionEvent();
        }
    }
}