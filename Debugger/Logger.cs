using System;
using UnityEngine;

namespace ModTools
{
    internal static class Logger
    {
        private static readonly object SyncObject = new object();
        private static ILogger customLogger;

        public static void SetCustomLogger(ILogger logger)
        {
            lock (SyncObject)
            {
                customLogger = logger;
            }
        }

        public static void Message(string s)
        {
            ILogger logger;
            lock (SyncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Log);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, s);
            }
        }

        public static void Error(string s)
        {
            ILogger logger;
            lock (SyncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Error);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Error, s);
            }

            Debug.LogError(s);
        }

        public static void Warning(string s)
        {
            ILogger logger;
            lock (SyncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(s, LogType.Warning);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Warning, s);
            }
        }

        public static void Exception(Exception ex)
        {
            ILogger logger;
            lock (SyncObject)
            {
                logger = customLogger;
            }

            if (logger != null)
            {
                logger.Log(ex.ToString(), LogType.Exception);
            }
            else
            {
                DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Error, ex.ToString());
            }

            Debug.LogException(ex);
        }
    }
}