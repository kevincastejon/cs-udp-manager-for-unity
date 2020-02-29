using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kevincastejon
{
    class UDPEndPoint
    {
        private string _address = null;
        private int _port = -1;

        public UDPEndPoint(string address,int port)
        {
            this._address = address;
            this._port = port;
        }

        public string Address
        {
            get
            {
                return (this._address);
            }
            set
            {
                this._address = value;
            }
        }
        public int Port
        {
            get
            {
                return (this._port);
            }
            set
            {
                this._port = value;
            }
        }
    }
}
