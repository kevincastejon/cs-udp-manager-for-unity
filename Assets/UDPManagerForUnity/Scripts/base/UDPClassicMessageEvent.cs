using System.Net;
namespace kevincastejon
{
    /// <summary>
    /// An UDPManagerEvent object is dispatched whenever a UDPClassicMessageEvent occurs.
    /// 
    /// This event has only one name : MESSAGE, you can listen to it if you need to receive "classic" UDP data (from a user who is not using nor UDPClient, UDPServer nor UDPManager to send the data)
    /// 
    /// </summary>
    public class UDPClassicMessageEvent : Event
    {
        private string _message;
        private IPEndPoint _remote;
        /// <summary>
        /// An enum containing all the names of the UDPManager events
        /// </summary>
        #pragma warning disable 108
        public enum Names { MESSAGE };
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">A string representing the event name</param>
        /// <param name="message">A string holding the message</param>
        /// <param name="remote">An <see cref="IPEndPoint"/> object that holds informations about the sender</param>
        internal UDPClassicMessageEvent(object name, string message, IPEndPoint remote) : base(name)
        {

            this._message = message;
            this._remote = remote;
        }
        /// <summary>
        /// A string holding the message
        /// </summary>
        public string Message
        {
            get
            {
                return (this._message);
            }
        }
        /// <summary>
        /// An <see cref="IPEndPoint"/> object that holds informations about the sender
        /// </summary>
        public IPEndPoint Remote
        {
            get
            {
                return (this._remote);
            }
        }
    }
}