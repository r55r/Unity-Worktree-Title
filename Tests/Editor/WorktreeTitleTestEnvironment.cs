/*
 * Worktree Title tests用に、実ユーザー環境から隔離したGit/Codexファイル構成を一時作成する。
 */

#if UNITY_INCLUDE_TESTS
using System;
using System.IO;

internal sealed class WorktreeTitleTestEnvironment : IDisposable
{
    public WorktreeTitleTestEnvironment()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "UnityWorktreeTitleTests",
            Guid.NewGuid().ToString("N")
        );
        CodexHome = Path.Combine(RootPath, "codex-home");
        Directory.CreateDirectory(CodexHome);
    }

    public string RootPath { get; }

    public string CodexHome { get; }

    public string SessionIndexPath => Path.Combine(CodexHome, "session_index.jsonl");

    public string CreatePrimaryCheckout(string folderName)
    {
        string projectRoot = Path.Combine(RootPath, folderName);
        Directory.CreateDirectory(Path.Combine(projectRoot, ".git"));
        return projectRoot;
    }

    public string CreatePrimaryMonorepoCheckout(string folderName, string projectRelativePath)
    {
        string checkoutRoot = Path.Combine(RootPath, folderName);
        string projectRoot = Path.Combine(checkoutRoot, projectRelativePath);
        Directory.CreateDirectory(Path.Combine(checkoutRoot, ".git"));
        Directory.CreateDirectory(projectRoot);
        return projectRoot;
    }

    public LinkedWorktreeFixture CreateNamedLinkedWorktree(
        string folderName,
        bool useRelativeGitDirectory
    )
    {
        string projectRoot = Path.Combine(RootPath, folderName);
        return CreateLinkedWorktree(projectRoot, folderName, useRelativeGitDirectory);
    }

    public LinkedWorktreeFixture CreateCodexLinkedWorktree(
        string worktreeId,
        bool useRelativeGitDirectory = false
    )
    {
        string projectRoot = Path.Combine(CodexHome, "worktrees", worktreeId, "SampleProject");
        return CreateLinkedWorktree(projectRoot, $"codex-{worktreeId}", useRelativeGitDirectory);
    }

    public LinkedWorktreeFixture CreateCodexLinkedMonorepoWorktree(
        string worktreeId,
        string projectRelativePath
    )
    {
        string checkoutRoot = Path.Combine(CodexHome, "worktrees", worktreeId, "Repository");
        return CreateLinkedWorktree(
            checkoutRoot,
            $"codex-{worktreeId}",
            false,
            projectRelativePath
        );
    }

    public LinkedWorktreeFixture CreateNamedLinkedMonorepoWorktree(
        string folderName,
        string projectRelativePath
    )
    {
        string checkoutRoot = Path.Combine(RootPath, folderName);
        return CreateLinkedWorktree(
            checkoutRoot,
            folderName,
            false,
            projectRelativePath
        );
    }

    public string CreateCheckoutWithSeparateGitDirectory(string folderName)
    {
        string projectRoot = Path.Combine(RootPath, folderName);
        string gitDirectory = Path.Combine(RootPath, "separate-git", folderName);
        Directory.CreateDirectory(projectRoot);
        Directory.CreateDirectory(gitDirectory);
        File.WriteAllText(Path.Combine(projectRoot, ".git"), $"gitdir: {gitDirectory}\n");
        return projectRoot;
    }

    public void WriteThreadMetadata(string gitDirectory, string ownerThreadId)
    {
        File.WriteAllText(
            Path.Combine(gitDirectory, "codex-thread.json"),
            $"{{\"version\":1,\"ownerThreadId\":\"{ownerThreadId}\"}}"
        );
    }

    public void WriteRawThreadMetadata(string gitDirectory, string json)
    {
        File.WriteAllText(Path.Combine(gitDirectory, "codex-thread.json"), json);
    }

    public void WriteSessionIndex(params string[] lines)
    {
        File.WriteAllText(SessionIndexPath, string.Join("\n", lines) + "\n");
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    private LinkedWorktreeFixture CreateLinkedWorktree(
        string projectRoot,
        string metadataName,
        bool useRelativeGitDirectory,
        string projectRelativePath = null
    )
    {
        string checkoutRoot = projectRoot;
        projectRoot = string.IsNullOrEmpty(projectRelativePath)
            ? checkoutRoot
            : Path.Combine(checkoutRoot, projectRelativePath);
        string commonGitDirectory = Path.Combine(RootPath, "repository", ".git");
        string gitDirectory = Path.Combine(commonGitDirectory, "worktrees", metadataName);
        Directory.CreateDirectory(projectRoot);
        Directory.CreateDirectory(gitDirectory);

        string relativeCommonDirectory = Path.GetRelativePath(gitDirectory, commonGitDirectory);
        File.WriteAllText(Path.Combine(gitDirectory, "commondir"), relativeCommonDirectory + "\n");

        string configuredGitDirectory = useRelativeGitDirectory
            ? Path.GetRelativePath(checkoutRoot, gitDirectory)
            : gitDirectory;
        File.WriteAllText(
            Path.Combine(checkoutRoot, ".git"),
            $"gitdir: {configuredGitDirectory}\n"
        );

        return new LinkedWorktreeFixture(projectRoot, gitDirectory);
    }
}

internal readonly struct LinkedWorktreeFixture
{
    public LinkedWorktreeFixture(string projectRoot, string gitDirectory)
    {
        ProjectRoot = projectRoot;
        GitDirectory = gitDirectory;
    }

    public string ProjectRoot { get; }

    public string GitDirectory { get; }
}
#endif
