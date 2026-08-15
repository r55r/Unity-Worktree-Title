/*
 * Codex session index のstampが変わったときだけworktree表示を再解決し、実効表示の変更を通知する。
 */

using System;

internal sealed class WorktreeTitleMonitor
{
    private readonly string projectRoot;
    private readonly string codexHome;
    private WorktreeTitleFileStamp processedStamp;
    private bool hasProcessedStamp;

    public WorktreeTitleMonitor(string projectRoot, string codexHome)
    {
        this.projectRoot = projectRoot;
        this.codexHome = codexHome;
        Current = WorktreeTitleResolver.Resolve(projectRoot, codexHome);
    }

    public WorktreeTitleSnapshot Current { get; private set; }

    public bool Poll()
    {
        if (
            !WorktreeTitleFileStamp.TryCapture(
                Current.SessionIndexPath,
                out WorktreeTitleFileStamp currentStamp
            )
        )
        {
            return false;
        }

        if (hasProcessedStamp && processedStamp.Equals(currentStamp))
        {
            return false;
        }

        WorktreeTitleSnapshot next = WorktreeTitleResolver.Resolve(projectRoot, codexHome);
        if (!next.SessionIndexReadSucceeded)
        {
            bool fallbackDisplayChanged = !string.Equals(
                Current.DisplayText,
                next.DisplayText,
                StringComparison.Ordinal
            );
            Current = next;
            hasProcessedStamp = false;
            return fallbackDisplayChanged;
        }

        if (
            !WorktreeTitleFileStamp.TryCapture(
                next.SessionIndexPath,
                out WorktreeTitleFileStamp resolvedStamp
            ) || !currentStamp.Equals(resolvedStamp)
        )
        {
            hasProcessedStamp = false;
            return false;
        }

        bool displayChanged = !string.Equals(
            Current.DisplayText,
            next.DisplayText,
            StringComparison.Ordinal
        );

        Current = next;
        processedStamp = resolvedStamp;
        hasProcessedStamp = true;
        return displayChanged;
    }
}
