namespace kevincastejon
{
    /// <summary>
    /// An UDPClient object is dispatched whenever a UDPClient event occurs.
    /// 
    /// These are all the available names for this event:
    /// <list type="UDPClientEvent">
    /// <item>CONNECTED_TO_SERVER</item> <description> - Dispatched when the instance is connected to a server </description>
    /// <item>CONNECTION_FAILED</item> <description> - Dispatched when a connection attempt to a server has failed </description>
    /// <item>SERVER_PONG</item> <description> - Dispatched when the server has responded to a ping </description>
    /// <item>SERVER_TIMED_OUT</item> <description> - Dispatched when the server has stopped responding </description>
    /// <item>SERVER_SENT_DATA</item> <description> - Dispatched when the server has sent data to the instance </description>
    /// </list>
    /// </summary>
    public class UDPClientEvent : UDPManagerEvent
    {
        /// <summary>
        /// An enum containing all the names of the UDPManager events
        /// </summary>
        #pragma warning disable 108
        public enum Names { CONNECTED_TO_SERVER, CONNECTION_FAILED, SERVER_PONG, SERVER_TIMED_OUT, SERVER_SENT_DATA };
        private UDPPeer _udpPeer;
        internal UDPClientEvent(object name, UDPPeer udpPeer, UDPDataInfo udpDataInfo = null) : base(name, udpDataInfo)
        {
            this._udpPeer = udpPeer;

        }
        /// <summary>
        /// An <see cref="UDPPeer"/> object that holds informations about the sender
        /// </summary>
        public UDPPeer UDPpeer
        {
            get
            {
                return (this._udpPeer);
            }
        }


    }
}
