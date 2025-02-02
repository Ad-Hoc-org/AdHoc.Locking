// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;

namespace AdHoc.ZooKeeper;
public static partial class Operations
{

    public enum CreateMode
    {
        Persistent = 0,
        Ephemeral = 1,
        Sequential = 1 << 1,
        PersistentSequential = Persistent | Sequential,
        EphemeralSequential = Ephemeral | Sequential,
        TimeToLive = 1 << 2,
        PersistentWithTimeToLive = Persistent | TimeToLive,
    }


    private static byte[] GetAclBytes(List<ACL> acl)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        writer.Write(BinaryPrimitives.ReverseEndianness(acl.Count));
        foreach (var entry in acl)
        {
            writer.Write(BinaryPrimitives.ReverseEndianness(entry.Perms));
            var schemeBytes = Encoding.UTF8.GetBytes(entry.Scheme);
            writer.Write(BinaryPrimitives.ReverseEndianness(schemeBytes.Length));
            writer.Write(schemeBytes);
            var idBytes = Encoding.UTF8.GetBytes(entry.Id);
            writer.Write(BinaryPrimitives.ReverseEndianness(idBytes.Length));
            writer.Write(idBytes);
        }

        return memoryStream.ToArray();
    }
    public class ACL
    {
        public int Perms { get; set; }
        public required string Scheme { get; set; }
        public required string Id { get; set; }
    }
    [Flags]
    public enum Permission
    {
        Read = 1,
        Write = 2,
        Create = 4,
        Delete = 8,
        Admin = 16,
        All = Read | Write | Create | Delete | Admin
    }
}
