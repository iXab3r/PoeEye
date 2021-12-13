using System.Collections.Generic;

namespace PoeShared.Services
{
    public interface IProcessEtwData
    {
        public int ProcessId { get; }

        public string ProcessName { get; }
        
        public IDictionary<ulong, ProcessEtwNetworkConnectionData> Connections { get; } 
    }
}