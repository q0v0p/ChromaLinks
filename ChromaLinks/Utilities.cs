using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using System;
using System.IO;

namespace ChromaControl.Shared
{
    /// <summary>
    /// The utilities class
    /// </summary>
    public class Utilities
    {
        /// <summary>
        /// Allocates a console window
        /// </summary>
        [DllImport("kernel32")]
        private static extern void AllocConsole();

        /// <summary>
        /// The application id
        /// </summary>
        internal static string ApplicationId = "Default";

        /// <summary>
        /// The application guid
        /// </summary>
        internal static string ApplicationGuid = "";

        /// <summary>
        /// Initializes the environment
        /// </summary>
        /// <param name="applicationId">The application id</param>
        /// <param name="applicationGuid">The application id</param>
        /// <param name="args">The command line arguments</param>
        /// <returns>If sucessful</returns>
        public static bool InitializeEnvironment(string applicationId, string applicationGuid)
        {
            ApplicationId = applicationId;
            ApplicationGuid = applicationGuid;

            var mutex = new Mutex(true, applicationId, out var result);

            if (!result)
            {
                return false;
            }

            //if (Debugger.IsAttached || args.Length > 0 && args[0] == "--console")
            //{
            //    AllocConsole();
            //}

            return true;
        }
    }
}
