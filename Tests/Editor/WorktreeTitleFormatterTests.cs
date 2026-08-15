/*
 * Unity既定titleのproject部分だけへcontextを挿入し、他の既定情報を維持することを検証する。
 */

#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;

[TestFixture]
public sealed class WorktreeTitleFormatterTests
{
    [Test]
    public void AppendProjectContext_WhenNonMacTitle_PreservesSceneTargetAndVersion()
    {
        const string title = "SampleProject - FlyingGame - Android - Unity (6000.4.1f1)";

        string result = WorktreeTitleFormatter.AppendProjectContext(
            title,
            "SampleProject",
            "FlyingGame",
            false,
            "c0de · Review package"
        );

        Assert.That(
            result,
            Is.EqualTo(
                "SampleProject [c0de · Review package] - FlyingGame - Android - Unity (6000.4.1f1)"
            )
        );
    }

    [Test]
    public void AppendProjectContext_WhenMacSceneMatchesProject_InsertsAfterProjectSlot()
    {
        const string title = "SampleProject - SampleProject - iOS - Unity (6000.4.1f1)";

        string result = WorktreeTitleFormatter.AppendProjectContext(
            title,
            "SampleProject",
            "SampleProject",
            true,
            "main"
        );

        Assert.That(
            result,
            Is.EqualTo("SampleProject - SampleProject [main] - iOS - Unity (6000.4.1f1)")
        );
    }

    [Test]
    public void AppendProjectContext_WhenTitleShapeIsUnknown_LeavesTitleUnchanged()
    {
        const string title = "Custom title from another extension";

        string result = WorktreeTitleFormatter.AppendProjectContext(
            title,
            "SampleProject",
            "FlyingGame",
            false,
            "main"
        );

        Assert.That(result, Is.EqualTo(title));
    }

    [Test]
    public void AppendProjectContext_WhenProjectNameIsFullPath_PreservesPathAndSuffixesSlot()
    {
        string projectPath = Path.GetFullPath(
            Path.Combine("projects", "worktrees", "sample", "SampleProject")
        );
        string title = $"FlyingGame - {projectPath} - iOS - Unity (6000.4.1f1)";

        string result = WorktreeTitleFormatter.AppendProjectContext(
            title,
            projectPath,
            "FlyingGame",
            true,
            "a1b2"
        );

        Assert.That(
            result,
            Is.EqualTo($"FlyingGame - {projectPath} [a1b2] - iOS - Unity (6000.4.1f1)")
        );
    }

    [Test]
    public void AppendProjectContext_WhenTitleAlreadyContainsContext_DoesNotDuplicateSuffix()
    {
        const string title = "FlyingGame - SampleProject [main] - iOS - Unity (6000.4.1f1)";

        string result = WorktreeTitleFormatter.AppendProjectContext(
            title,
            "SampleProject",
            "FlyingGame",
            true,
            "main"
        );

        Assert.That(result, Is.EqualTo(title));
    }
}
#endif
