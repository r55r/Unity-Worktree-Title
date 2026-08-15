/*
 * Codex session index の軽量な変更検出に使う、存在状態・更新日時・サイズのsnapshotを表す。
 */

using System;
using System.IO;

internal readonly struct WorktreeTitleFileStamp : IEquatable<WorktreeTitleFileStamp>
{
    private WorktreeTitleFileStamp(bool exists, DateTime lastWriteTimeUtc, long length)
    {
        Exists = exists;
        LastWriteTimeUtc = lastWriteTimeUtc;
        Length = length;
    }

    public bool Exists { get; }

    public DateTime LastWriteTimeUtc { get; }

    public long Length { get; }

    public static bool TryCapture(string path, out WorktreeTitleFileStamp stamp)
    {
        stamp = default;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var file = new FileInfo(path);
            stamp = file.Exists
                ? new WorktreeTitleFileStamp(true, file.LastWriteTimeUtc, file.Length)
                : new WorktreeTitleFileStamp(false, DateTime.MinValue, 0L);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Equals(WorktreeTitleFileStamp other)
    {
        return Exists == other.Exists
            && LastWriteTimeUtc == other.LastWriteTimeUtc
            && Length == other.Length;
    }

    public override bool Equals(object obj)
    {
        return obj is WorktreeTitleFileStamp other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = Exists.GetHashCode();
            hashCode = (hashCode * 397) ^ LastWriteTimeUtc.GetHashCode();
            hashCode = (hashCode * 397) ^ Length.GetHashCode();
            return hashCode;
        }
    }
}
