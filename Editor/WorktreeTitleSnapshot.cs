/*
 * Unity既定titleのproject部分へ追加する表示値と、その値を更新するCodex indexの状態を保持する。
 */

internal readonly struct WorktreeTitleSnapshot
{
    public WorktreeTitleSnapshot(
        string displayText,
        string sessionIndexPath,
        bool sessionIndexReadSucceeded = true
    )
    {
        DisplayText = displayText;
        SessionIndexPath = sessionIndexPath;
        SessionIndexReadSucceeded = sessionIndexReadSucceeded;
    }

    public string DisplayText { get; }

    public string SessionIndexPath { get; }

    public bool SessionIndexReadSucceeded { get; }

    public static WorktreeTitleSnapshot Empty => new(null, null);
}
