using System.Net;
using System.Net.Sockets;

namespace Lolcode.Runtime;

internal sealed class LolFileBlob(Stream? stream, bool failed, bool appendWrites = false) : LolBlob
{
    internal Stream? Stream { get; } = stream;
    internal bool HasError { get; set; } = failed;
    internal bool AppendWrites { get; } = appendWrites;

    internal Stream GetStream(string operation)
    {
        ThrowIfClosed(operation);
        if (Stream is null)
            throw new LolRuntimeException($"Cannot {operation} a failed file BLOB handle");
        return Stream;
    }

    protected override void DisposeCore()
    {
        try
        {
            Stream?.Dispose();
        }
        catch
        {
            HasError = true;
        }
    }
}

internal sealed class LolSocketLease(Socket socket) : IDisposable
{
    private Socket? _socket = socket;

    internal Socket GetSocket(string operation) =>
        Volatile.Read(ref _socket)
        ?? throw new LolRuntimeException($"Cannot {operation} a closed socket BLOB handle");

    public void Dispose()
    {
        Socket? socket = Interlocked.Exchange(ref _socket, null);
        if (socket is null)
            return;
        try
        {
            socket.Dispose();
        }
        catch
        {
            // Closing an operating-system socket is best effort and idempotent.
        }
    }
}

internal sealed class LolSocketBlob(LolSocketLease? lease, bool failed) : LolBlob
{
    internal LolSocketLease? Lease { get; } = lease;
    internal bool Failed { get; } = failed;

    internal Socket GetSocket(string operation)
    {
        ThrowIfClosed(operation);
        if (Failed || Lease is null)
            throw new LolRuntimeException($"Cannot {operation} a failed socket BLOB handle");
        return Lease.GetSocket(operation);
    }

    protected override void DisposeCore() => Lease?.Dispose();
}

internal sealed class LolRandomState
{
    private readonly object _gate = new();
    private Random _random = new();

    internal void Seed(int seed)
    {
        lock (_gate)
            _random = new Random(seed);
    }

    internal int Next(int maximum)
    {
        if (maximum <= 0)
            return 0;
        lock (_gate)
            return _random.Next(maximum);
    }
}

internal static class LolLibraries
{
    internal static LolObject? Create(LolScope scope, string name) => name switch
    {
        "STDIO" => CreateStdio(scope),
        "SOCKS" => CreateSocks(scope),
        "STDLIB" => CreateStdlib(scope),
        "STRING" => CreateString(scope),
        _ => null,
    };

    private static LolObject CreateStdio(LolScope scope)
    {
        var library = new LolObject(scope, scope.Caller);
        AddFunction(library, "OPEN", ["filename", "mode"], OpenFile);
        AddFunction(library, "DIAF", ["file"], FileError);
        AddFunction(library, "LUK", ["file", "length"], ReadFile);
        AddFunction(library, "SCRIBBEL", ["file", "data"], WriteFile);
        AddFunction(library, "AGEIN", ["file"], RewindFile);
        AddFunction(library, "CLOSE", ["file"], CloseFile);
        return library;
    }

    private static LolObject CreateSocks(LolScope scope)
    {
        var library = new LolObject(scope, scope.Caller);
        AddFunction(library, "RESOLV", ["addr"], ResolveHost);
        AddFunction(library, "BIND", ["addr", "port"], BindSocket);
        AddFunction(library, "LISTN", ["local"], AcceptSocket);
        AddFunction(library, "KONN", ["local", "addr", "port"], ConnectSocket);
        AddFunction(library, "CLOSE", ["local"], CloseSocket);
        AddFunction(library, "PUT", ["local", "remote", "data"], SendSocket);
        AddFunction(library, "GET", ["local", "remote", "amount"], ReceiveSocket);
        return library;
    }

    private static LolObject CreateStdlib(LolScope scope)
    {
        var state = new LolRandomState();
        var library = new LolObject(scope, scope.Caller);
        AddFunction(library, "MIX", ["seed"], (_, args) =>
        {
            state.Seed(LolRuntime.CastToNumbr(args[0]));
            return null;
        });
        AddFunction(library, "BLOW", ["max"], (_, args) =>
            state.Next(LolRuntime.CastToNumbr(args[0])));
        return library;
    }

    private static LolObject CreateString(LolScope scope)
    {
        var library = new LolObject(scope, scope.Caller);
        AddFunction(library, "LEN", ["string"], (_, args) =>
            LolRuntime.GetYarnBytes(args[0]).Length);
        AddFunction(library, "AT", ["string", "position"], (_, args) =>
        {
            byte[] bytes = LolRuntime.GetYarnBytes(args[0]);
            int position = LolRuntime.CastToNumbr(args[1]);
            return position < 0 || position >= bytes.Length
                ? string.Empty
                : LolRuntime.CreateByteYarn(bytes[position]);
        });
        return library;
    }

    private static void AddFunction(
        LolObject library,
        string name,
        string[] parameterNames,
        Func<LolScope, object?[], object?> implementation)
    {
        var resolvers = parameterNames
            .Select(parameterName =>
                (LolParameterNameResolver)(scope => new LolResolvedSlot(scope, parameterName)))
            .ToArray();
        library.Values[name] = new LolFunction(
            parameterNames.Length,
            (scope, _, arguments, _) => implementation(scope, arguments),
            resolvers);
    }

    private static object OpenFile(LolScope scope, object?[] args)
    {
        string path = LolRuntime.CastToYarn(args[0]);
        string mode = LolRuntime.CastToYarn(args[1]);
        FileStream? stream = null;
        try
        {
            (FileMode fileMode, FileAccess access, bool append) = mode switch
            {
                "r" => (FileMode.Open, FileAccess.Read, false),
                "w" => (FileMode.Create, FileAccess.Write, false),
                "a" => (FileMode.OpenOrCreate, FileAccess.Write, true),
                "r+" => (FileMode.Open, FileAccess.ReadWrite, false),
                "w+" => (FileMode.Create, FileAccess.ReadWrite, false),
                "a+" => (FileMode.OpenOrCreate, FileAccess.ReadWrite, true),
                _ => throw new ArgumentException("Unsupported file mode", nameof(args)),
            };
            stream = new FileStream(path, fileMode, access, FileShare.ReadWrite);
            if (append)
                stream.Seek(0, SeekOrigin.End);
            LolFileBlob blob = scope.Resources.Register(
                new LolFileBlob(stream, failed: false, appendWrites: append));
            stream = null;
            return blob;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or ArgumentException or
                NotSupportedException)
        {
            stream?.Dispose();
            return scope.Resources.Register(new LolFileBlob(stream: null, failed: true));
        }
    }

    private static object FileError(LolScope _, object?[] args)
    {
        LolFileBlob file = RequireFile(args[0]);
        return file.IsClosed || file.Stream is null || file.HasError;
    }

    private static object ReadFile(LolScope _, object?[] args)
    {
        LolFileBlob file = RequireFile(args[0]);
        int length = LolRuntime.CastToNumbr(args[1]);
        if (length <= 0)
            return string.Empty;

        try
        {
            Stream stream = file.GetStream("read");
            byte[] data = new byte[length];
            int read = stream.Read(data, 0, data.Length);
            return read == 0
                ? string.Empty
                : new LolByteYarn(data[..read]);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or NotSupportedException or
                ObjectDisposedException or LolRuntimeException)
        {
            file.HasError = true;
            return string.Empty;
        }
    }

    private static object? WriteFile(LolScope _, object?[] args)
    {
        LolFileBlob file = RequireFile(args[0]);
        byte[] bytes = LolRuntime.GetExplicitYarnBytes(args[1]);
        try
        {
            Stream stream = file.GetStream("write");
            if (file.AppendWrites)
                stream.Seek(0, SeekOrigin.End);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            return null;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or NotSupportedException or
                ObjectDisposedException or LolRuntimeException)
        {
            file.HasError = true;
            return null;
        }
    }

    private static object? RewindFile(LolScope _, object?[] args)
    {
        LolFileBlob file = RequireFile(args[0]);
        try
        {
            file.GetStream("rewind").Seek(0, SeekOrigin.Begin);
            file.HasError = false;
            return null;
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or NotSupportedException or
                ObjectDisposedException or LolRuntimeException)
        {
            file.HasError = true;
            return null;
        }
    }

    private static object? CloseFile(LolScope _, object?[] args)
    {
        RequireFile(args[0]).Dispose();
        return null;
    }

    private static LolFileBlob RequireFile(object? value) =>
        value as LolFileBlob
        ?? throw new LolRuntimeException("Expected a file BLOB handle");

    private static object ResolveHost(LolScope _, object?[] args)
    {
        string host = LolRuntime.CastToYarn(args[0]);
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            IPAddress? address = addresses.FirstOrDefault(
                candidate => candidate.AddressFamily == AddressFamily.InterNetwork)
                ?? addresses.FirstOrDefault();
            return address?.ToString()
                ?? throw new LolRuntimeException($"Unable to resolve host: {host}");
        }
        catch (SocketException ex)
        {
            throw new LolRuntimeException($"Unable to resolve host: {host}", ex);
        }
    }

    private static object BindSocket(LolScope scope, object?[] args)
    {
        string addressText = LolRuntime.CastToYarn(args[0]);
        int port = LolRuntime.CastToNumbr(args[1]);
        Socket? socket = null;
        try
        {
            IPAddress address = ResolveAddress(addressText);
            socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(address, port));
            LolSocketBlob blob = scope.Resources.Register(
                new LolSocketBlob(new LolSocketLease(socket), failed: false));
            socket = null;
            return blob;
        }
        catch (Exception ex) when (
            ex is SocketException or ArgumentException or NotSupportedException)
        {
            socket?.Dispose();
            return scope.Resources.Register(new LolSocketBlob(lease: null, failed: true));
        }
    }

    private static object AcceptSocket(LolScope scope, object?[] args)
    {
        Socket local = RequireSocket(args[0]).GetSocket("accept from");
        try
        {
            local.Listen(10);
            Socket accepted = local.Accept();
            return scope.Resources.Register(
                new LolSocketBlob(new LolSocketLease(accepted), failed: false));
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to accept socket connection", ex);
        }
    }

    private static object ConnectSocket(LolScope scope, object?[] args)
    {
        LolSocketBlob localBlob = RequireSocket(args[0]);
        Socket local = localBlob.GetSocket("connect");
        string addressText = LolRuntime.CastToYarn(args[1]);
        int port = LolRuntime.CastToNumbr(args[2]);
        try
        {
            local.Connect(new IPEndPoint(ResolveAddress(addressText), port));
            return scope.Resources.Register(
                new LolSocketBlob(localBlob.Lease, failed: false));
        }
        catch (Exception ex) when (
            ex is SocketException or ArgumentException or ObjectDisposedException)
        {
            throw new LolRuntimeException("Unable to connect socket", ex);
        }
    }

    private static object CloseSocket(LolScope _, object?[] args)
    {
        LolSocketBlob socket = RequireSocket(args[0]);
        socket.Dispose();
        return socket;
    }

    private static object SendSocket(LolScope _, object?[] args)
    {
        RequireSocket(args[0]).GetSocket("send from");
        Socket remote = RequireSocket(args[1]).GetSocket("send to");
        byte[] data = LolRuntime.GetYarnBytes(args[2]);
        try
        {
            return remote.Send(data);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            return -1;
        }
    }

    private static object ReceiveSocket(LolScope _, object?[] args)
    {
        RequireSocket(args[0]).GetSocket("receive on");
        Socket remote = RequireSocket(args[1]).GetSocket("receive from");
        int amount = LolRuntime.CastToNumbr(args[2]);
        if (amount <= 0)
            return string.Empty;

        byte[] data = new byte[amount];
        try
        {
            int received = remote.Receive(data);
            return received <= 0 ? string.Empty : new LolByteYarn(data[..received]);
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
        {
            return string.Empty;
        }
    }

    private static LolSocketBlob RequireSocket(object? value) =>
        value as LolSocketBlob
        ?? throw new LolRuntimeException("Expected a socket BLOB handle");

    private static IPAddress ResolveAddress(string address)
    {
        if (address == "ANY")
            return IPAddress.Any;
        if (IPAddress.TryParse(address, out IPAddress? parsed))
            return parsed;

        IPAddress[] addresses = Dns.GetHostAddresses(address);
        return addresses.FirstOrDefault(
                candidate => candidate.AddressFamily == AddressFamily.InterNetwork)
            ?? addresses.FirstOrDefault()
            ?? throw new LolRuntimeException($"Unable to resolve host: {address}");
    }
}
