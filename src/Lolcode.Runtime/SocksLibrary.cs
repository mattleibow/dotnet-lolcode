using System.Net;
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
        if (socket is not null)
            socket.Dispose();
    }
}

internal sealed class SocketBlob(SocketLease? lease, bool failed) : LolBlob
{
    internal SocketLease? Lease { get; } = lease;
    internal bool Failed { get; } = failed;

    internal Socket GetSocket(string operation)
    {
        ThrowIfClosed(operation);
        return !Failed && Lease is not null
            ? Lease.GetSocket(operation)
            : throw new LolRuntimeException($"Cannot {operation} a failed socket BLOB handle");
    }

    protected override void DisposeCore() => Lease?.Dispose();
}

[LolcodeLibrary("SOCKS")]
public sealed class SocksLibrary
{
    /// <summary>Resolves a host address.</summary>
    public string RESOLV(string host)
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            IPAddress? address = addresses.FirstOrDefault(
                static candidate => candidate.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses.FirstOrDefault();
            return address?.ToString()
                ?? throw new LolRuntimeException($"Unable to resolve host: {host}");
        }
        catch (SocketException ex)
        {
            throw new LolRuntimeException($"Unable to resolve host: {host}", ex);
        }
    }

    /// <summary>Binds a socket and returns its BLOB handle.</summary>
    public object BIND(string addressText, int port)
    {
        Socket? socket = null;
        try
        {
            IPAddress address = ResolveAddress(addressText);
            socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(address, port));
            SocketBlob blob = new(new SocketLease(socket), false);
            socket = null;
            return blob;
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException or NotSupportedException)
        {
            socket?.Dispose();
            return new SocketBlob(null, true);
        }
    }

    /// <summary>Accepts a connection from a socket BLOB.</summary>
    public object LISTN(object local)
    {
        try
        {
            Socket socket = RequireSocket(local).GetSocket("accept from");
            socket.Listen(10);
            return new SocketBlob(new SocketLease(socket.Accept()), false);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to accept socket connection", ex);
        }
    }

    /// <summary>Connects a socket BLOB.</summary>
    public object KONN(object local, string addressText, int port)
    {
        SocketBlob localBlob = RequireSocket(local);
        try
        {
            localBlob.GetSocket("connect").Connect(new IPEndPoint(ResolveAddress(addressText), port));
            return new SocketBlob(localBlob.Lease?.Acquire(), false);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to connect socket", ex);
        }
    }

    /// <summary>Closes a socket BLOB.</summary>
    public object CLOSE(object local)
    {
        SocketBlob socket = RequireSocket(local);
        socket.Dispose();
        return socket;
    }

    /// <summary>Sends data through socket BLOBs.</summary>
    public int PUT(object local, object remote, object data)
    {
        RequireSocket(local).GetSocket("send from");
        try { return RequireSocket(remote).GetSocket("send to").Send(LolRuntime.GetYarnBytes(data)); }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException) { return -1; }
    }

    /// <summary>Receives data through socket BLOBs.</summary>
    public object GET(object local, object remote, int amount)
    {
        RequireSocket(local).GetSocket("receive on");
        if (amount <= 0)
            return string.Empty;
        byte[] data = new byte[amount];
        try
        {
            int received = RequireSocket(remote).GetSocket("receive from").Receive(data);
            return received <= 0 ? string.Empty : LolRuntime.CreateByteYarn(data[..received]);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException) { return string.Empty; }
    }

    private static SocketBlob RequireSocket(object? value) =>
        value as SocketBlob ?? throw new LolRuntimeException("Expected a socket BLOB handle");

    private static IPAddress ResolveAddress(string address)
    {
        if (address == "ANY")
            return IPAddress.Any;
        if (IPAddress.TryParse(address, out IPAddress? parsed))
            return parsed;
        IPAddress[] addresses = Dns.GetHostAddresses(address);
        return addresses.FirstOrDefault(static candidate => candidate.AddressFamily == AddressFamily.InterNetwork)
            ?? addresses.FirstOrDefault()
            ?? throw new LolRuntimeException($"Unable to resolve host: {address}");
    }
}
