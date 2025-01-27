// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;

namespace AdHoc.Locking;
internal static class FileLocks
{

    public const int OpenInterval = 10;
    public const int MinAcquiringInterval = 100;
    public const int MaxAcquiringInterval = 1000;

    public static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(5);

    public static void ThrowIfContainsInvalidFileNameChars(string name, [CallerArgumentExpression(nameof(name))] string? argumentName = null)
    {
        int i = name.IndexOfAny(Path.GetInvalidFileNameChars());
        if (i > -1)
            throw new ArgumentException($"'{argumentName}' can't contain character '{name[i]}' at position {i}");
    }

}
