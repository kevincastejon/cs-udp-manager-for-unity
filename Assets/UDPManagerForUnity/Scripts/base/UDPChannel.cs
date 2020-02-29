using System;
using System.Collections.Generic;

namespace kevincastejon
{
    /// <summary>
    /// A UDPChannel object is used to determine reliability options and handle the queuing, retrying, and/or canceling of messages depending on these options
    /// <remarks>You can't instantiate an UDPChannel directly, instead of that you will use the <see cref="AddChannel"/> methods of UDPManager, UDPClient or UDPServer objects</remarks>
    /// 
    /// Basic usage:
    /// 
    /// <code>
    /// 
    ///         //Adds a channel named "myChannel" that guaranties the delivery by retrying send every 100ms until a receipt is received, and that cancels the sending if no receipt in 2000ms, and that sends a message only when the previous one is or delivered or canceled
    ///         client.AddChannel("myChannel",true,true,100,2000);      //This channel is fully reliable
    /// 
    ///         //Adds a channel named "myOtherChannel" that guaranties the delivery by retrying send every 50ms until a receipt is received, and that cancels the sending if no receipt in 1000ms, and that sends messages without waiting delivery or canceling of the other ones
    ///         server.AddChannel("myOtherChannel",true,false,50,1000);      //This channel is semi reliable
    /// 
    ///         //Adds a channel named "onMoreChannel" that does not guaranty the delivery and that sends messages without waiting delivery or canceling of the other ones
    ///         manager.AddChannel("onMoreChannel",false,false);      //This channel is not reliable
    /// 
    /// <remarks>You can use any reliability coniguration with any of the of three UDPClient, UDPServer or UDPManager</remarks>
    ///         
    /// </code>
    /// 
    /// </summary>
    public class UDPChannel : EventDispatcher
    {
        private bool _guarantiesDelivery;   //if true, asks for receipt and retry (if retryTime>0) until it cancels (if cancelTime>0). If retryTime and cancelTime = 0 it will wait for receipt possibly infinitely, if maintainOrder is true, the channel will be blocked then
        private bool _maintainOrder;            //don't send any more message until the last is or delivered or canceled
        private double _retryTime;              //time in ms before retrying to send message (if it's not delivered yet)
        private double _cancelTime;             //time in ms before canceling the sending (and go to the next if maintainOrder is true)
        private List<UDPDataInfo> _dataBuffer = new List<UDPDataInfo>();            //Array containing messages awaiting to be sent
        private List<UDPDataInfo> _dataWaitingReceipt = new List<UDPDataInfo>();    //Array containing messages awaiting for a receipt (to be delivered)
        private string _name;                   //channel name
        private bool _running;              //if true messages are still being sent (dataBuffer is not empty)
        private bool _closed;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name">A string that represent the name of the channel</param>
        /// <param name='guarantiesDelivery'>If true the messages sent though this channel will wait for a receipt from the target that will guaranty the delivery. It will wait during the time specified on <paramref name="retryTime"/> until what it will retry sending the message, etc... If false the message is sent once without guranty of delivery. Default is false.<remarks>The guaranty of the delivery works only if the target uses the same library (C#,AS3 or JS) to communicate over UDP!</remarks></param>
        /// <param name='maintainOrder'>If true it will wait for a message to be delivered before sending the next one.<remarks> Only works if <paramref name="guarantiesDelivery"/> is true</remarks></param>
        /// <param name='retryTime'>The number of milliseconds the channel will wait before retrying sending the message if not delivered. Default is 30.</param>
        /// <param name='cancelTime'>The number of milliseconds the channel will wait before canceling the message if not delivered. Default is 500.</param>
        internal UDPChannel(string name, bool guarantiesDelivery = false, bool maintainOrder = false, double retryTime = 30, double cancelTime = 1000)
        {
            _name = name;
            _guarantiesDelivery = guarantiesDelivery;
            _maintainOrder = maintainOrder;
            _retryTime = retryTime;
            _cancelTime = cancelTime;
            if (this._guarantiesDelivery == false)
            {
                this._maintainOrder = false; //if guarantiesDelivery is false so is maintainOrder
                this.RetryTime = this.CancelTime = 0;
            }
            if (Double.IsNaN(_cancelTime)) _cancelTime = 0;
            if (Double.IsNaN(_retryTime)) _retryTime = 0;     //0 means no timer (no retry or no cancel)

        }
        /// <summary>
        /// A string that represent the name of the channel
        /// </summary>
        public string Name
        {
            get
            {
                return (this._name);
            }
        }
        /// <summary>
        /// Is true if the channel sends a message only when the previous one is or delivered or canceled
        /// </summary>
        public bool MaintainOrder
        {
            get
            {
                return (this._maintainOrder);
            }
        }
        /// <summary>
        /// Is true if the channel guaranties the delivery by retrying send every <see cref="RetryTime"/> ms until a receipt is received, and that cancels the sending if no receipt in <see cref="CancelTime"/> ms
        /// </summary>
        public bool GuarantiesDelivery
        {
            get
            {
                return (this._guarantiesDelivery);
            }
        }
        /// <summary>
        /// The number of milliseconds the channel will wait before retrying sending the message if not delivered.
        /// </summary>
        public double RetryTime
        {
            get
            {
                return (this._retryTime);
            }
            set
            {
                this._retryTime = value;
            }
        }
        /// <summary>
        /// The number of milliseconds the channel will wait before canceling the message if not delivered.
        /// </summary>
        public double CancelTime
        {
            get
            {
                return (this._cancelTime);
            }
            set
            {
                this._cancelTime = value;
            }
        }
        internal void _AddDataToBuffer(UDPDataInfo dataInfo)
        {
            _dataBuffer.Add(dataInfo);
            if (!_running) _SendNextData();  //if there is no message currently being sent start the sending again
        }
        private void _SendNextData()
        { //send next message in dataBuffer
            if (this._closed == false)
            {
                if (this._dataBuffer.Count > 0)
                {
                    this._running = true;
                    var dataInfo = this._dataBuffer[0]; //
                    this._dataBuffer.RemoveAt(0); //retrieve next message and remove it from dataBufffer

                    if (this._guarantiesDelivery)
                    { //if delivery is guaranteed 
                        this._dataWaitingReceipt.Add(dataInfo); //retry handling
                        dataInfo.AddEventListener<UDPDataEvent>(UDPDataEvent.Names.RETRIED, this._RetryHandler); //
                    }
                    if (this._cancelTime > 0)
                    {
                        //cancel handling

                        dataInfo.AddEventListener<UDPDataEvent>(UDPDataEvent.Names.CANCELED, this._CancelHandler);
                    }
                    dataInfo._Send(this._retryTime, this._cancelTime); //notify the message that he is being sent
                    this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent._SEND_DATA, dataInfo));
                    if (!this._maintainOrder)
                    { //if maintainOrder is false
                        this._SendNextData(); //send next message on buffer right now (don't wait for nor delivery nor cancel)
                    }
                }
                else this._running = false; //if the dataBuffer is empty stop sending, set running false
            }
        }

        private void _RetryHandler(UDPDataEvent e)
        {

            if (this._closed == false)
            {
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_RETRIED, e.Target as UDPDataInfo)); //dispatch public event to UDPManager
                                                                                                                      //e.target._send(this._retryTime,this._cancelTime);
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent._SEND_DATA, e.Target as UDPDataInfo)); //dispatch internal event to UDPManager
            }
        }

        private void _CancelHandler(UDPDataEvent e)
        {
            if (this._closed == false)
            {
                (e.Target as UDPDataInfo)._SetCanceled(true); //notify the message that he is canceled
                if (this._guarantiesDelivery) this._dataWaitingReceipt.RemoveAt(this._dataWaitingReceipt.IndexOf(e.Target as UDPDataInfo)); //remove from waiting receipt array
                this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_CANCELED, e.Target as UDPDataInfo)); //dispatch public event to UDPManager
                if (this._maintainOrder) this._SendNextData(); //send next message if order is maintained
            }
        }

        internal void _GetReceipt(int ID)
        { //investigates the receipt to try to validate a message
            if (this._guarantiesDelivery && this._closed == false)
            {
                var dataInfo = this._GetWaitingReceiptDataInfoByID(ID);
                if (dataInfo != null)
                {
                    this._dataWaitingReceipt.RemoveAt(this._dataWaitingReceipt.IndexOf(dataInfo));
                    dataInfo._SetReceived(true); //notify the message that he is delivered
                    dataInfo.RemoveEventListener<UDPDataEvent>(UDPDataEvent.Names.RETRIED, this._RetryHandler);
                    if (this._cancelTime > 0) dataInfo.RemoveEventListener<UDPDataEvent>(UDPDataEvent.Names.CANCELED, this._CancelHandler);
                    this.DispatchEvent(new UDPManagerEvent(UDPManagerEvent.Names.DATA_DELIVERED, dataInfo)); //dispatch public event to UDPManager
                    if (this._maintainOrder) this._SendNextData(); //send next message is order is maintained
                } //else tracetrace("get receipt for canceled or already received data");
            }
        }

        private UDPDataInfo _GetWaitingReceiptDataInfoByID(int ID)
        { //retrieve a message that is waiting for a receipt by his ID
            int max = this._dataWaitingReceipt.Count;
            for (int i = 0; i < max; i++)
            {
                if (this._dataWaitingReceipt[i].ID == ID) return (this._dataWaitingReceipt[i]);
            }
            return (null);
        }
        
        internal void _Close(){
            this._running = false;
            this._closed = true;
            int max = this._dataWaitingReceipt.Count;
            for (int i = 0; i<max; i++) {
            var dataInf = this._dataWaitingReceipt[i];
            dataInf.Close();
            dataInf.RemoveAllEventListeners();
            }
        }
    }
}