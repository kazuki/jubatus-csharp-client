using System;
using System.Net;

namespace Jubatus.Client
{
    public interface IRPC : IDisposable
    {
        object Invoke (EndPoint remote, string name, params object[] args);
    }
}
