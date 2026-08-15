/*
 * Worktree Title の内部ロジックを、公開 API を増やさず package test assembly から検証可能にする。
 */

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FlyingGorilla.WorktreeTitle.Tests")]
