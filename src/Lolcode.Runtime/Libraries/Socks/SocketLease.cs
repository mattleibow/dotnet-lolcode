using System.Net.Sockets;

namespace Lolcode.Runtime;

internal sealed class SocketLease(Socket socket) : IDisposable
{
    private readonly object _gate = new();
    private Socket? _socket = socket;
    private int _references = 1;

    internal Socket GetSocket(string operation)
    {
        lock (_gate)
        {
            return _socket
                ?? throw new LolRuntimeException($"Cannot {operation} a closed socket BLOB handle");
        }
    }

    internal SocketLease Acquire()
    {
        lock (_gate)
        {
            if (_socket is null)
                throw new LolRuntimeException("Cannot share a closed socket BLOB handle");
            checked { _references++; }
            return this;
        }
    }

    public void Dispose()
    {
        Socket? socket = null;
        lock (_gate)
        {
            if (_references == 0 || --_references != 0)
                return;
            socket = _socket;
            _socket = null;
        }
        socket?.Dispose();
    }
}
