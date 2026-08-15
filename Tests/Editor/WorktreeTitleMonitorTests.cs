/*
 * session indexのstampと実効表示を分け、rename時だけEditor更新が必要になることを検証する。
 */

#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;

[TestFixture]
public sealed class WorktreeTitleMonitorTests
{
    private WorktreeTitleTestEnvironment environment;
    private LinkedWorktreeFixture worktree;

    [SetUp]
    public void SetUp()
    {
        environment = new WorktreeTitleTestEnvironment();
        worktree = environment.CreateCodexLinkedWorktree("d00d");
        environment.WriteThreadMetadata(worktree.GitDirectory, "thread-monitor");
    }

    [TearDown]
    public void TearDown()
    {
        environment.Dispose();
    }

    [Test]
    public void Poll_WhenSessionIndexAppearsWithTask_ReportsDisplayChange()
    {
        var monitor = new WorktreeTitleMonitor(worktree.ProjectRoot, environment.CodexHome);
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Initial task\"}"
        );

        bool changed = monitor.Poll();

        Assert.That(changed, Is.True);
        Assert.That(monitor.Current.DisplayText, Is.EqualTo("d00d · Initial task"));
    }

    [Test]
    public void Poll_WhenTaskIsRenamed_ReportsDisplayChange()
    {
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Initial task\"}"
        );
        var monitor = new WorktreeTitleMonitor(worktree.ProjectRoot, environment.CodexHome);
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Renamed task with detail\"}"
        );

        bool changed = monitor.Poll();

        Assert.That(changed, Is.True);
        Assert.That(monitor.Current.DisplayText, Is.EqualTo("d00d · Renamed task with detail"));
    }

    [Test]
    public void Poll_WhenIndexChangesWithoutEffectiveTaskChange_DoesNotReportDisplayChange()
    {
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Stable task\"}"
        );
        var monitor = new WorktreeTitleMonitor(worktree.ProjectRoot, environment.CodexHome);
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Stable task\"}",
            "{\"id\":\"thread-other\",\"thread_name\":\"Other task\"}"
        );

        bool changed = monitor.Poll();

        Assert.That(changed, Is.False);
        Assert.That(monitor.Current.DisplayText, Is.EqualTo("d00d · Stable task"));
    }

    [Test]
    public void Poll_WhenIndexStampIsUnchanged_DoesNotReportDisplayChange()
    {
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Stable task\"}"
        );
        var monitor = new WorktreeTitleMonitor(worktree.ProjectRoot, environment.CodexHome);

        bool changed = monitor.Poll();

        Assert.That(changed, Is.False);
        Assert.That(monitor.Current.DisplayText, Is.EqualTo("d00d · Stable task"));
    }

    [Test]
    public void Poll_WhenSessionIndexIsDeleted_FallsBackToIdAndReportsDisplayChange()
    {
        environment.WriteSessionIndex(
            "{\"id\":\"thread-monitor\",\"thread_name\":\"Initial task\"}"
        );
        var monitor = new WorktreeTitleMonitor(worktree.ProjectRoot, environment.CodexHome);
        File.Delete(environment.SessionIndexPath);

        bool changed = monitor.Poll();

        Assert.That(changed, Is.True);
        Assert.That(monitor.Current.DisplayText, Is.EqualTo("d00d"));
    }
}
#endif
