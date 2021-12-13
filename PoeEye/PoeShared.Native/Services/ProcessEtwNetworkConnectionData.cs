using System.Net;

namespace PoeShared.Services
{
    public sealed record ProcessEtwNetworkConnectionData
    {
        public ProcessEtwNetworkConnectionData(
            ulong connId)
        {
            ConnId = connId;
        }

        public ulong ConnId { get; }
        
        public IPAddress Destination { get; set; }
        
        public IPAddress Source { get; set; }
        
        public int DestinationPort { get; set; }
        
        public int SourcePort { get; set; }
    }
}