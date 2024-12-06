// Copyright AdHoc Authors
// SPDX-License-Identifier: MIT

namespace AdHoc.Locking;
internal static class LockFiles
{


    internal static readonly TimeSpan _DefaultTimeToLive = TimeSpan.FromMinutes(5);

    private const int _OpenInterval = 100;


    internal static async ValueTask<FileStream> OpenAsync(string path, bool readOnly, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(path))
            {
                if (directory is not null && !Directory.Exists(directory))
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (IOException) { }
            }

            try
            {
                return readOnly ? File.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)
                    : File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (IOException)
            {
                if (Directory.Exists(path))
                    throw new InvalidDataException($"'{path}' is a directory and can't be used as a lock file.");
                await Task.Delay(_OpenInterval, cancellationToken);
            }
        }
    }

}
