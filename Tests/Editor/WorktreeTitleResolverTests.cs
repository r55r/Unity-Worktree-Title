/*
 * Git worktree分類、Codex metadata fallback、task名の正規化という表示contractを検証する。
 */

#if UNITY_INCLUDE_TESTS
using System.Globalization;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class WorktreeTitleResolverTests
{
    private WorktreeTitleTestEnvironment environment;

    [SetUp]
    public void SetUp()
    {
        environment = new WorktreeTitleTestEnvironment();
    }

    [TearDown]
    public void TearDown()
    {
        environment.Dispose();
    }

    [Test]
    public void Resolve_WhenPrimaryCheckout_ReturnsMainIdentifier()
    {
        string projectRoot = environment.CreatePrimaryCheckout("PrimaryProject");

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            projectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("main"));
        Assert.That(result.SessionIndexPath, Is.Null);
    }

    [Test]
    public void Resolve_WhenCodexLinkedWorktreeHasTask_ReturnsIdAndTaskName()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("c0de");
        environment.WriteThreadMetadata(worktree.GitDirectory, "thread-alpha");
        environment.WriteSessionIndex(
            "{\"id\":\"thread-alpha\",\"thread_name\":\"Review package integration\"}"
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("c0de · Review package integration"));
        Assert.That(result.SessionIndexPath, Is.EqualTo(environment.SessionIndexPath));
    }

    [Test]
    public void Resolve_WhenNamedWorktreeUsesRelativeGitDirectory_ReturnsFolderName()
    {
        LinkedWorktreeFixture worktree = environment.CreateNamedLinkedWorktree(
            "feature-shop",
            true
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("feature-shop"));
        Assert.That(result.SessionIndexPath, Is.Null);
    }

    [Test]
    public void Resolve_WhenNamedWorktreeFolderExceedsTaskLimit_DoesNotTruncateIdentifier()
    {
        string folderName = new string('w', 60);
        LinkedWorktreeFixture worktree = environment.CreateNamedLinkedWorktree(folderName, false);

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo(folderName));
    }

    [Test]
    public void Resolve_WhenCodexThreadMetadataIsMissing_FallsBackToId()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("b10c");

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("b10c"));
    }

    [Test]
    public void Resolve_WhenCodexThreadMetadataIsInvalid_FallsBackToId()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("b10c");
        environment.WriteRawThreadMetadata(worktree.GitDirectory, "{not-json");

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("b10c"));
    }

    [Test]
    public void Resolve_WhenSessionIndexContainsInvalidAndDuplicateEntries_UsesLastValidMatch()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("f00d");
        environment.WriteThreadMetadata(worktree.GitDirectory, "thread-beta");
        environment.WriteSessionIndex(
            "not-json",
            "{\"id\":\"thread-beta\",\"thread_name\":\"Old task\"}",
            "{\"id\":\"thread-other\",\"thread_name\":\"Other task\"}",
            "{\"id\":\"thread-beta\",\"thread_name\":\"New task\"}"
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("f00d · New task"));
    }

    [Test]
    public void Resolve_WhenLatestMatchingTaskNameIsEmpty_FallsBackToId()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("f00d");
        environment.WriteThreadMetadata(worktree.GitDirectory, "thread-gamma");
        environment.WriteSessionIndex(
            "{\"id\":\"thread-gamma\",\"thread_name\":\"Old task\"}",
            "{\"id\":\"thread-gamma\",\"thread_name\":\"\"}"
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("f00d"));
    }

    [Test]
    public void Resolve_WhenLaterMatchingEntryOmitsTaskName_KeepsLastValidTaskName()
    {
        LinkedWorktreeFixture worktree = environment.CreateCodexLinkedWorktree("f00d");
        environment.WriteThreadMetadata(worktree.GitDirectory, "thread-delta");
        environment.WriteSessionIndex(
            "{\"id\":\"thread-delta\",\"thread_name\":\"Valid task\"}",
            "{\"id\":\"thread-delta\"}"
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            worktree.ProjectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("f00d · Valid task"));
    }

    [Test]
    public void Resolve_WhenCheckoutIsNotGitManaged_ReturnsNoContext()
    {
        string projectRoot = System.IO.Path.Combine(environment.RootPath, "NoGitProject");
        System.IO.Directory.CreateDirectory(projectRoot);

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            projectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.Null);
        Assert.That(result.SessionIndexPath, Is.Null);
    }

    [Test]
    public void Resolve_WhenPrimaryCheckoutUsesSeparateGitDirectory_ReturnsMainIdentifier()
    {
        string projectRoot = environment.CreateCheckoutWithSeparateGitDirectory(
            "SeparateGitProject"
        );

        WorktreeTitleSnapshot result = WorktreeTitleResolver.Resolve(
            projectRoot,
            environment.CodexHome
        );

        Assert.That(result.DisplayText, Is.EqualTo("main"));
    }

    [Test]
    public void NormalizeDisplayText_WhenWhitespaceAndControlsRepeat_CollapsesAndTrims()
    {
        string result = WorktreeTitleResolver.NormalizeTaskName(
            "  Review\n\tpackage\u0000   title  "
        );

        Assert.That(result, Is.EqualTo("Review package title"));
    }

    [Test]
    public void NormalizeDisplayText_WhenUnicodeExceedsLimit_TruncatesByTextElement()
    {
        string grapheme = "e\u0301";
        string source = string.Concat(Enumerable.Repeat(grapheme, 60));

        string result = WorktreeTitleResolver.NormalizeTaskName(source);

        Assert.That(StringInfo.ParseCombiningCharacters(result), Has.Length.EqualTo(48));
        Assert.That(result, Does.StartWith(string.Concat(Enumerable.Repeat(grapheme, 47))));
        Assert.That(result, Does.EndWith("…"));
    }

    [Test]
    public void NormalizeDisplayText_WhenEmojiCrossesLimit_DoesNotSplitSurrogatePair()
    {
        string source = new string('A', 47) + "🦍" + "B";

        string result = WorktreeTitleResolver.NormalizeTaskName(source);

        Assert.That(StringInfo.ParseCombiningCharacters(result), Has.Length.EqualTo(48));
        Assert.That(result, Is.EqualTo(new string('A', 47) + "…"));
    }

    [TestCase("🇯🇵")]
    [TestCase("👍🏽")]
    [TestCase("👨‍👩‍👧‍👦")]
    public void NormalizeTaskName_WhenEmojiSequenceCrossesLimit_KeepsSequenceWhole(string emoji)
    {
        string source = new string('A', 46) + emoji + "BC";

        string result = WorktreeTitleResolver.NormalizeTaskName(source);

        Assert.That(result, Is.EqualTo(new string('A', 46) + emoji + "…"));
    }

    [Test]
    public void NormalizeDisplayText_WhenUnicodeContainsUnpairedSurrogates_RemovesInvalidUnits()
    {
        string result = WorktreeTitleResolver.NormalizeTaskName("A\uD800B\uDC00C");

        Assert.That(result, Is.EqualTo("ABC"));
    }
}
#endif
