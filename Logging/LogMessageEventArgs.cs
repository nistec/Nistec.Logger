using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nistec.Logging
{
    #region  Log delegate

    /// <summary>
    /// Log Message EventHandler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void LogMessageEventHandler(object sender, LogMessageEventArgs e);

    /// <summary>
    /// Represent log message event arguments.
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        // Fields
        private string message;
        private IAsyncResult result;
        private Logger sender;

        // Methods
        internal LogMessageEventArgs(Logger sender, IAsyncResult result)
        {
            this.result = result;
            this.sender = sender;
        }

        /// <summary>
        /// Get message.
        /// </summary>
        public string Message
        {
            get
            {
                if (this.message == null)
                {
                    try
                    {
                        this.message = this.sender.EndLog(this.result);
                    }
                    catch
                    {
                        throw;
                    }
                }
                return this.message;
            }
        }
    }

    #endregion
}
