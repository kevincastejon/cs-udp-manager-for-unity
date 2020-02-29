using System.Collections.Generic;
using System.Linq;

namespace kevincastejon
{
    /// <summary>  
    /// UDPServer allows receiving connections from UDPClient instances, and communicate with reliability options and features like connection, ping, server timeout, etc..
    ///  
    /// Basic usage:
    /// 
    /// 
    /// <code>
    /// 
    ///   class UDPServerTester
    ///   {
    ///        UDPServer server = new UDPServer();
    ///        public UDPServerTester(int localPort)
    ///        {
    ///            server.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, UDPManagerHandler);
    ///            server.AddEventListener<UDPServerEvent>(UDPServerEvent.Names.CLIENT_CONNECTED, ServerHandler);
    ///            server.AddEventListener<UDPServerEvent>(UDPServerEvent.Names.CLIENT_PONG, ServerHandler);
    ///            server.AddEventListener<UDPServerEvent>(UDPServerEvent.Names.CLIENT_RECONNECTED, ServerHandler);
    ///            server.AddEventListener<UDPServerEvent>(UDPServerEvent.Names.CLIENT_SENT_DATA, ServerHandler);
    ///            server.AddEventListener<UDPServerEvent>(UDPServerEvent.Names.CLIENT_TIMED_OUT, ServerHandler);
    ///            server.AddChannel("mainChannel",false,true,50,1000);
    ///            server.Start(localPort);
    ///        }
    ///        private void ServerHandler(UDPServerEvent e)
    ///        {
    ///            //Console.WriteLine(e.Name);
    ///            if(e.Name == UDPServerEvent.Names.CLIENT_CONNECTED.ToString())
    ///            {
    ///                Console.WriteLine("A client is connected! ID:" + e.UDPpeer.ID.ToString());
    ///            }
    ///            else if (e.Name == UDPServerEvent.Names.CLIENT_SENT_DATA.ToString())
    ///            {
    ///                Console.WriteLine("Client sent : " + e.UDPdataInfo.Data["message"]);
    ///                server.SendToClient("mainChannel",new { message="You're welcome!"},e.UDPpeer);
    ///            }
    ///        }
    ///        private void UDPManagerHandler(UDPManagerEvent e)
    ///        {
    ///            Console.WriteLine(e.Name);
    ///        }
    ///   }
    ///         
    /// </code>
    ///     
    /// The instances of this class dispatch the following events:
    /// 
    /// <see cref="UDPServerEvent"/>:
    /// 
    /// <list type="UDPServerEvent">
    /// <item>CLIENT_CONNECTED</item> <description> - Dispatched when the instance is connected to a server </description>
    /// <item>CLIENT_RECONNECTED</item> <description> - Dispatched when the client has responded to a ping </description>
    /// <item>CLIENT_PONG</item> <description> - Dispatched when a connection attempt to a server has failed </description>
    /// <item>CLIENT_SENT_DATA</item> <description> - Dispatched when the server has sent data to the instance </description>
    /// <item>CLIENT_TIMED_OUT</item> <description> - Dispatched when the server has stopped responding </description>
    /// </list>
    /// 
    /// You can also listen to these events that are dispatched by the UDPManager core class and redispatched by the UDPServer
    /// 
    /// <see cref="UDPManagerEvent"/>:
    /// 
    /// <list type="UDPManagerEvent">
    /// <item>BOUND</item> <description> - Dispatched when the instance is bound to a local port </description>
    /// <item>DATA_CANCELED</item> <description> - Dispatched when a data sending is canceled </description>
    /// <item>DATA_DELIVERED</item> <description> - Dispatched when data has been delivered </description>
    /// <item>DATA_RECEIVED</item> <description> - Dispatched when data has been received </description>
    /// <item>DATA_RETRIED</item> <description> - Dispatched when data sending has been retried </description>
    /// <item>DATA_SENT</item> <description> - Dispatched when data has been sent </description>
    /// </list>
    /// 
    /// <seealso cref="UDPClient"/>
    /// </summary>  
    public class UDPServer : EventDispatcher
    {
        private UDPManager _udpManager;
        private List<UDPPeer> _peers = new List<UDPPeer>();      //clients
        private bool _started;

        /// <summary>
        /// constructor </summary>
        /// <param name='localPort'>The local port to bind to directly on the instanciation, you can specify a port from 1 to 65535, 0  will bind to the first available port, -1 will not bind (you will have to call Bind method manually after instanciation then). Default is -1.</param><c>!Any other value will throw an exception!</c>
        public UDPServer(int localPort = -1)
        {
            _udpManager = new UDPManager(-1);
            _udpManager._udpServerPeers = new List<UDPPeer>();
            _udpManager._InitHiddenChannels();
            _udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RECEIVED, _ReceivedDataHandler);
            _udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, _CancelHandler);
            _udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, _RetryHandler);
            _udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, _DeliveryHandler);
            _udpManager.AddEventListener<UDPClassicMessageEvent>(UDPClassicMessageEvent.Names.MESSAGE, _ClassicDataSystemHandler);
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, this._Listening);
            if (localPort > -1) this.Start(localPort);
        }
        /// <summary>
        /// Start the server. Bind to the local port provided and start listening for connections</summary>
        /// <remarks>Only call this method if you did not provide a local port on the constructor parameter(or if you provide -1)</remarks>
        /// <c>If the server is already started (=Bound), the method will call Reset before starting again on the specified localPort</c>
        /// <param name='localPort'>The local port to bind to, you can specify a port from 1 to 65535, 0  will bind to the first available port. Default is 0.</param><c>!Any other value will throw an exception!</c>
        public void Start(int localPort)
        {
            if (_started) Close(false);
            _udpManager.Bind(localPort);
            _started = true;
        }
        /// <summary>
        /// Add a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to create.</param>
        /// <c>It must be unique on this instance, if a channel with the same name has already been added the method will throw an exception!</c>
        /// <remarks>You can check if the name is already used by calling <see cref="GetChannelByName"/></remarks>
        /// <param name='guarantiesDelivery'>If true the messages sent though this channel will wait for a receipt from the target that will guaranty the delivery. It will wait during the time specified on <paramref name="retryTime"/> until what it will retry sending the message, etc... If false the message is sent once without guranty of delivery. Default is false.<remarks>The guaranty of the delivery works only if the target uses the same library (C#,AS3 or JS) to communicate over UDP!</remarks></param>
        /// <param name='maintainOrder'>If true it will wait for a message to be delivered before sending the next one.<remarks> Only works if <paramref name="guarantiesDelivery"/> is true</remarks></param>
        /// <param name='retryTime'>The number of milliseconds the channel will wait before retrying sending the message if not delivered. Default is 30.</param>
        /// <param name='cancelTime'>The number of milliseconds the channel will wait before canceling the message if not delivered. Default is 500.</param>
        public void AddChannel(string channelName, bool guarantiesDelivery = false, bool maintainOrder = false, float retryTime = 30, float cancelTime = 500)
        {
            _udpManager.AddChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        public void RemoveChannel(string channelName)
        {
            _udpManager.RemoveChannel(channelName);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int num)
        {
            return (this._udpManager.GetChannelAt(num));
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            return (_udpManager.GetChannelByName(channelName));
        }
        /// <summary>
        /// Get a connected UDPPeer at the specified index</summary>
        /// <remarks>You can list all UDPPeers connected with <see cref="NumClients"/></remarks>
        public UDPPeer GetClientAt(int index)
        {
            return (_peers[index]);
        }
        /// <summary>
        /// Send data through an UDPChannel to all the clients and returns an array of <see cref="UDPDataInfo"/> objects.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        public List<UDPDataInfo> SendToAll(string channelName, object udpData)
        {       //sendToAllClients
            int max = _peers.Count;
            if (max == 0) return (null);
            List<UDPDataInfo> ret = new List<UDPDataInfo>();
            for (int i = 0; i < max; i++)
            {
                ret.Add(_udpManager.Send(channelName, udpData, _peers[i].Address, _peers[i].Port));
            }
            return (ret);
        }
        /// <summary>
        /// Send data through an UDPChannel to a distant user and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="peer">A connected UDPPeer</param>
        public UDPDataInfo SendToClient(string channelName, object udpData, UDPPeer peer)
        {
            return (_udpManager.Send(channelName, udpData, peer.Address, peer.Port));
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {
            _udpManager.SendOutOfChannels(udpData, remoteAddress, remotePort);
        }
        /// <summary>
        /// Add a "white" IP address to the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddWhiteAddress(string address)
        {
            _udpManager.AddWhiteAddress(address);
        }
        /// <summary>
        /// Add a "black" IP address to the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddBlackAddress(string address)
        {
            _udpManager.AddBlackAddress(address);
        }
        /// <summary>
        /// Remove a "white" IP address from the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port). It must be registered on this instance, if that address can't be found on the list the method will throw an exception!<remarks>You can check if the name is registered by calling <see cref="GetWhiteAddressAt(int)"/> and <see cref="WhiteListLength"/></remarks></param>
        public void RemoveWhiteAddress(string address)
        {
            _udpManager.RemoveWhiteAddress(address);
        }
        /// <summary>
        /// Remove a "black" IP address from the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port).</param>
        /// <c>It must be registered on this instance, if that address can't be found on the list the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetBlackAddressAt(int)"/> and <see cref="BlackListLength"/></remarks>
        public void RemoveBlackAddress(string address)
        {
            _udpManager.RemoveBlackAddress(address);
        }
        /// <summary>
        /// Get the white address at the specified index on the whitelist</summary>
        public string GetWhiteAddressAt(int index)
        {
            return (_udpManager.GetWhiteAddressAt(index));
        }
        /// <summary>
        /// Get the black address at the specified index on the blacklist</summary>
        public string GetBlackAddressAt(int index)
        {
            return (_udpManager.GetBlackAddressAt(index));
        }
        /// <summary>
        /// Disconnect the connected UDPPeer</summary>
        public void KickClient(UDPPeer peer)
        {
            this._peers.RemoveAt(this._peers.IndexOf(peer));
            this._udpManager._udpServerPeers.RemoveAt(this._udpManager._udpServerPeers.IndexOf(peer));
            this._udpManager._RemoveHiddenClientPingChannel(peer.ID);
            peer.RemoveEventListener<Event>("ping", this._PingTimerHandler);
            peer.Close();
        }
        /// <summary>
        /// Disconnect the connected UDPClient and add its IP to the blacklist</summary>
        /// <remarks>The <paramref name="blackListEnabled"/> has to be true for the bannishment to be effective</remarks>
        public void BanClient(UDPPeer peer)
        {
            AddBlackAddress(peer.Address);
            KickClient(peer);
        }
        /// <summary>
        /// Resets the UDPServer. Means unbind, and remove the UDPChannels if you specify the parameter as true</summary>
        /// <param name='removeChannels'></param>
        public void Close(bool removeChannels = true)
        {
            _started = false;
            int max = _peers.Count;
            for (int i = 0; i < max; i++)
            {
                _peers[i].Close();
            }
            _peers = new List<UDPPeer>();
            _udpManager._udpServerPeers = new List<UDPPeer>();
            _udpManager._CloseHiddenChannels();
            _udpManager.Close(removeChannels);
        }
        /// <summary>
        /// True if the instance is bound to a port</summary>
        public bool Bound
        {
            get
            {
                return (this._udpManager.Bound);
            }
        }
        /// <summary>
        /// Returns the local port on which the UDPServer is started. Returns 0 if the UDPServer is not started yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                return (this._udpManager.BoundPort);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance</summary>
        public int NumChannels
        {
            get
            {
                return (this._udpManager.NumChannels);
            }
        }
        /// <summary>
        /// The number of connected UDPPeers</summary>
        public int NumClients
        {
            get
            {
                return (_peers.Count);
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the whitelist should be the only ones to be treated or not</summary><seealso cref="AddWhiteAddress(string)"/>
        public bool WhiteListEnabled
        {
            get
            {
                return (_udpManager.WhiteListEnabled);
            }
            set
            {
                _udpManager.WhiteListEnabled = value;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the blacklist should be ignored or not</summary><seealso cref="AddBlackAddress(string)"/>
        public bool BlackListEnabled
        {
            get
            {
                return (_udpManager.BlackListEnabled);
            }
            set
            {
                _udpManager.BlackListEnabled = value;
            }
        }
        /// <summary>
        /// The length of the whitelist</summary>
        public int WhiteListLength
        {
            get
            {
                return (_udpManager.WhiteListLength);
            }
        }
        /// <summary>
        /// The length of the blacklist</summary>
        public int BlackListLength
        {
            get
            {
                return (_udpManager.BlackListLength);
            }
        }
        
        //
        //private methods
        //
        //UDPManagerEvents listeners
        private void _ReceivedDataHandler(UDPManagerEvent e)
        {

            UDPPeer peer;

            if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCC && e.UDPdataInfo.Data["messageType"].ToString() == "newConnection")
            {   //implements connection feature
                peer = this._GetPeerByAddress(e.UDPdataInfo.RemoteAddress, e.UDPdataInfo.RemotePort);

                if (peer == null)
                {
                    peer = new UDPPeer(e.UDPdataInfo.RemoteAddress, e.UDPdataInfo.RemotePort);
                    this._peers.Add(peer);
                    this._udpManager._udpServerPeers.Add(peer);
                    this._udpManager._AddHiddenClientPingChannel(peer.ID);
                    peer.AddEventListener<Event>("ping", this._PingTimerHandler);
                    peer.StartPingTimer();
                    this.DispatchEvent(new UDPServerEvent(UDPServerEvent.Names.CLIENT_CONNECTED, peer));
                }
                else this.DispatchEvent(new UDPServerEvent(UDPServerEvent.Names.CLIENT_RECONNECTED, peer));
            }
            else if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCP && e.UDPdataInfo.Data["messageType"].ToString() == "ping")
            {           //implements ping feature
                        //do nothing
            }
            else
            {
                peer = this._peers.Where(a => a.Address == e.UDPdataInfo.RemoteAddress).Where(a => a.Port == e.UDPdataInfo.RemotePort).FirstOrDefault();
                if (peer != null)
                {
                    this.DispatchEvent(new UDPServerEvent(UDPServerEvent.Names.CLIENT_SENT_DATA, peer, e.UDPdataInfo));
                }
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RECEIVED, e.UDPdataInfo));  //redispatch
            }
        }
        private void _CancelHandler(UDPManagerEvent e)
        {
            if (e.UDPdataInfo.ChannelName.Substring(0, 7) == UDPManager._UDPMRCP && e.UDPdataInfo.Data.messageType.ToString() == "ping")
            {   //implements time out feature
                UDPPeer peer = this._peers.Where(a => a.Address == e.UDPdataInfo.RemoteAddress).Where(a => a.Port == e.UDPdataInfo.RemotePort).FirstOrDefault();
                this.KickClient(peer);
                this.DispatchEvent(new UDPServerEvent(UDPServerEvent.Names.CLIENT_TIMED_OUT, peer));
            }
            else
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_CANCELED, e.UDPdataInfo));      //redispatch
        }
        private void _RetryHandler(UDPManagerEvent e)
        {
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RETRIED, e.UDPdataInfo));
        }
        private void _DeliveryHandler(UDPManagerEvent e) 
        {
            if (e.UDPdataInfo.ChannelName.Substring(0, 7) == UDPManager._UDPMRCP && e.UDPdataInfo.Data.messageType.ToString() == "ping")
            {   //implements ping feature
                UDPPeer peer = this._peers.Where(a => a.Address == e.UDPdataInfo.RemoteAddress).Where(a => a.Port == e.UDPdataInfo.RemotePort).FirstOrDefault();
                peer.StartPingTimer();
                peer.SetPing(e.UDPdataInfo.Ping);
                this.DispatchEvent(new UDPServerEvent(UDPServerEvent.Names.CLIENT_PONG, peer));
            }
            else
            {
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_DELIVERED, e.UDPdataInfo));     //redispatch
            }
        }
        private void _ClassicDataSystemHandler(UDPClassicMessageEvent e)
        {
            this.DispatchEvent(new UDPClassicMessageEvent(UDPClassicMessageEvent.Names.MESSAGE, e.Message, e.Remote));      //redispatch classic datagramsocket event
        }
        private void _Listening(UDPManagerEvent e)
        {
            this.DispatchEvent(e);
        }
        //ping handling event
        private void _PingTimerHandler(Event e)
        {
            UDPPeer peer = e.Target as UDPPeer;
            this.SendToClient(UDPManager._UDPMRCP + ":" + peer.ID, new { messageType = "ping" }, peer);
        }
        private UDPPeer _GetPeerByAddress(string address, int port)
        {
            int max = this._peers.Count;
            for (int i = 0; i < max; i++)
            {
                if (this._peers[i].Address == address && this._peers[i].Port == port)
                {
                    return (this._peers[i]);
                }
            }
            return (null);
        }
    }
}