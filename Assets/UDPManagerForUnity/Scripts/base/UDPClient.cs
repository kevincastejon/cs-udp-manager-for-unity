using System;
namespace kevincastejon
{
    /// <summary>  
    /// UDPClient allows you to connect to a UDPServer, and communicate with reliability options and features like connection, ping, server timeout, etc..
    ///  
    /// Basic usage:
    /// 
    /// 
    /// <code>
    /// 
    ///    class UDPClientTester
    ///    {
    ///        private UDPClient client = new UDPClient();
    ///        public UDPClientTester(string serverIP, int serverPort,int localPort=0)
    ///        {
    ///            client.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, UDPManagerHandler);
    ///            client.AddEventListener<UDPClientEvent>(UDPClientEvent.Names.CONNECTED_TO_SERVER, ClientHandler);
    ///            client.AddEventListener<UDPClientEvent>(UDPClientEvent.Names.CONNECTION_FAILED, ClientHandler);
    ///            client.AddEventListener<UDPClientEvent>(UDPClientEvent.Names.SERVER_PONG, ClientHandler);
    ///            client.AddEventListener<UDPClientEvent>(UDPClientEvent.Names.SERVER_SENT_DATA, ClientHandler);
    ///            client.AddEventListener<UDPClientEvent>(UDPClientEvent.Names.SERVER_TIMED_OUT, ClientHandler);
    ///            client.AddChannel("mainChannel", false, true, 50, 1000);
    ///            client.Connect(serverIP, serverPort, localPort);
    ///        }
    ///        private void ClientHandler(UDPClientEvent e)
    ///        {
    ///            //Console.WriteLine("clientside event: "+e.Name);
    ///            if (e.Name == UDPClientEvent.Names.CONNECTED_TO_SERVER.ToString())
    ///            {
    ///                client.SendToServer("mainChannel", new { message = "Thanks for accepting my connection !" });
    ///            }
    ///            else if (e.Name == UDPClientEvent.Names.SERVER_SENT_DATA.ToString())
    ///            {
    ///                Console.WriteLine("Server sent : " + e.UDPdataInfo.Data["message"]);
    ///            }
    ///        }
    ///        private void UDPManagerHandler(UDPManagerEvent e)
    ///        {
    ///            Console.WriteLine(e.Name);
    ///        }
    ///    }
    ///         
    /// </code>
    /// 
    /// The instances of this class dispatch the following events:
    /// 
    /// <see cref="UDPClientEvent"/>:
    /// 
    /// <list type="UDPClientEvent">
    /// <item>CONNECTED_TO_SERVER</item> <description> - Dispatched when the instance is connected to a server </description>
    /// <item>CONNECTION_FAILED</item> <description> - Dispatched when a connection attempt to a server has failed </description>
    /// <item>SERVER_PONG</item> <description> - Dispatched when the server has responded to a ping </description>
    /// <item>SERVER_SENT_DATA</item> <description> - Dispatched when the server has sent data to the instance </description>
    /// <item>SERVER_TIMED_OUT</item> <description> - Dispatched when the server has stopped responding </description>
    /// </list>
    /// 
    /// You can also listen to these events that are dispatched by the UDPManager core class and redispatched by the UDPClient
    /// 
    /// <see cref="UDPManagerEvent"/>:
    /// 
    /// <list type="UDPManagerEvent">
    /// <item>BOUND</item> <description> - Dispatched when the instance is bound to a local port </description>
    /// <item>DATA_CANCELED</item> <description> - Dispatched when a data sending is canceled </description>
    /// <item>DATA_DELIVERED</item> <description> - Dispatched when data has been delivered </description>
    /// <item>DATA_RECEIVED</item> <description> - Dispatched when data has been received </description>
    /// <item>DATA_RETRIED</item> <description> - Dispatched when data sending has been retried </description>
    /// </list>
    /// 
    /// <seealso cref="UDPServer"/>
    /// </summary>  

    public class UDPClient : EventDispatcher
    {
        private UDPPeer _udpServer = null;
        private int _serverPort;
        private string _serverAddress;
        private bool _connected = false;
        private bool _connecting = false;
        private UDPManager _udpManager = new UDPManager(-1);

        /// <summary>
        /// constructor </summary>
        /// <param name='localPort'>The local port to bind to directly on the instantiation, you can specify a port from 1 to 65535, 0  will bind to the first available port, -1 will not bind (you will have to call Bind method manually after instantiation then). Default is -1.</param>
        /// <c>!Any other value will throw an exception!</c>
        /// <param name='serverAddress'>The IPV4 address of an UDPServer. If you provide this parameter it will try to connect directly on the instantiation.</param>
        /// <c>If you provide this parameter you have to provide the <paramref name="serverPort"/> too!</c>
        /// <param name='serverPort'>The port of an UDPServer. If you provide this parameter it will try to connect directly on the instantiation.</param>
        /// <c>If you provide this parameter you have to provide the <paramref name="serverAddress"/> too!</c>
        public UDPClient(int localPort = -1, string serverAddress = null, int serverPort = 0)
        {
            this._serverPort = serverPort;
            this._serverAddress = serverAddress;
            this._udpManager._InitHiddenChannels();
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RECEIVED, this._ReceivedDataHandler);
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelHandler);
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryHandler);
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveryHandler);
            this._udpManager.AddEventListener<UDPClassicMessageEvent>(UDPClassicMessageEvent.Names.MESSAGE, this._ClassicDataSystemHandler);
            this._udpManager.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, this._Listening);
            if (localPort > -1)
                this._udpManager.Bind(localPort);

        }
        /// <summary>
        /// Connect to an UDPServer (C#, AS3 or JS).</summary>
        /// <remarks>Only call this method if you did not provide <paramref name="serverAddress"/> and <paramref name="serverPort"/> on the constructor </remarks>
        /// <param name='serverAddress'>The IPV4 address of an UDPServer. If you provide this parameter it will try to connect directly on the instantiation.</param>
        /// <c>If you provide this parameter you have to provide the <paramref name="serverPort"/> too!</c>
        /// <param name='serverPort'>The port of an UDPServer. If you provide this parameter it will try to connect directly on the instantiation.</param>
        /// <c>If you provide this parameter you have to provide the <paramref name="serverAddress"/> too!</c>
        /// <param name='localPort'>The local port to bind to. If the instance is already bound it will call Reset(false) first. You can specify a port from 1 to 65535, 0  will bind to the first available port, -1 will not bind (you will have to call Bind method manually after instantiation then). Default is -1.</param>
        /// <c>!Any other value will throw an exception!</c>
        public void Connect(string serverAddress, int serverPort, int localPort = 0)
        {
            if (this._connecting == false)
            {
                if (this._connected)
                    this.Close(false);
                this._serverAddress = serverAddress;
                this._serverPort = serverPort;
                if (this._udpManager.Bound == false)
                {
                    this._udpManager.Bind(localPort);
                }
                else
                {
                    this._udpManager.Send(UDPManager._UDPMRCC, new { messageType = "newConnection" }, serverAddress, serverPort);
                    this._connecting = true;
                    this._udpManager._UDPClientConnecting = new UDPEndPoint(serverAddress, serverPort);
                }
            }
            else throw new Exception("UDPClient is already connecting");
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
            this._udpManager.AddChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        public void RemoveChannel(string channelName)
        {
            _udpManager.RemoveChannel(channelName);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int num){
		return (this._udpManager.GetChannelAt(num));
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            return (_udpManager.GetChannelByName(channelName));
        }
        /// <summary>
        /// Returns the server IPV4 address</summary>
        public string GetServerAddress()
        {
            return (_serverAddress);
        }
        /// <summary>
        /// Returns the server port</summary>
        public int GetServerPort()
        {
            return (_serverPort);
        }
        /// <summary>
        /// Send data through an UDPChannel to the server and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        public UDPDataInfo SendToServer(string channelName, object udpData)
        {
            return (_udpManager.Send(channelName, udpData, _serverAddress, _serverPort));
        }
        /// <summary>
        /// Send data through an UDPChannel to a distant user, other than server, and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public UDPDataInfo SendToNonServerPeer(string channelName, object udpData, string remoteAddress, int remotePort)
        {
            return (_udpManager.Send(channelName, udpData, remoteAddress, remotePort));
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {   //send to one client
            _udpManager.SendOutOfChannels(udpData, remoteAddress, remotePort);
        }
        /// <summary>
        /// Resets the UDPClient. Means disconnect, unbind, and remove the UDPChannels if you specify the parameter as true</summary>
        /// <param name='removeChannels'></param>
        public void Close(bool removeChannels = true)
        {
            this._connecting = false;
            this._udpManager._UDPClientConnecting = null;
            this._connected = false;
            if (this._udpServer != null) this._udpServer.Close();
            this._udpServer = null;
            if (removeChannels) this._udpManager._CloseHiddenChannels();
            this._udpManager.Close(removeChannels);
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
        /// Returns the local port on which the UDPClient is bound. Returns 0 if the UDPClient is not bound yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                return (this._udpManager.BoundPort);
            }
        }
        /// <summary>
        /// True if the instance is connected to a server instance</summary>
        public bool Connected
        {
            get
            {
                return (this._connected);
            }
        }
        /// <summary>
        /// True if the instance is connected to a server instance</summary>
        public bool Connecting
        {
            get
            {
                return (this._connecting);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance
        /// </summary>
        public int NumChannels
        {
            get
            {
                return (this._udpManager.NumChannels);
            }
        }
        

        //
        //private methods
        //

        private void _ReceivedDataHandler(UDPManagerEvent e)
        {
            if (e.UDPdataInfo.ChannelName.Substring(0, 7) == UDPManager._UDPMRCP && e.UDPdataInfo.Data["messageType"].ToString() == "ping")
            { 
                //certainly do nothin
            }
            else
            {
                if (this._udpServer != null && this._udpServer.Address == e.UDPdataInfo.RemoteAddress && this._udpServer.Port == e.UDPdataInfo.RemotePort)
                {
                    this.DispatchEvent(new UDPClientEvent(UDPClientEvent.Names.SERVER_SENT_DATA, this._udpServer, e.UDPdataInfo));
                }
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RECEIVED, e.UDPdataInfo));
            }
        }
        private void _CancelHandler(UDPManagerEvent e)
        {
            if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCP && e.UDPdataInfo.Data.messageType.ToString() == "ping")
            {
                this.DispatchEvent(new UDPClientEvent(UDPClientEvent.Names.SERVER_TIMED_OUT, this._udpServer));
                this.Close();
            }
            else if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCC && e.UDPdataInfo.Data.messageType.ToString() == "newConnection")
            {
                this.DispatchEvent(new UDPClientEvent(UDPClientEvent.Names.CONNECTION_FAILED, null));
                this.Close();
            }
            else
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_CANCELED, e.UDPdataInfo));
        }
        private void _ClassicDataSystemHandler(UDPClassicMessageEvent e)
        {
            this.DispatchEvent(new UDPClassicMessageEvent(UDPClassicMessageEvent.Names.MESSAGE,e.Message,e.Remote));      //redispatch classic datagramsocket event
        }
        private void _RetryHandler(UDPManagerEvent e)
        {
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RETRIED, e.UDPdataInfo));
        }
        private void _DeliveryHandler(UDPManagerEvent e)
        {
            if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCC && e.UDPdataInfo.Data.messageType.ToString() == "newConnection")
            {
                this._udpServer = new UDPPeer(e.UDPdataInfo.RemoteAddress, e.UDPdataInfo.RemotePort);
                this._udpServer.AddEventListener<Event>("ping", this._PingTimerHandler);
                this._udpServer.StartPingTimer();
                this._connecting = false;
                this._udpManager._UDPClientConnecting = null;
                this._connected = true;
                this.DispatchEvent(new UDPClientEvent(UDPClientEvent.Names.CONNECTED_TO_SERVER, this._udpServer));
            }
            else if (e.UDPdataInfo.ChannelName == UDPManager._UDPMRCP && e.UDPdataInfo.Data.messageType.ToString() == "ping")
            {
                this._udpServer.SetPing(e.UDPdataInfo.Ping);
                this._udpServer.StartPingTimer();
                this.DispatchEvent(new UDPClientEvent(UDPClientEvent.Names.SERVER_PONG, this._udpServer));
            }
            else
            {
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_DELIVERED, e.UDPdataInfo));
            }
        }
        private void _Listening(UDPManagerEvent e)
        {
            if (this._serverAddress != null && this._serverPort > 0) this.Connect(this._serverAddress, this._serverPort);
            this.DispatchEvent(e);
        }
        private void _PingTimerHandler(Event e)
        {
            this.SendToServer(UDPManager._UDPMRCP, new { messageType = "ping" });
        }

    }


}