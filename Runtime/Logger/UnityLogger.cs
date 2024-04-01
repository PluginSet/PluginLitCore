
namespace PluginLit.Core
{
    public class UnityLogger: Logger
    {
        protected override void DoLogDebug(string msg, params object[] args)
        {
            UnityEngine.Debug.LogFormat(msg, args);
        }

        protected override void DoLogWarn(string msg, params object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(msg, args);
        }

        protected override void DoLogError(string msg, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(msg, args);
        }

        protected override void DoLogInfo(string msg, params object[] args)
        {
            UnityEngine.Debug.LogFormat(msg, args);
        }

        protected override void DoLogDebug(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }

        protected override void DoLogWarn(string msg)
        {
            UnityEngine.Debug.LogWarning(msg);
        }

        protected override void DoLogError(string msg)
        {
            UnityEngine.Debug.LogError(msg);
        }

        protected override void DoLogInfo(string msg)
        {
            UnityEngine.Debug.Log(msg);
        }
    }
}