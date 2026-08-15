/*
 * Unityが構築した既定titleのproject部分へsuffixを挿入し、scene・target・version等をそのまま保つ。
 */

using System;

internal static class WorktreeTitleFormatter
{
    public static string AppendProjectContext(
        string title,
        string projectName,
        string activeSceneName,
        bool isMacEditor,
        string context
    )
    {
        if (
            string.IsNullOrEmpty(title)
            || string.IsNullOrEmpty(projectName)
            || string.IsNullOrEmpty(activeSceneName)
            || string.IsNullOrEmpty(context)
        )
        {
            return title;
        }

        string expectedPrefix = isMacEditor
            ? $"{activeSceneName} - {projectName}"
            : $"{projectName} - {activeSceneName}";
        if (!title.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return title;
        }

        if (
            title.Length > expectedPrefix.Length
            && !title.Substring(expectedPrefix.Length).StartsWith(" - ", StringComparison.Ordinal)
        )
        {
            return title;
        }

        int projectNameEnd = isMacEditor ? expectedPrefix.Length : projectName.Length;
        return title.Insert(projectNameEnd, $" [{context}]");
    }
}
