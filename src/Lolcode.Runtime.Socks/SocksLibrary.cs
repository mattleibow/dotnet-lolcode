using System.Net;
using System.Net.Sockets;
using Lolcode.Runtime;
namespace Lolcode.Runtime.Socks;

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

internal static class SocksLibrary
{
    public static string RESOLV(string host)
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

    public static object BIND(LolcodeLibraryContext context, string addressText, int port)
    {
        Socket? socket = null;
        try
        {
            IPAddress address = ResolveAddress(addressText);
            socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(address, port));
            SocketBlob blob = context.RegisterResource(new SocketBlob(new SocketLease(socket), false));
            socket = null;
            return blob;
        }

        catch (Exception ex) when (ex is SocketException or ArgumentException or NotSupportedException)
        {
            socket?.Dispose();
            return context.RegisterResource(new SocketBlob(null, true));
        }
    }

    public static object LISTN(LolcodeLibraryContext context, object local)
    {
        try
        {
            Socket socket = RequireSocket(local).GetSocket("accept from");
            socket.Listen(10);
            return context.RegisterResource(new SocketBlob(new SocketLease(socket.Accept()), false));
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to accept socket connection", ex);
        }
    }

    public static object KONN(LolcodeLibraryContext context, object local, string addressText, int port)
    {
        SocketBlob localBlob = RequireSocket(local);
        try
        {
            localBlob.GetSocket("connect").Connect(new IPEndPoint(ResolveAddress(addressText), port));
            return context.RegisterResource(new SocketBlob(localBlob.Lease?.Acquire(), false));
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to connect socket", ex);
        }
    }

    public static object CLOSE(object local)
    {
        SocketBlob socket = RequireSocket(local);
        socket.Dispose();
        return socket;
    }

    public static int PUT(object local, object remote, object data)
    {
        RequireSocket(local).GetSocket("send from");
        try { return RequireSocket(remote).GetSocket("send to").Send(LolRuntime.GetYarnBytes(data)); }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException) { return -1; }
    }

    public static object GET(object local, object remote, int amount)
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

/// <summary>Creates the official SOCKS provider for static LOLCODE imports.</summary>
public static class SocksLibraryFactory
{
    /// <summary>Creates a scope-bound SOCKS module without runtime discovery.</summary>
    /// <param name="scope">The importing LOLCODE scope.</param>
    /// <returns>The SOCKS module.</returns>
    public static LolObject Create(LolScope scope)
    {
        var builder = new LolcodeLibraryBuilder(scope);
        builder.AddFunction("RESOLV", 1, static (_, arguments) =>
            SocksLibrary.RESOLV(LolRuntime.CastToYarn(arguments[0])));
        builder.AddFunction("BIND", 2, static (context, arguments) =>
            SocksLibrary.BIND(
                context,
                LolRuntime.CastToYarn(arguments[0]),
                LolRuntime.CastToNumbr(arguments[1])));
        builder.AddFunction("LISTN", 1, static (context, arguments) =>
            SocksLibrary.LISTN(context, arguments[0]!));
        builder.AddFunction("KONN", 3, static (context, arguments) =>
            SocksLibrary.KONN(
                context,
                arguments[0]!,
                LolRuntime.CastToYarn(arguments[1]),
                LolRuntime.CastToNumbr(arguments[2])));
        builder.AddFunction("CLOSE", 1, static (_, arguments) => SocksLibrary.CLOSE(arguments[0]!));
        builder.AddFunction("PUT", 3, static (_, arguments) =>
            SocksLibrary.PUT(arguments[0]!, arguments[1]!, arguments[2]!));
        builder.AddFunction("GET", 3, static (_, arguments) =>
            SocksLibrary.GET(arguments[0]!, arguments[1]!, LolRuntime.CastToNumbr(arguments[2])));
        return builder.Build();
    }
}
