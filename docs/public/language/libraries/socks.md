# SOCKS library

<span class="badge badge-reference">1.4 reference behavior</span>
<span class="badge badge-reference">Host network access</span>

`SOCKS` exposes blocking TCP operations through BLOB handles.

| Slot | Behavior |
| --- | --- |
| `RESOLV YR host` | resolves a host, preferring IPv4 |
| `BIND YR address AN YR port` | returns a bound local socket BLOB; `"ANY"` maps to all local interfaces |
| `LISTN YR local` | listens and blocks until it accepts a connection, returning a remote BLOB |
| `KONN YR local AN YR address AN YR port` | connects and returns an alias BLOB sharing the socket lease |
| `PUT YR local AN YR remote AN YR data` | sends bytes and returns count, or -1 on send failure |
| `GET YR local AN YR remote AN YR amount` | receives bytes; EOF, failure, or nonpositive amount returns `""` |
| `CLOSE YR socket` | idempotently releases that BLOB's lease and returns it |

`KONN` aliases share a socket lease. Closing one alias does not close the
underlying socket until every lease has closed. Accepted sockets use their own
lease. Resources register with the invoking caller scope, even when an escaped
module retains its provider and lexical state.

The calls are intentionally blocking and use the real host network. Apply
process, network, timeout, and trust boundaries outside the compiler. Provider
implementation classes are internal rather than generated public API.
