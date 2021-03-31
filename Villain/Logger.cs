using System;
using MelonLoader;

namespace Villain
{
    internal class AssertionFailedException : Exception
    {
        public AssertionFailedException(string message): base(message) { }
    }

    internal static class AssertionMessage
    {
        public static string Create(string assertion)
        {
            return $"Assertion (\"{assertion}\") just failed. It might be caused by game update or pragram bugs, please consider contacting me.";
        }
    }

    internal static class Logger
    {
        /// <Summary>
        /// Output a normal massage
        /// </Summary>
        public static void Info(string s)
        {
            MelonLogger.Msg(s);
        }

        /// <Summary>
        /// Output a debug massage if <see cref="MelonDebug"/> is enabled
        /// </Summary>
        public static void Debug(string s)
        {
            MelonDebug.Msg(s);
        }

        /// <summary>
        /// Same as <see cref="Debug(string)"/> but do nothing when <see cref="MelonDebug"/> is disabled.
        /// </summary>
        /// <param name="f">A function generate the debug message</param>
        public static void Debug(Func<string> f)
        {
            if (MelonDebug.IsEnabled())
            {
                MelonDebug.Msg(f());
            }
        }

        /// <summary>
        /// Output a warning message
        /// </summary>
        public static void Warn(string s)
        {
            MelonLogger.Warning(s);
        }

        /// <summary>
        /// Output a error message
        /// </summary>
        public static void Error(string s)
        {
            MelonLogger.Error(s);
        }

        /// <summary>
        /// Checks for a <see cref="cond"/>; if the condition is <see langword="false" />, calls <see cref="Error"/> with <see cref="assertion"/> and throw an <see cref="AssertionFailedException"/>.
        /// </summary>
        /// <param name="cond"></param>
        /// <param name="assertion"></param>
        public static void Assert(bool cond, string assertion)
        {
            if (cond) return;

            var message = AssertionMessage.Create(assertion);

            Error(message);
            throw new AssertionFailedException(message);
        }

        /// <summary>
        /// Same as <see cref="Assert(bool, string)"/> but does not construct a useless string when <see cref="cond"/> is <see langword="true"></see>.
        /// </summary>
        /// <param name="cond"></param>
        /// <param name="f"></param>
        public static void Assert(bool cond, Func<string> f)
        {
            if (cond) return;

            var message = AssertionMessage.Create(f());
            
            Error(message);
            throw new AssertionFailedException(message);
        }
    }
}