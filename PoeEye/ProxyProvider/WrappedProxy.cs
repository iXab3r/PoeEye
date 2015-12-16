namespace ProxyProvider
{
    using System;
    using System.Net;

    internal sealed class WrappedProxy : IWebProxy
    {
        private readonly Uri address;
        private readonly WebProxy proxy;

        private bool Equals(WrappedProxy other)
        {
            return Equals(address, other.address);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is WrappedProxy && Equals((WrappedProxy) obj);
        }

        public override int GetHashCode()
        {
            return address?.GetHashCode() ?? 0;
        }

        public WrappedProxy(Uri address)
        {
            this.address = address;
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            proxy = new WebProxy(address, true);
        }

        public Uri GetProxy(Uri destination)
        {
            return proxy.GetProxy(destination);
        }

        public bool IsBypassed(Uri host)
        {
            return proxy.IsBypassed(host);
        }

        public ICredentials Credentials
        {
            get { return proxy.Credentials; }
            set { proxy.Credentials = value; }
        }

        public override string ToString()
        {
            return $"[WebProxy] {address}";
        }
    }
}