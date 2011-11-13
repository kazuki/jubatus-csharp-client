using System;

namespace Jubatus.Client
{
    public class JubatusClientExeption : Exception
    {
        public JubatusClientExeption (string msg) : base (msg) {}
    }
}
