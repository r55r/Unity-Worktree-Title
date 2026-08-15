/*
 * Unity Editor起動時にworktree titleを登録し、Codex task renameだけを軽量監視して反映する。
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class WorktreeTitleInitializer
{
    private const double PollIntervalSeconds = 2d;
    private static WorktreeTitleMonitor monitor;
    private static double nextPollTime;

    static WorktreeTitleInitializer()
    {
        EditorApplication.delayCall -= Initialize;
        EditorApplication.delayCall += Initialize;
        AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
        AssemblyReloadEvents.beforeAssemblyReload += Unregister;
        EditorApplication.quitting -= Unregister;
        EditorApplication.quitting += Unregister;
    }

    private static void Initialize()
    {
        try
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string codexHome = WorktreeTitleResolver.ResolveDefaultCodexHome();
            monitor = new WorktreeTitleMonitor(projectRoot, codexHome);
            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            EditorApplication.updateMainWindowTitle -= ApplyWorktreeTitle;
            EditorApplication.updateMainWindowTitle += ApplyWorktreeTitle;
            EditorApplication.update -= PollSessionIndex;
            EditorApplication.update += PollSessionIndex;
            EditorApplication.UpdateMainWindowTitle();
        }
        catch (Exception)
        {
            monitor = null;
        }
    }

    private static void ApplyWorktreeTitle(ApplicationTitleDescriptor descriptor)
    {
        try
        {
            descriptor.title = WorktreeTitleFormatter.AppendProjectContext(
                descriptor.title,
                descriptor.projectName,
                descriptor.activeSceneName,
                Application.platform == RuntimePlatform.OSXEditor,
                monitor?.Current.DisplayText
            );
        }
        catch (Exception)
        {
            // Window title integration must never interrupt the Editor.
        }
    }

    private static void PollSessionIndex()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
        {
            return;
        }

        nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

        try
        {
            if (monitor != null && monitor.Poll())
            {
                EditorApplication.UpdateMainWindowTitle();
            }
        }
        catch (Exception)
        {
            // Codex metadata is best-effort and must not add Console noise.
        }
    }

    private static void Unregister()
    {
        EditorApplication.delayCall -= Initialize;
        EditorApplication.updateMainWindowTitle -= ApplyWorktreeTitle;
        EditorApplication.update -= PollSessionIndex;
        AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
        EditorApplication.quitting -= Unregister;
    }
}
