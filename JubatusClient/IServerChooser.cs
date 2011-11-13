using System.Net;

namespace Jubatus.Client
{
    public interface IServerChooser
    {
        EndPoint Choose();
    }
}
