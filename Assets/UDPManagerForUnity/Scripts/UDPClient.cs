using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using _UDPClient = kevincastejon.UDPClient;

namespace kevincastejon.unity
{
    [Serializable]
    public class UUDPClientEvent : UnityEvent<UDPClientEvent> { }
    [Serializable]
    public class UDPClientListeners
    {
        public UUDPManagerEvent OnBound;
        public UUDPClientEvent OnConnectedToServer;
        public UUDPClientEvent OnConnectionFailed;
        public UUDPClientEvent OnServerPong;
        public UUDPClientEvent OnServerSentData;
        public UUDPClientEvent OnServerTimedOut;
    }
    public class UDPClient : MonoBehaviour
    {
        public bool autoConnect;
        public string serverAddress;
        public int serverPort;
        public int localPort;
        public UDPClientListeners listeners;
        public List<UChannel> channels;
        _UDPClient udpc;
        void Reset()
        {
            channels = new List<UChannel>()
        {
            new UChannel("myChannelName")
        };
        }
        // Start is called before the first frame update
        void Start()
        {
            udpc = new _UDPClient(localPort);
            udpc.DefaultTarget = this;
            //Add listeners on the instance of UDPClient
            udpc.On<UDPManagerEvent>(UDPManagerEvent.Names.BOUND, (UDPManagerEvent e) =>
            {
                listeners.OnBound?.Invoke(e);
            });
            udpc.On<UDPClientEvent>(UDPClientEvent.Names.CONNECTED_TO_SERVER, (UDPClientEvent e) =>
            {
                listeners.OnConnectedToServer.Invoke(e);
            });
            udpc.On(UDPClientEvent.Names.CONNECTION_FAILED, (UDPClientEvent e) =>
            {
                listeners.OnConnectionFailed.Invoke(e);
            });
            udpc.On<UDPClientEvent>(UDPClientEvent.Names.SERVER_PONG, (UDPClientEvent e) =>
            {
                listeners.OnServerPong.Invoke(e);
            });
            udpc.On<UDPClientEvent>(UDPClientEvent.Names.SERVER_SENT_DATA, (UDPClientEvent e) =>
            {
                listeners.OnServerSentData.Invoke(e);
            });
            udpc.On<UDPClientEvent>(UDPClientEvent.Names.SERVER_TIMED_OUT, (UDPClientEvent e) =>
            {
                listeners.OnServerTimedOut.Invoke(e);
            });

            channels.ForEach((UChannel uc) =>
            {
                udpc.AddChannel(uc.Name, uc.GuarantiesDelivery, uc.MaintainOrder, uc.RetryTime, uc.CancelTime);
            });

            if (autoConnect)
            {
                udpc.Connect(serverAddress, serverPort, localPort);
            }
        }
        // Update is called once per frame
        void Update()
        {

        }
        /// <summary>
        /// Adds an event listener on the instance that will call the <paramref name="callBack"/> method when an event with the name <paramref name="eventName"/> is dispatched
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="name">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void On<T>(object name, Action<T> callBack)
        where T : Event
        {
            AddEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Adds an event listener on the instance that will call the <paramref name="callBack"/> method when an event with the name <paramref name="eventName"/> is dispatched
        /// </summary>
        /// <typeparam name="T">Any type inherited of Event or Event itself</typeparam>
        /// <param name="name">A string that represents the name of the event</param>
        /// <param name="callBack">A method that accepts as unique parameter an instance of the type specified in <typeparamref name="T"/> and that will be called when an event with the name specified in <paramref name="eventName"/> is dispatched</param>
        public void AddEventListener<T>(object name, Action<T> callBack)
        where T : Event
        {
            udpc.AddEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Removes a previously added event listener on this instance, stop invoking the callback method when the event is dispatched
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callBack"></param>
        public void Off<T>(object name, Action<T> callBack)
        where T : Event
        {
            RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Removes a previously added event listener on this instance, stop invoking the callback method when the event is dispatched
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callBack"></param>
        public void RemoveEventListener<T>(object name, Action<T> callBack)
        where T : Event
        {
            udpc.RemoveEventListener<T>(name, callBack);
        }
        /// <summary>
        /// Dispatches an event, call all the callback methods registered with <see cref="AddEventListener{T}(object, Action{T})"/> for this event (depending on its name) and pass this event object as parameter to these methods
        /// </summary>
        /// <param name="e">An Event or inherited object that will be pass as parameter to the callback methods registered to this event (depending on its name)</param>
        public void DispatchEvent(Event e)
        {
            udpc.DispatchTargetedEvent(e, this);
        }
        /// <summary>
        /// Removes all event listeners registered to the specified <paramref name="eventName"/>. If not provided or null, then all event listeners of all type will be removed on this instance.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveAllEventListeners(object name = null)
        {
            udpc.RemoveAllEventListeners(name);
        }
        /// <summary>
        /// Returns true if an event listener of the specified type has been added with <see cref="AddEventListener{T}(object, Action{T})"/>
        /// </summary>
        /// <param name="name">A string that represents the name of the event</param>
        /// <returns></returns>
        public bool HasEventListener(object name)
        {
            return (udpc.HasEventListener(name));
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
            udpc.Connect(serverAddress, serverPort, localPort);
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
            udpc.AddChannel(channelName, guarantiesDelivery, maintainOrder, retryTime, cancelTime);
        }
        /// <summary>
        /// Removes a UDPChannel on the instance</summary>
        /// <param name='channelName'>The name of the channel you want to remove.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        public void RemoveChannel(string channelName)
        {
            udpc.RemoveChannel(channelName);
        }
        /// <summary>
        /// Get a registered UDPChannel at the specified index</summary>
        public UDPChannel GetChannelAt(int num)
        {
            return (udpc.GetChannelAt(num));
        }
        /// <summary>
        /// Get a registered UDPChannel by specifying his name</summary>
        public UDPChannel GetChannelByName(string channelName)
        {
            return (udpc.GetChannelByName(channelName));
        }
        /// <summary>
        /// Returns the server IPV4 address</summary>
        public string GetServerAddress()
        {
            return (udpc.GetServerAddress());
        }
        /// <summary>
        /// Returns the server port</summary>
        public int GetServerPort()
        {
            return (udpc.GetServerPort());
        }
        /// <summary>
        /// Send data through an UDPChannel to the server and returns an <see cref="UDPDataInfo"/> object.</summary>
        /// <param name='channelName'>The name of the channel you want to send your message through.</param>
        /// <c>It must be registered on this instance, if a channel with that name can't be found the method will throw an exception!</c>
        /// <remarks>You can check if the name is registered by calling <see cref="GetChannelByName"/></remarks>
        /// <param name="udpData">A literal object that contains the data to send</param>
        public UDPDataInfo SendToServer(string channelName, object udpData)
        {
            return (udpc.SendToServer(channelName, udpData));
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
            return (udpc.SendToNonServerPeer(channelName, udpData, remoteAddress, remotePort));
        }
        /// <summary>
        /// Send data "classicaly" to a distant user (means no UDPManager features are usable)</summary>
        /// <param name="udpData">A literal object that contains the data to send</param>
        /// <param name="remoteAddress">The IPV4 address of the target</param>
        /// <param name="remotePort">The port of the target</param>
        public void SendOutOfChannels(object udpData, string remoteAddress, int remotePort)
        {   //send to one client
            udpc.SendOutOfChannels(udpData, remoteAddress, remotePort);
        }
        /// <summary>
        /// Resets the UDPClient. Means disconnect, unbind, and remove the UDPChannels if you specify the parameter as true</summary>
        /// <param name='removeChannels'></param>
        public void Close(bool removeChannels = true)
        {
            udpc.Close();
        }
        /// <summary>
        /// True if the instance is bound to a port</summary>
        public bool Bound
        {
            get
            {
                return (udpc.Bound);
            }
        }

        /// <summary>
        /// Returns the local port on which the UDPClient is bound. Returns 0 if the UDPClient is not bound yet.
        /// </summary>
        public int BoundPort
        {
            get
            {
                return (udpc.BoundPort);
            }
        }
        /// <summary>
        /// True if the instance is connected to a server instance</summary>
        public bool Connected
        {
            get
            {
                return (udpc.Connected);
            }
        }
        /// <summary>
        /// True if the instance is connected to a server instance</summary>
        public bool Connecting
        {
            get
            {
                return (udpc.Connecting);
            }
        }
        /// <summary>
        /// The number of UDPChannel registered on the instance
        /// </summary>
        public int NumChannels
        {
            get
            {
                return (udpc.NumChannels);
            }
        }
        private void OnDestroy()
        {
            udpc.RemoveAllEventListeners();
            udpc.Close();
        }
    }
}