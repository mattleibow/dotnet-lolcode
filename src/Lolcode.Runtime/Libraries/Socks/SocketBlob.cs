using System.Net.Sockets;

namespace Lolcode.Runtime;

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
