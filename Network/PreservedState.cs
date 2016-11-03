using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AgCubio
{
    /// <summary>
    /// 
    /// This class create a preserved state of the object
    /// 
    /// Author: Qiaofeng Wang &  Rizwan Mohammud
    /// </summary>
    public class PreservedState
    {
        public const int bufferSize = 1024;
        public Socket socket;
        public byte[] buffer;
        public StringBuilder sb;
        public long uid;
        public Action<PreservedState> callback { get; set; }

        public PreservedState(Action<PreservedState> result)
        {
            callback = result;
            sb = new StringBuilder();
            buffer = new byte[bufferSize];
            uid = 0;
            callback = result;
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }
    }
}