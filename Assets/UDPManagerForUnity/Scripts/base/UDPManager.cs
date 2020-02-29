using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// The UDPManager package offers a event-driven framework on top of UDP with many reliability options, peer-to-peer communication, server and client connection features, and much more.
/// </summary>
namespace kevincastejon
{
    /// <summary>  
    /// UDPManager is the core class of the UDPManager package. It allows you to send and receive easily data through UDP, to create channels with differents reliability settings such as delivery guaranty, order maintain, or just send without any callback.
    /// 
    /// For more advanced features such as client-server connection, ping, timeout, etc... please see <see cref="UDPClient"/> and <see cref="UDPServer"/>
    /// 
    /// Basic usage:
    /// 
    /// <code>
    /// 
    ///         //Instantiate UDPManager and bind on the port of your choice
    ///         UDPManager udpm = new UDPManager(9876);
    ///     
    ///         //Add listeners on the instance of UDPManager
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, UDPManagerHandler);
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, UDPManagerHandler);
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, UDPManagerHandler);
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RECEIVED, UDPManagerHandler);
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, UDPManagerHandler);
    ///         udpm.On<UDPManagerEvent>(UDPManagerEvent.Names.DATA_SENT, UDPManagerHandler);
    /// 
    ///         //Add a UDPChannel
    ///         udpm.AddChannel("mainChannel", true, true, 50, 1000);
    /// 
    ///         //Send a message to the target IP and port
    ///         udpm.Send("mainChannel", new { msg = "Hello!" }, "x.x.x.x", 6789);
    /// 
    ///         private void UDPManagerHandler(UDPManagerEvent e){
    ///         //Monitor UDPManagerEvents
    ///         Console.WriteLine(e.Name);
    /// 
    ///             if(e.name==UDPManagerEvent.Names.DATA_RECEIVED){
    ///             //Display received messages
    ///             Console.WriteLine(e.UdpDataInfo.Data);
    ///             }
    ///         }
    ///         
    /// </code>
    /// 
    /// This method dispatch the following events:
    /// 
    /// <see cref="UDPManagerEvent"/>:
    /// 
    /// <list type="UDPManagerEvent">
    /// <item>BOUND</item> <description> - Dispatched when the instance is bound to a local port </description>
    /// <item>DATA_RECEIVED</item> <description> - Dispatched when data has been received </description>
    /// <item>DATA_DELIVERED</item> <description> - Dispatched when data has been delivered </description>
    /// <item>DATA_RETRIED</item> <description> - Dispatched when data sending has been retried </description>
    /// <item>DATA_CANCELED</item> <description> - Dispatched when a data sending is canceled </description>
    /// <item>DATA_SENT</item> <description> - Dispatched when data is being sent </description>
    /// </list>
    /// 
    /// </summary>

    public class UDPManager : EventDispatcher
    {
        internal const string _UDPMRCP = "UDPMRCP";
        internal const string _UDPMRCC = "UDPMRCC";
        internal static Stopwatch chrono;
        internal List<UDPPeer> _udpServerPeers = new List<UDPPeer>();
        internal UDPEndPoint _UDPClientConnecting = null;        //

        private bool _bound = false;
        private UdpClient _UDPSocketIPv4 = null;
        private List<string> _whiteList = new List<string>();
        private List<string> _blackList = new List<string>();
        private bool _whiteListEnabled;
        private bool _blackListEnabled;
        private List<string> _receivedIDs = new List<string>();
        private List<UDPChannel> _channels = new List<UDPChannel>();
        private List<UDPChannel> _clientsPingChannels = new List<UDPChannel>();
        private UDPChannel _pingChannel = null;         //Implemented for UDPServer
        private UDPChannel _connectionChannel = null;   //and UDPClient
        private static List<UDPManager> UDPmanagers = new List<UDPManager>();
        private static readonly object locker = new object();

        /// <summary>
        /// constructor </summary>
        /// <param name='localPort'>The local port to bind to directly on the instanciation, you can specify a port from 1 to 65535, 0  will bind to the first available port, -1 will not bind (you will have to call Bind method manually after instanciation then). Default is -1.</param><c>!Any other value will throw an exception!</c>
        public UDPManager(int localPort = -1)
        {
            if (chrono == null) { chrono = new Stopwatch(); chrono.Start(); }
            UDPmanagers.Add(this);
            if (localPort > -1) Bind(localPort);
        }
        /// <summary>
        /// Bind to a local port </summary>
        /// <remarks>Only call this method if you did not provide a local port on the constructor parameter(or if you provide -1)</remarks>
        /// <c>If the instance is already bound, the method will call Reset before bounding again on the specified localPort</c>
        /// <param name='localPort'>The local port to bind to, you can specify a port from 1 to 65535, 0  will bind to the first available port. Default is 0.</param><c>!Any other value will throw an exception!</c>
        public void Bind(int localPort)
        {
            this.Close(false);
            IPAddress ownAddress = IPAddress.Parse("0.0.0.0");
            IPEndPoint myEP = new IPEndPoint(ownAddress, localPort);
            _UDPSocketIPv4 = new UdpClient(myEP);
            _UDPSocketIPv4.BeginReceive(new AsyncCallback(this._ReceiveDataHandler), new object());
            _bound = true;
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.BOUND, null));
        }
        /// <summary>
        /// Add a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to create. It must be unique on this instance, if a channel with the same name has already been added the method will throw an exception!<remarks>You can check if the name is already used by calling <see cref="GetChannelByName"/></remarks></param>
        /// <param name='guarantiesDelivery'>If true the messages sent though this channel will wait for a receipt from the target that will guaranty the delivery. It will wait during the time specified on <paramref name="retryTime"/> until what it will retry sending the message, etc... If false the message is sent once without guranty of delivery. Default is false.<remarks>The guaranty of the delivery works only if the target uses the same library (C#,AS3 or JS) to communicate over UDP!</remarks></param>
        /// <param name='maintainOrder'>If true it will wait for a message to be delivered before sending the next one.<remarks> Only works if <paramref name="guarantiesDelivery"/> is true</remarks></param>
        /// <param name='retryTime'>The number of milliseconds the channel will wait before retrying sending the message if not delivered. Default is 30.</param>
        /// <param name='cancelTime'>The number of milliseconds the channel will wait before canceling the message if not delivered. Default is 500.</param>
        public void AddChannel(string channelName, bool guarantiesDelivery = false, bool maintainOrder = false, float retryTime = 30, float cancelTime = 500)
        {
            if (this.GetChannelByName(channelName) == null)
            {
                UDPChannel channel = new UDPChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
                channel.AddEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
                channel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
                channel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
                channel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
                this._channels.Add(channel);
            }
            else throw new ArgumentException("channelName " + channelName + " already used");
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        public void RemoveChannel(string channelName)
        {
            UDPChannel channel = this.GetChannelByName(channelName);
            channel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            channel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            channel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            channel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            channel._Close();
            this._channels.Remove(channel);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int index)
        {
            return (this._channels[index]);
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name. Returns null if no channel with that has been found.</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            int max = this._channels.Count;
            for (int i = 0; i < max; i++)
            {
                if (this._channels[i].Name == channelName) return (this._channels[i]);
            }
            return (null);
        }
        /// <summary>
        /// Send data through an UDPChannel to a distant user and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A <see cref="JavaScriptObject"/> that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public UDPDataInfo Send(string channelName, object udpData, string remoteAddress, int remotePort)
        {

            UDPChannel channel;
            if (channelName == UDPManager._UDPMRCP)
                channel = this._pingChannel;
            else if (channelName == UDPManager._UDPMRCC)
                channel = this._connectionChannel;
            else if (channelName.Length >= 8 && channelName.Substring(0, 8) == UDPManager._UDPMRCP + ":")
            {
                channel = this._GetHiddenClientPingChannelByID(channelName.Split(':')[1]);
            }
            else
                channel = this.GetChannelByName(channelName);
            if (channel != null)
            {
                UDPDataInfo udpDataInf = new UDPDataInfo(channelName, remoteAddress, remotePort, udpData, this._GetNextUniqueID());
                channel._AddDataToBuffer(udpDataInf);
                return (udpDataInf);
            }
            else throw new ArgumentNullException("Channel :" + channelName + " not registered, use addChannel method first");
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A <see cref="JavaScriptObject"/> that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {
            this._ClassicSend(udpData, remoteAddress, remotePort);

        }
        /// <summary>
        /// Add a "white" IP address to the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddWhiteAddress(string address)
        {
            this._whiteList.Add(address);
        }
        /// <summary>
        /// Add a "black" IP address to the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port)</param>
        public void AddBlackAddress(string address)
        {
            this._blackList.Add(address);
        }
        /// <summary>
        /// Remove a "white" IP address from the whitelist</summary>
        /// <remarks>The filter will be effective if <see cref="WhiteListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port). It must be registered on this instance, if that address can't be found on the list the method will throw an exception!<remarks>You can check if the name is registered by calling <see cref="GetWhiteAddressAt(int)"/> and <see cref="WhiteListLength"/></remarks></param>
        public void RemoveWhiteAddress(string address)
        {
            this._whiteList.RemoveAt(this._whiteList.IndexOf(address));
        }
        /// <summary>
        /// Remove a "black" IP address from the blacklist</summary>
        /// <remarks>The filter will be effective if <see cref="BlackListEnabled"/> is set true</remarks>
        /// <param name="address">An IPV4 address (without the port).</param>
        /// <c>It must be registered on this instance, if that address can't be found on the list the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetBlackAddressAt(int)"/> and <see cref="BlackListLength"/></remarks>
        public void RemoveBlackAddress(string address)
        {
            this._blackList.RemoveAt(this._blackList.IndexOf(address));
        }
        /// <summary>
        /// Get the white address at the specified index on the whitelist</summary>
        public string GetWhiteAddressAt(int index)
        {
            return (this._whiteList[index]);
        }
        /// <summary>
        /// Get the black address at the specified index on the blacklist</summary>
        public string GetBlackAddressAt(int index)
        {
            return (this._blackList[index]);
        }
        /// <summary>
        /// Unbind from the localport</summary>
        /// <param name='removeChannels'>Remove all the added UDPChannels, true by default
        /// </param>
        public void Close(bool removeChannels = true)
        {
            this._bound = false;
            if (this._UDPSocketIPv4 != null)
            {

                //_UDPSocketIPv4.EndReceive();
                this._UDPSocketIPv4.Close();
                this._UDPSocketIPv4 = null;
            }
            if (removeChannels)
            {
                while (this._channels.Count > 0)
                {
                    this.RemoveChannel(this._channels[0].Name);
                }
            }
        }
        /// <summary>
        /// Utilitary static method to reset all instances of UDPManager on the program</summary>
        public static void ResetAllUDPManagers(bool removeChannels = true)
        {
            foreach (UDPManager udpm in UDPmanagers)
            {
                udpm.Close(removeChannels);
            }
        }
        /// <summary>
        /// Gets the LAN broadcast address
        /// </summary>
        public static List<IPAddress> GetBroadcastAddresses()
        {
            List<IPAddress> ret = new List<IPAddress>();

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        int addressInt = BitConverter.ToInt32(ip.Address.GetAddressBytes(), 0);
                        int maskInt = BitConverter.ToInt32(ip.IPv4Mask.GetAddressBytes(), 0);
                        int broadcastInt = addressInt | ~maskInt;
                        IPAddress broadcast = new IPAddress(BitConverter.GetBytes(broadcastInt));
                        ret.Add(broadcast);
                    }
                }
            }
            return (ret);
        }
        /// <summary>
        /// True if the instance is bound to a port</summary>
        public bool Bound
        {
            get
            {
                return (_UDPSocketIPv4 != null);
            }
        }
        /// <summary>
        /// Returns the local port on which the UDPManager is bound. Returns 0 if the UDPManager is not bound yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                if (this._UDPSocketIPv4 == null) return (0);
                return (((IPEndPoint)this._UDPSocketIPv4.Client.LocalEndPoint).Port);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance</summary>
        public int NumChannels
        {
            get
            {
                return _channels.Count;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the whitelist should be the only ones to be treated or not</summary><seealso cref="AddWhiteAddress(string)"/>
        public bool WhiteListEnabled
        {
            get
            {
                return _whiteListEnabled;
            }
            set
            {
                _whiteListEnabled = value;
            }
        }
        /// <summary>
        /// Specify if the messages incoming from the addresses added on the blacklist should be ignored or not</summary><seealso cref="AddBlackAddress(string)"/>
        public bool BlackListEnabled
        {
            get
            {
                return _blackListEnabled;
            }
            set
            {
                _blackListEnabled = value;
            }
        }
        /// <summary>
        /// The length of the whitelist</summary>
        public int WhiteListLength
        {
            get
            {
                return _whiteList.Count;
            }
        }
        /// <summary>
        /// The length of the blacklist</summary>
        public int BlackListLength
        {
            get
            {
                return _blackList.Count;
            }
        }

        private void _DeliveredNotifForward(UDPManagerEvent e)
        { //console.log("DATA DELIVERED ",e.udpDataInfo.channelName,e.udpDataInfo.data.messageType);
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_DELIVERED, e.UDPdataInfo));
        }

        private void _RetryNotifForward(UDPManagerEvent e)
        { //console.log("RETRY ",e.udpDataInfo.channelName,e.udpDataInfo.data.messageType);
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RETRIED, e.UDPdataInfo));
        }

        private void _CancelNotifForward(UDPManagerEvent e)
        {

            if (e.UDPdataInfo.ChannelName.Length >= 8 && e.UDPdataInfo.ChannelName.Substring(0, 8) == UDPManager._UDPMRCP + ":")
            {
                int max = this._receivedIDs.Count;
                for (int i = 0; i < max; i++)
                {
                    string[] ar = _receivedIDs[i].Split(',');
                    if (ar[0].Split(':')[0] == e.UDPdataInfo.RemoteAddress && ar[0].Split(':')[1] == e.UDPdataInfo.RemotePort.ToString())
                    {
                        _receivedIDs.RemoveAt(i);
                        i--; max--;
                    }
                }


            }
            //POUR MOI IL FAUT METTRE UN ELSE ICI
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_CANCELED, e.UDPdataInfo));
        }

        private void _ReceiveDataHandler(IAsyncResult ar)
        {
            lock (locker)
            {

                IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes;
                string receivedString;
                #pragma warning disable 168
                try
                {

                    receiveBytes = _UDPSocketIPv4.EndReceive(ar, ref receivedIpEndPoint);

                }
                catch (Exception e)
                {
                    if (_UDPSocketIPv4 != null)
                        _UDPSocketIPv4.BeginReceive(new AsyncCallback(_ReceiveDataHandler), new object());
                    return;
                }
                _UDPSocketIPv4.BeginReceive(new AsyncCallback(_ReceiveDataHandler), new object());
                receivedString = ASCIIEncoding.ASCII.GetString(receiveBytes);
                dynamic udpData;
                try
                {
                    udpData = JsonConvert.DeserializeObject(receivedString);
                }
                catch (NullReferenceException e)
                {

                    this.DispatchEvent(new UDPClassicMessageEvent(UDPClassicMessageEvent.Names.MESSAGE, receivedString, receivedIpEndPoint));
                    return;
                }
                #pragma warning restore 168
                int ID = 0;
                if (udpData["UDPMSID"] != null && udpData["UDPMCN"] != null && udpData["UDPMRA"] != null)
                {

                    ID = int.Parse(udpData["UDPMSID"].ToString());

                    bool boool = true;
                    if (this._whiteListEnabled)
                    {
                        if (this._whiteList.IndexOf(receivedIpEndPoint.Address.ToString()) == -1)
                            boool = false;
                    }
                    else if (this._blackListEnabled)
                    {
                        if (this._blackList.IndexOf(receivedIpEndPoint.Address.ToString()) > -1)
                            boool = false;
                    }

                    if (this._UDPClientConnecting != null && receivedIpEndPoint.Address.ToString() == this._UDPClientConnecting.Address && receivedIpEndPoint.Port.ToString() == this._UDPClientConnecting.ToString() && udpData["UDPMCN"] != "UDPMRCC") boool = false;
                    if (udpData["UDPMCN"].ToString() == "UDPMRCP" && this._udpServerPeers != null && this._udpServerPeers.Where(a => a.Address == receivedIpEndPoint.Address.ToString()).Where(a => a.Port == receivedIpEndPoint.Port).FirstOrDefault() == null) boool = false;

                    if (boool)
                    {

                        if (this._receivedIDs.IndexOf(receivedIpEndPoint.Address.ToString() + ":" + receivedIpEndPoint.Port.ToString() + "," + ID) == -1)
                        {
                            this._receivedIDs.Add(receivedIpEndPoint.Address.ToString() + ":" + receivedIpEndPoint.Port.ToString() + "," + ID);
                            if (this._receivedIDs.Count == 999) this._receivedIDs.RemoveAt(0);
                            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RECEIVED, new UDPDataInfo(udpData["UDPMCN"].ToString(), receivedIpEndPoint.Address.ToString(), receivedIpEndPoint.Port, udpData["data"], ID)));

                        }
                        //else console.log("already received");
                        if (udpData["UDPMRA"] != null)
                        {
                            this._ClassicSend(new { UDPMDRID = ID, UDPMCN = udpData["UDPMCN"] }, receivedIpEndPoint.Address.ToString(), receivedIpEndPoint.Port);        //Send receipt
                        }
                    }
                }
                else if (udpData["UDPMDRID"] != null && udpData["UDPMCN"] != null)
                {
                    ID = int.Parse(udpData["UDPMDRID"].ToString());
                    string cn = udpData["UDPMCN"].ToString();

                    UDPChannel channel;
                    if (udpData["UDPMCN"].ToString() == UDPManager._UDPMRCP)
                        channel = _pingChannel;
                    else if (udpData["UDPMCN"].ToString() == UDPManager._UDPMRCC)
                        channel = _connectionChannel;
                    else if (udpData["UDPMCN"].ToString().Length >= 8 && udpData["UDPMCN"].ToString().Substring(0, 8) == UDPManager._UDPMRCP + ":")
                        channel = this._GetHiddenClientPingChannelByID(udpData["UDPMCN"].ToString().Split(':')[1]);
                    else
                        channel = this.GetChannelByName(udpData["UDPMCN"].ToString());

                    if (channel != null) channel._GetReceipt(ID);
                }
                else
                {

                    this.DispatchEvent(new UDPClassicMessageEvent(UDPClassicMessageEvent.Names.MESSAGE, receivedString, receivedIpEndPoint));
                }
            }


        }



        internal void _AddHiddenClientPingChannel(int peerID)
        { //SERVERSIDE
            UDPChannel pingChannel = new UDPChannel(UDPManager._UDPMRCP + ":" + peerID, true, true, 1000, 10000);
            pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            this._clientsPingChannels.Add(pingChannel);
        }

        internal void _RemoveHiddenClientPingChannel(int peerID)
        { //SERVERSIDE
            UDPChannel pingChannel = this._clientsPingChannels.Where(a => a.Name == UDPManager._UDPMRCP + ":" + peerID).FirstOrDefault();
            this._clientsPingChannels.RemoveAt(this._clientsPingChannels.IndexOf(pingChannel));
            pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            pingChannel._Close();
        }

        internal UDPChannel _GetHiddenClientPingChannelByID(string peerID)
        {
            return (this._clientsPingChannels.Where(a => a.Name == UDPManager._UDPMRCP + ":" + peerID).FirstOrDefault());
        }

        internal void _InitHiddenChannels()
        { //CLIENTSIDE
            this._pingChannel = new UDPChannel(UDPManager._UDPMRCP, true, true, 1000, 10000);
            this._connectionChannel = new UDPChannel(UDPManager._UDPMRCC, true, false, 1000, 10000);
            this._pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            this._pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            this._pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            this._pingChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            this._connectionChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            this._connectionChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            this._connectionChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            this._connectionChannel.AddEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
        }

        internal void _CloseHiddenChannels()
        { //CLIENTSIDE
            this._pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            this._pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            this._pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            this._pingChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            this._pingChannel._Close();
            this._connectionChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent._SEND_DATA, this._SendDataFromChannel);
            this._connectionChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_DELIVERED, this._DeliveredNotifForward);
            this._connectionChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_RETRIED, this._RetryNotifForward);
            this._connectionChannel.RemoveEventListener<UDPManagerEvent>(UDPManagerEvent.Names.DATA_CANCELED, this._CancelNotifForward);
            this._connectionChannel._Close();
        }

        private void _SendDataFromChannel(UDPManagerEvent e)
        {
            object data = new { UDPMSID = e.UDPdataInfo.ID, UDPMCN = e.UDPdataInfo.ChannelName, UDPMRA = (e.Target as UDPChannel).GuarantiesDelivery, data = e.UDPdataInfo.Data };
            this._ClassicSend(data, e.UDPdataInfo.RemoteAddress, e.UDPdataInfo.RemotePort);
            this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_SENT, e.UDPdataInfo));
        }
        private void _ClassicSend(object udpData, string remoteAddress, int remotePort)
        {
            byte[] sendbuf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(udpData));
            _UDPSocketIPv4.Send(sendbuf, sendbuf.Length, new IPEndPoint(IPAddress.Parse(remoteAddress), remotePort));

        }

        private int _GetNextUniqueID()
        {
            return (Convert.ToInt32(new System.Random().NextDouble().ToString().Substring(2, 9), 10));
        }

        public new void AddEventListener<T>(object eventName, Action<T> callBack)
        where T : Event
        {
            base.AddEventListener<T>(eventName, callBack);
            if (eventName.ToString() == UDPManagerEvent.Names.BOUND.ToString() && this._bound) this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.BOUND, null));  //Hack that dispatch the BOUND event if it already occurs when the listener is added
        }

    }
}