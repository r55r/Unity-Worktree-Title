/*
 * project root を含む Git worktree と Codex metadata を読み、Editor title 用の表示値へ変換する。
 * Codex のローカル形式は非公開なので、すべての読み取り失敗を通常のworktree表示へ閉じ込める。
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

internal static class WorktreeTitleResolver
{
    internal const int MaximumDisplayTextElements = 48;

    public static WorktreeTitleSnapshot Resolve(string projectRoot, string codexHome)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return WorktreeTitleSnapshot.Empty;
            }

            string normalizedProjectRoot = Path.GetFullPath(projectRoot);
            if (
                !Directory.Exists(normalizedProjectRoot)
                || !TryResolveGitEntry(
                    normalizedProjectRoot,
                    out string checkoutRoot,
                    out string gitEntryPath
                )
            )
            {
                return WorktreeTitleSnapshot.Empty;
            }

            if (Directory.Exists(gitEntryPath))
            {
                return new WorktreeTitleSnapshot("main", null);
            }

            GitFileCheckoutKind checkoutKind = ResolveGitFileCheckout(
                gitEntryPath,
                out string gitDirectory
            );
            if (checkoutKind == GitFileCheckoutKind.Primary)
            {
                return new WorktreeTitleSnapshot("main", null);
            }

            if (checkoutKind != GitFileCheckoutKind.Linked)
            {
                return WorktreeTitleSnapshot.Empty;
            }

            string worktreeFolderName = SanitizeDisplayText(
                Path.GetFileName(checkoutRoot.TrimEnd(PathSeparators))
            );
            if (string.IsNullOrEmpty(worktreeFolderName))
            {
                return WorktreeTitleSnapshot.Empty;
            }

            string codexWorktreeId = ResolveCodexWorktreeId(checkoutRoot, codexHome);
            if (string.IsNullOrEmpty(codexWorktreeId))
            {
                return new WorktreeTitleSnapshot(worktreeFolderName, null);
            }

            string sessionIndexPath = Path.Combine(codexHome, "session_index.jsonl");
            string taskName = ResolveCodexTaskName(
                gitDirectory,
                sessionIndexPath,
                out bool sessionIndexReadSucceeded
            );
            string displayText = string.IsNullOrEmpty(taskName)
                ? codexWorktreeId
                : $"{codexWorktreeId} · {taskName}";
            return new WorktreeTitleSnapshot(
                displayText,
                sessionIndexPath,
                sessionIndexReadSucceeded
            );
        }
        catch (Exception)
        {
            return WorktreeTitleSnapshot.Empty;
        }
    }

    public static string ResolveDefaultCodexHome()
    {
        string configuredHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(configuredHome))
        {
            return Path.GetFullPath(configuredHome);
        }

        string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userHome)
            ? null
            : Path.Combine(Path.GetFullPath(userHome), ".codex");
    }

    public static string NormalizeTaskName(string value)
    {
        string text = SanitizeDisplayText(value);
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text.Length <= MaximumDisplayTextElements)
        {
            return text;
        }

        int[] textElementOffsets = ParseExtendedTextElementOffsets(text);
        if (textElementOffsets.Length <= MaximumDisplayTextElements)
        {
            return text;
        }

        int ellipsisOffset = textElementOffsets[MaximumDisplayTextElements - 1];
        return text.Substring(0, ellipsisOffset) + "…";
    }

    private static string SanitizeDisplayText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var normalized = new StringBuilder(value.Length);
        bool hasPendingSpace = false;

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                hasPendingSpace = normalized.Length > 0;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                continue;
            }

            bool hasSurrogatePair = char.IsHighSurrogate(character);
            if (
                hasSurrogatePair
                && (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
            )
            {
                continue;
            }

            if (hasPendingSpace)
            {
                normalized.Append(' ');
                hasPendingSpace = false;
            }

            normalized.Append(character);
            if (hasSurrogatePair)
            {
                normalized.Append(value[++index]);
            }
        }

        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized.ToString();
    }

    private static bool TryResolveGitEntry(
        string projectRoot,
        out string checkoutRoot,
        out string gitEntryPath
    )
    {
        checkoutRoot = null;
        gitEntryPath = null;

        for (
            var directory = new DirectoryInfo(projectRoot);
            directory != null;
            directory = directory.Parent
        )
        {
            string candidatePath = Path.Combine(directory.FullName, ".git");
            if (!Directory.Exists(candidatePath) && !File.Exists(candidatePath))
            {
                continue;
            }

            checkoutRoot = directory.FullName;
            gitEntryPath = candidatePath;
            return true;
        }

        return false;
    }

    private static GitFileCheckoutKind ResolveGitFileCheckout(
        string gitEntryPath,
        out string gitDirectory
    )
    {
        gitDirectory = null;

        try
        {
            string gitFile = ReadSharedTextFile(gitEntryPath).Trim();
            int lineBreakIndex = gitFile.IndexOfAny(new[] { '\r', '\n' });
            if (lineBreakIndex >= 0)
            {
                gitFile = gitFile.Substring(0, lineBreakIndex).Trim();
            }

            const string gitDirectoryPrefix = "gitdir:";
            if (!gitFile.StartsWith(gitDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return GitFileCheckoutKind.Unknown;
            }

            string configuredGitDirectory = gitFile.Substring(gitDirectoryPrefix.Length).Trim();
            if (string.IsNullOrEmpty(configuredGitDirectory))
            {
                return GitFileCheckoutKind.Unknown;
            }

            string gitEntryDirectory = Path.GetDirectoryName(gitEntryPath);
            string resolvedGitDirectory = ResolveConfiguredPath(
                gitEntryDirectory,
                configuredGitDirectory
            );

            if (!Directory.Exists(resolvedGitDirectory))
            {
                return GitFileCheckoutKind.Unknown;
            }

            string commonDirectoryFile = Path.Combine(resolvedGitDirectory, "commondir");
            if (!File.Exists(commonDirectoryFile))
            {
                gitDirectory = resolvedGitDirectory;
                return GitFileCheckoutKind.Primary;
            }

            string configuredCommonDirectory = ReadSharedTextFile(commonDirectoryFile).Trim();
            if (string.IsNullOrEmpty(configuredCommonDirectory))
            {
                return GitFileCheckoutKind.Unknown;
            }

            string resolvedCommonDirectory = ResolveConfiguredPath(
                resolvedGitDirectory,
                configuredCommonDirectory
            );
            if (!Directory.Exists(resolvedCommonDirectory))
            {
                return GitFileCheckoutKind.Unknown;
            }

            gitDirectory = resolvedGitDirectory;
            return GitFileCheckoutKind.Linked;
        }
        catch (Exception)
        {
            return GitFileCheckoutKind.Unknown;
        }
    }

    private static string ResolveConfiguredPath(string baseDirectory, string configuredPath)
    {
        string path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(baseDirectory, configuredPath);
        return Path.GetFullPath(path);
    }

    private static string ResolveCodexWorktreeId(string projectRoot, string codexHome)
    {
        if (string.IsNullOrWhiteSpace(codexHome))
        {
            return null;
        }

        try
        {
            string worktreesRoot = Path.GetFullPath(Path.Combine(codexHome, "worktrees"))
                .TrimEnd(PathSeparators);
            string normalizedProjectRoot = Path.GetFullPath(projectRoot).TrimEnd(PathSeparators);
            string worktreesPrefix = worktreesRoot + Path.DirectorySeparatorChar;
            StringComparison comparison =
                Path.DirectorySeparatorChar == '\\'
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            if (!normalizedProjectRoot.StartsWith(worktreesPrefix, comparison))
            {
                return null;
            }

            string relativeProjectPath = normalizedProjectRoot.Substring(worktreesPrefix.Length);
            string[] segments = relativeProjectPath.Split(
                PathSeparators,
                StringSplitOptions.RemoveEmptyEntries
            );
            if (segments.Length < 1)
            {
                return null;
            }

            return SanitizeDisplayText(segments[0]);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ResolveCodexTaskName(
        string gitDirectory,
        string sessionIndexPath,
        out bool sessionIndexReadSucceeded
    )
    {
        sessionIndexReadSucceeded = true;

        try
        {
            string threadMetadataPath = Path.Combine(gitDirectory, "codex-thread.json");
            if (!TryReadJson(threadMetadataPath, out CodexThreadMetadata metadata))
            {
                return null;
            }

            string ownerThreadId = metadata.ownerThreadId?.Trim();
            if (string.IsNullOrEmpty(ownerThreadId) || !File.Exists(sessionIndexPath))
            {
                return null;
            }

            string latestTaskName = null;

            using var stream = OpenSharedTextFile(sessionIndexPath);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);

            while (reader.ReadLine() is { } line)
            {
                if (!TryParseJson(line, out CodexSessionIndexEntry entry))
                {
                    continue;
                }

                if (!string.Equals(entry.id, ownerThreadId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (entry.thread_name == null)
                {
                    continue;
                }

                latestTaskName = entry.thread_name;
            }

            return NormalizeTaskName(latestTaskName);
        }
        catch (Exception)
        {
            sessionIndexReadSucceeded = false;
            return null;
        }
    }

    private static bool TryReadJson<T>(string path, out T value)
        where T : class
    {
        value = null;

        try
        {
            return TryParseJson(ReadSharedTextFile(path), out value);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryParseJson<T>(string json, out T value)
        where T : class
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonUtility.FromJson<T>(json);
            return value != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string ReadSharedTextFile(string path)
    {
        using var stream = OpenSharedTextFile(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return reader.ReadToEnd();
    }

    private static FileStream OpenSharedTextFile(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete
        );
    }

    private static int[] ParseExtendedTextElementOffsets(string text)
    {
        int[] baseOffsets = StringInfo.ParseCombiningCharacters(text);
        if (baseOffsets.Length < 2)
        {
            return baseOffsets;
        }

        var extendedOffsets = new List<int>(baseOffsets.Length);
        int elementIndex = 0;

        while (elementIndex < baseOffsets.Length)
        {
            extendedOffsets.Add(baseOffsets[elementIndex]);
            bool startsWithRegionalIndicator = IsRegionalIndicator(
                GetCodePointAt(text, baseOffsets[elementIndex])
            );
            elementIndex++;

            if (
                startsWithRegionalIndicator
                && elementIndex < baseOffsets.Length
                && IsRegionalIndicator(GetCodePointAt(text, baseOffsets[elementIndex]))
            )
            {
                elementIndex++;
            }

            while (elementIndex < baseOffsets.Length)
            {
                int elementOffset = baseOffsets[elementIndex];
                int codePoint = GetCodePointAt(text, elementOffset);
                if (IsExtendedElementContinuation(text, elementOffset, codePoint))
                {
                    elementIndex++;
                    continue;
                }

                if (codePoint == 0x200D)
                {
                    elementIndex++;
                    if (elementIndex < baseOffsets.Length)
                    {
                        elementIndex++;
                    }

                    continue;
                }

                if (ElementEndsWithZeroWidthJoiner(text, baseOffsets, elementIndex - 1))
                {
                    elementIndex++;
                    continue;
                }

                break;
            }
        }

        return extendedOffsets.ToArray();
    }

    private static bool IsExtendedElementContinuation(string text, int offset, int codePoint)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, offset);
        return category == UnicodeCategory.NonSpacingMark
            || category == UnicodeCategory.SpacingCombiningMark
            || category == UnicodeCategory.EnclosingMark
            || codePoint is 0xFE0E or 0xFE0F
            || codePoint is >= 0xE0100 and <= 0xE01EF
            || codePoint is >= 0x1F3FB and <= 0x1F3FF
            || codePoint is >= 0xE0020 and <= 0xE007F;
    }

    private static bool IsRegionalIndicator(int codePoint)
    {
        return codePoint is >= 0x1F1E6 and <= 0x1F1FF;
    }

    private static bool ElementEndsWithZeroWidthJoiner(
        string text,
        IReadOnlyList<int> offsets,
        int elementIndex
    )
    {
        if (elementIndex < 0)
        {
            return false;
        }

        int elementEnd = elementIndex + 1 < offsets.Count ? offsets[elementIndex + 1] : text.Length;
        int finalCodePointOffset = elementEnd - 1;
        if (
            finalCodePointOffset > offsets[elementIndex]
            && char.IsLowSurrogate(text[finalCodePointOffset])
        )
        {
            finalCodePointOffset--;
        }

        return GetCodePointAt(text, finalCodePointOffset) == 0x200D;
    }

    private static int GetCodePointAt(string text, int offset)
    {
        char character = text[offset];
        return char.IsHighSurrogate(character)
            ? char.ConvertToUtf32(character, text[offset + 1])
            : character;
    }

    private static readonly char[] PathSeparators =
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar,
    };

    private enum GitFileCheckoutKind
    {
        Unknown,
        Primary,
        Linked,
    }

    [Serializable]
    private sealed class CodexThreadMetadata
    {
        public string ownerThreadId = string.Empty;
    }

    [Serializable]
    private sealed class CodexSessionIndexEntry
    {
        public string id = string.Empty;
        public string thread_name = null;
    }
}
