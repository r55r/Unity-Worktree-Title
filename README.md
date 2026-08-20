# Unity Worktree Title

`com.flying-gorilla-studios.worktree-title` は、現在のGit worktreeと、取得できる場合はCodex task名をUnity Editorのメインウィンドウタイトルへ表示するEditor専用UPM packageです。

複数のworktreeでUnity Editorを同時に開いていても、Dock、ウィンドウ切り替え、画面共有から対象を区別できます。

## 表示例

```text
SampleProject [a1b2 · Update inventory UI]
```

表示規則は次のとおりです。

- Codex worktree: `SampleProject [a1b2 · Codex task名]`
- task名を取得できないCodex worktree: `SampleProject [a1b2]`
- primary checkout: `SampleProject [main]`
- 通常のlinked worktree: `SampleProject [worktreeフォルダ名]`
- Git管理外または識別不能: Unityの既定タイトルを変更しない

`main` はbranch名ではなく、primary checkoutを表す固定ラベルです。

## 導入

Unity Package ManagerのGit URL、またはprojectの `Packages/manifest.json` へtag固定で追加します。

```json
{
  "dependencies": {
    "com.flying-gorilla-studios.worktree-title": "https://github.com/r55r/Unity-Worktree-Title.git#v0.1.0"
  }
}
```

導入後の設定やRuntime componentはありません。Editor assemblyが自動で初期化されます。

Unity projectがmonorepoのサブディレクトリにある場合も、最も近い祖先の `.git` をcheckout rootとして自動検出します。

## Codex連携

[CodexのGit worktrees](https://learn.chatgpt.com/docs/environments/git-worktrees)では、worktreeごとに独立したtaskを割り当てられます。このpackageは、その対応を次のローカルファイルからbest-effortで解決します。

- linked worktreeのGit metadata directoryにある `codex-thread.json`
- `$CODEX_HOME/session_index.jsonl`
- `CODEX_HOME` 未設定時は `~/.codex/session_index.jsonl`

これらはCodexの公開APIではありません。欠損、形式変更、不正JSON、読み取り失敗時は例外をEditorへ漏らさず、worktree IDのみの表示へフォールバックします。

task名は空白と制御文字を正規化し、Unicodeの文字単位を壊さず、`…` を含め最大48文字にします。indexの更新日時を2秒間隔で確認し、実際の表示名が変わった場合だけタイトルを更新します。

## Unityタイトルとの共存

Unity 2023.2以降の公開API [`EditorApplication.updateMainWindowTitle`](https://docs.unity3d.com/ja/2023.2/ScriptReference/EditorApplication-updateMainWindowTitle.html) を使用します。`ApplicationTitleDescriptor.projectName` は読み取り専用のため、Unityが生成済みの `title` にあるproject部分の直後へsuffixを挿入します。既定のscene、Build Target、Unity product/version、Code Coverage等の後続部分は再構築せず、そのまま維持します。

想定外のタイトル形式や、先に別extensionが完全な独自タイトルへ変更している場合は、安全のため何も変更しません。

## プライバシー

Codex task名はOSのウィンドウタイトルへ表示されます。画面共有、Dock、タスク切り替え、スクリーンショットへ映り得るため、機密情報をtask名へ含めないでください。

## 互換性

- 最低対応: Unity 2023.2
- 検証済み: Unity 6.4.1f1
- Editor専用。Player buildとRuntime assemblyには含まれません。

## License

MIT License。詳細は `LICENSE.md` を参照してください。
