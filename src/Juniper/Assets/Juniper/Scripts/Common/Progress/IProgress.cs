using UnityEngine;

namespace Juniper.Progress
{
    public interface IProgress
    {
        float Progress
        {
            get;
        }
    }

    public static class IProgressExt
    {
        public static bool IsComplete(this IProgress prog)
        {
            return Mathf.Approximately(prog.Progress, 1);
        }
    }
}
