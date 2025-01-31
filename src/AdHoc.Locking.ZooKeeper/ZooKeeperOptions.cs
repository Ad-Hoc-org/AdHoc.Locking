// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace AdHoc.Locking.ZooKeeper;
public record ZooKeeperOptions
{
    public record Authentication(string Scheme, byte[] Data);


    public string ConnectionString { get => field ?? string.Empty; set; }

    [field: AllowNull]
    public ICollection<Authentication> Authentications { get => field ??= []; set; }

    public TimeSpan ConnectionTimeout { get; set; }

    public TimeSpan SessionTimeout { get; set; }


    public ZooKeeperOptions(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ConnectionString = connectionString;
    }

}
