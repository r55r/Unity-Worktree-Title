# Unity Worktree Title

## 目的

同一projectのprimary checkoutと複数のGit linked worktreeを、Unity Editorのメインウィンドウタイトルから識別できるようにします。Codexが作成したworktreeでは、取得できる場合にtask名も表示します。

## 判定順

1. project rootに `.git` directoryがあればprimary checkoutとして `[main]` を表示します。
2. `.git` fileの `gitdir:` を解決します。有効な `commondir` があればlinked worktreeとして扱い、`commondir` がない有効なGit metadata directoryはprimary checkoutとして `[main]` を表示します。
3. project rootが `<CODEX_HOME>/worktrees/<id>/...` にあればCodex worktree IDを表示します。
4. Codex worktreeでなければproject rootのfolder名をworktree名として表示します。
5. Git状態を安全に識別できなければUnityの既定タイトルを維持します。

`.git` のrelative pathはproject rootを基準に、`commondir` のrelative pathは解決済みGit metadata directoryを基準に処理します。Git内部のmetadata folder名は表示名に使用しません。

## Codex task名

linked worktreeのGit metadata directoryにある `codex-thread.json` から `ownerThreadId` を取得し、`$CODEX_HOME/session_index.jsonl` の同一IDに一致する最後の有効レコードから `thread_name` を取得します。`CODEX_HOME` が空または未設定なら `~/.codex` を使用します。

Codexのローカルmetadataは公開APIではありません。そのため、次の状態は通常のfallbackとして扱い、Console warningや例外を発生させません。

- metadataまたはsession indexが存在しない
- JSONまたはJSONLの一部が不正
- owner threadに対応するレコードがない
- 読み取り中にファイルが更新・置換された
- 将来のCodex更新で形式が変わった

session indexはwriterを妨げない共有モードで行単位に読みます。不正行だけを無視し、同じIDが複数ある場合は最後の有効なtask名を使います。明示的な空task名はIDのみの表示へ戻します。

## 更新と文字列処理

- session indexの存在、最終更新時刻、ファイルサイズを2秒間隔で比較します。
- stampが変化したときだけmetadataを再読込します。
- 読み取り前後でstampが変わった場合は処理済みにせず、次回pollで再試行します。
- 正規化後の表示値が変わった場合だけ `EditorApplication.UpdateMainWindowTitle()` を呼びます。
- 空白と制御文字は単一のASCII spaceへまとめ、前後を除去します。
- 不正な単独surrogateは除去します。
- 48 Unicode text elementsを超える場合は、先頭47 elementsと `…` を表示します。

## UnityタイトルAPI

[`EditorApplication.updateMainWindowTitle`](https://docs.unity3d.com/ja/2023.2/ScriptReference/EditorApplication-updateMainWindowTitle.html) のcallbackへ渡される `ApplicationTitleDescriptor.projectName` は公開getterのみです。このpackageは `descriptor.title` にあるUnity既定のplatform別prefixを検証し、project部分の直後へ ` [context]` を一度だけ挿入します。

prefixがUnity既定形式と一致しない場合はno-opにするため、別extensionの独自タイトルを推測で書き換えません。scene、Build Target、Unity product/version、Code Coverage、将来追加される後続情報は、Unityが生成した文字列を維持します。

## セキュリティとプライバシー

metadataから取得した値は表示だけに使用します。shell、Git command、process、path解決へ再利用しません。

task名はOSのウィンドウタイトル、Dock、画面共有、録画、スクリーンショットへ映り得ます。機密情報をtask名へ含めないでください。
