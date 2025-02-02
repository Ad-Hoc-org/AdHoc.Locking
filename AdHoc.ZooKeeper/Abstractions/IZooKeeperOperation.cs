// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Buffers;

namespace AdHoc.ZooKeeper.Abstractions;
public interface IZooKeeperOperation<TResult>
{
    public void WriteRequest(
        IBufferWriter<byte> writer,
        Action<IBufferWriter<byte>, OperationCode> writeRequestID
    );

    public TResult ReadResponse(ZooKeeperResponse response);
}
