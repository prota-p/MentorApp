# 開発ガイド

本ドキュメントは MentorApp の実装時のルール・規約をまとめたものです。
[要件・設計ドキュメント](spec.md)

---

## 目次

1. [アーキテクチャ方針](#1-アーキテクチャ方針)
2. [コーディング規約](#2-コーディング規約)
3. [例外ハンドリング・ロギング](#3-例外ハンドリングロギング)
4. [テスト方針](#4-テスト方針)
5. [UI層（Blazorコンポーネント）](#5-ui層blazorコンポーネント)

---

## 1. アーキテクチャ方針

### 1.1 インターフェース化の方針

**基本原則**: 過度なインターフェース化は行わない。交換可能性が明確なもののみインターフェース化する。

**インターフェース化する：**
- 認証・認可のインフラ実装（モック / Entra ID の切り替え）
- リポジトリ（レイヤー境界の明確化、テスト時のモック化）
- 外部API連携など

**インターフェース化しない：**
- アプリケーションサービス

**インターフェースの配置：**
- **リポジトリインターフェース**: Domain 層の各集約フォルダに配置
- **その他のインターフェース（Contracts フォルダ）**: 認証サービスなど、インフラ実装の差し替えが必要なものは Application 層の Contracts フォルダに配置
- **依存性逆転の原則**: インターフェースは「使う側」が定義し、Infrastructure 層が実装

### 1.2 認証・認可の実現方式

- 外部IdP（OIDC）でユーザーを認証
- 認証結果を内部表現（ExternalIdentity）に変換
- Cookie 認証 + Claims を用いてアプリ内セッションを確立
- ロールは IdP に依存せず、アプリ独自に管理
- 外部IdPの情報（sub等）は ExternalId として保持
- IClaimsTransformation により外部認証結果 → アプリ内ロールクレームに変換
- 認証基盤は Infrastructure 層に隠蔽し、Application / Web から切り離す

### 1.3 永続化の方針

#### 集約設計

集約とは、**ビジネスルールを守るために一つのトランザクションで一緒に変更する必要があるオブジェクトのまとまり**。集約をまたぐ変更は別トランザクションで行う（強い整合性は求めない）。

| エンティティ | 位置づけ | 理由 |
|-------------|---------|------|
| User | 集約根 | 独立したライフサイクルを持つ |
| Mentorship | 集約根 | 独立したライフサイクルを持つ |
| Topic | 集約根 | 独立したライフサイクルを持つ |
| Message | Topic の子エンティティ | 「Open でない Topic にはメッセージを追加できない」というルールを、同一トランザクションで確実に守るため |

#### EF Core と DDD

**基本方針：**
- 規約を重視し、規約で設定できないものは Fluent API で設定
- EF Core の利便性と集約間の疎結合のバランスを考慮

**ナビゲーションプロパティの扱い：**
- **同一集約内**：ナビゲーションプロパティで相互参照、カスケード削除を許容
- **別集約間**：エンティティ定義上は存在するが、リポジトリ（Command側）ではIncludeしない。QueryService（Query側）でのみ表示用途で使用

**値オブジェクトの実装方針：**
- ビジネスルールやバリデーションを持つドメイン概念は値オブジェクト化を検討
- 不変性（Immutable）と等価性（Equality）を備える
- EF Core の Value Conversion を使用してDB型との相互変換を実現

#### Command/Query分離（CQRS）

**基本方針：**
- **Command側（リポジトリ）**：状態変更の前処理に使用。エンティティを返す。
- **Query側（QueryService）**：UI表示用。DTOを返す。

**リポジトリ（Command側）：**
- `FindByIdAsync` は更新対象の取得に使用
- インターフェースはDomain層に配置
- UnitOfWork経由でアクセス

**QueryService（Query側）：**
- 一覧取得、詳細表示などUI表示用途
- 別集約を含めたJOIN/Includeを許容
- AsNoTrackingで最適化
- DTOのみを返す（エンティティは返さない）
- インターフェースはApplication層（Contracts）に配置
- UI層から直接呼び出し可能（Application Serviceを経由しない）

**【認可規約】QueryServiceの呼び出しルール：**
- 全メソッドは `CurrentUser` を引数に受け取ること
- 認可ロジック（誰が何を見られるか）はQueryService内部のWHERE句に集約する
- UI層でロールを見て「どのメソッドを呼ぶか」を分岐させてはならない
- 例：`if (isAdmin) GetAllAsync() else GetByUserIdAsync()` → NG
- 例：`GetAccessibleAsync(currentUser)` → OK（Admin/非Adminの判断はサービス内で行う）

**Application Service：**
- Command操作（作成、更新、削除）のみを提供
- Query操作はQueryServiceに委譲

#### EF Core マイグレーション運用

- `EnsureCreated` を使用
- スキーマ変更時のみ `EnsureDeleted` で削除して再作成
- シンプルで迅速な開発サイクルを優先
- **本番・検証環境へのデプロイを想定する場合は `dotnet ef migrations` を活用すること**（`EnsureCreated` はスキーマ進化に対応できないため）

---

## 2. コーディング規約

### 2.1 命名規則

**基本原則**: Microsoft標準の命名規則に従う。

**参照ドキュメント:**
- [.NET の名前付けガイドライン](https://learn.microsoft.com/ja-jp/dotnet/standard/design-guidelines/naming-guidelines)

### 2.2 コメント方針

**基本原則**: コードで表現できることはコードで表現する。コメントは "Why" を書く場所。

**XMLドキュメントコメント：**
- `public` なクラス・メソッドには原則記載するが、以下は省略可：
  - クラス名・メソッド名から自明な内容
  - 単純なDTO（record）で型名や構造が明確なもの
  - 標準的なCRUDメソッド（`FindByIdAsync` など）
- 記載する場合は簡潔に（1～2行程度を目安）
- パラメータごとの `<param>` や戻り値の `<returns>` は複雑な場合のみ記載

**RemarksとWHYの記載：**
- 設計判断の理由は `<remarks>` に記載する
- 学習用途として特に重要な概念には丁寧に説明を加える
  - 例：集約の境界、ドメインサービスの役割、依存性逆転など
- 将来の変更を検討する人のために「なぜこの実装を選んだか」を残す
  - 例：「EF Coreの規約を優先」「認証基盤は隠蔽」など
- ビジネスルールの意図や制約を説明する

**インラインコメント（`//`）：**
- わかりにくいロジックや一時的な制約には記載
- 例：特殊なビジネスルール、回避策、パフォーマンス上の理由など

**TODO / HACK：**
- 一時的な対応や将来の改善点は `// TODO:` `// HACK:` で明示

---

## 3. 例外ハンドリング・ロギング

### 3.1 層ごとの責務

**Domain層：**
- ビジネスルール違反時に `InvalidOperationException` 等をスロー
- 例外メッセージはドメインの文脈で記述
- ログ記録は行わない

**Application層：**
- すべてのpublicメソッドを `try-catch` で囲む
- 正常系・準正常系（Not Found等）はログに記録
- 例外発生時に `LogError` で構造化ログに記録し、`throw;` で再スロー
- ユーザーに具体的な理由を伝える必要がある場合のみカスタム例外を定義

**Web層（Blazorコンポーネント）：**
- すべての非同期処理を `try-catch-finally` で囲む
- ログ記録は行わない（ユーザー向けには `ToastService` で表示、内部詳細はApplication層のログに委ねる）
  - ただし **Blazor Server** はサーバーサイドで動作するため、デバッグ目的で `ILogger` を追加しても差し支えない
  - **WASM / Client-side Blazor** の場合はログがブラウザコンソールへの出力になるため、本番環境では注意すること
- ユーザーフレンドリーなメッセージを `ToastService` で表示
- 内部エラー情報（スタックトレース等）は含めない
- `finally` でローディング状態をクリーンアップ

**Infrastructure層：**
- **初期化処理（DB作成、シード等）**：`Information` / `Debug` レベルでログ出力
  - Application層を経由しないため、インフラ層で直接記録
  - 起動時の状態確認やトラブルシュートに有用
- **リポジトリ**：ログ出力しない
  - Application層でログを一元管理
  - EF Core の詳細ログで SQL レベルの診断は可能
- **認証系**：必要に応じて `Debug` / `Trace` レベルで診断用ログを検討
  - 認証トラブルシュート用（本番では通常無効化されるログレベル）

### 3.2 例外ハンドリング

#### カスタム例外の使用

**原則：**
- ユーザーに具体的な理由を伝える必要がある場合のみ定義
- 例：ドメインサービスで `InvalidOperationException` を使用（メンタリング重複チェック等）
- 単なるデータ不在は `KeyNotFoundException`、引数不正は `ArgumentException` で十分

#### セキュリティ

- ユーザーへのエラーメッセージには内部情報を含めない
- 詳細なエラー情報はログに記録

#### 本番アプリへの拡張（参考）

学習用アプリとしてシンプルさを優先しているが、本番環境では以下を検討：
- エラーコード体系の導入でログとUI表示を紐付け
- ログ・例外のボイラープレートについては、Mediatorパターン（Mediatorライブラリ等）を使った共通化も可能（学習コストあり）

### 3.3 ロギング構成（Serilog）

**基本方針：**
- **構造化ログ**：プロパティベースで記録し、検索・分析を容易にする
- **ログの一元管理**：主にアプリケーション層でログを出力
- **ロガー取得**：DI経由で`ILogger<T>`を注入

**シンク構成：**
- Console（開発時）
- File（ローリング）

**環境別ログレベル：**
- Development：Debug 以上
- Production：Information 以上

---

## 4. テスト方針

### 4.1 テストケース選定方針

本アプリのテストは、全ての入力パターンや画面操作を網羅することではなく、主要な動作経路が壊れていないことを少数の代表ケースで確認する方針とする。

E2E テストおよび Application 層テストは、認証・画面遷移・DB 永続化・主要ユースケースの連携が成立していることを確認するスモークテストとして位置づける。

Domain 層テストは、値オブジェクト、集約、ドメインサービスのテスト例を示すための代表ケースを中心とする。網羅的な仕様テストではなく、ドメインルールをどのように単体で検証するかを示すサンプルとして扱い、重要なルールや変更頻度の高い箇所は必要に応じて追加する。

### 4.2 Domain層テスト

**基本構成：**
- xUnit 標準の `Assert` を使用し、追加ライブラリに依存しないシンプルなテストにする
- `Arrange / Act / Assert` を基本形とする
- `Assert.Throws` のように実行と検証が一体になる場合は `Act & Assert` として扱う

**テスト対象の検証：**
- ドメインモデルの初期状態、値オブジェクトの検証、ビジネスルール違反時の例外を確認する

### 4.3 アプリケーション層テスト

**基本構成：**
- xUnit の IAsyncLifetime を使用したテストクラス単位のセットアップ/クリーンアップ
- 本番と同じDI拡張メソッドを再利用し、テスト用実装のみ差し替え

**データベース：**
- SQL Server LocalDB を使用（GUID付き個別DB、使い捨て方式）
- テストごとに独立したDBを作成し、テスト後に破棄
- テストの並列実行が可能

**テストデータの準備：**
- テストメソッド内で直接セットアップ（Arrange フェーズ）
- UnitOfWork パターンでデータを投入
- 必要に応じて TestClock で時刻を制御

**テスト対象の検証：**
- Application層のサービスクラスを通じたエンドツーエンドの動作確認
- xUnit 標準の `Assert` によるシンプルなアサーション

### 4.4 E2Eテスト

**テストフレームワーク：**
- **Playwright** を使用したブラウザ自動化テスト
- **WebApplicationFactory** でKestrelサーバーを起動

**テスト環境構成：**
- Testing環境として実行（自動DB初期化をスキップ）
- テストクラス単位でサーバーとDBを共有
- 各テストは独自のブラウザコンテキストを使用（Cookie等は分離）

**データベース：**
- アプリケーション層テストと同様にGUID付き個別DB
- テストデータにはユニークなGUIDを使用して衝突を回避

**ブラウザ設定：**
- Chromium をヘッドレスモードで実行
- テストクラス間でブラウザインスタンスを共有（xUnit IClassFixture）

---

## 5. UI層（Blazorコンポーネント）

### 5.1 コンポーネント設計の基本原則

**コンポーネント分離の判断基準：**
- **分離する**：複数箇所で再利用される（例：`UserDetailForm` → Profile/Admin両方で使用）
- **分離する**：ロジックが複雑で単独テストが必要
- **分離しない**：1箇所でのみ使用され、再利用の見込みがない

**コンポーネントの自己完結性：**
- 単独で動作可能に設計（オブジェクト指向UIの考え方）
- 必要なサービスは自身で`@inject`し、データ取得も自身で行う
- 親からはパラメータ（モード切替、イベントコールバック等）のみを受け取る

### 5.2 InteractiveLoaderの適用

**SSR → Interactive 切り替え時のちらつき対策：**
- ログイン後にリダイレクトされる可能性があるページに`InteractiveLoader`を適用
- 適用対象：Home、Profile、一覧・詳細ページ等
- 適用不要：Login（SSRのみ）、エラーページ

### 5.3 スタイリング方針

**Bootstrapの使用：**
- レイアウト（グリッド、コンテナ）、基本的なユーティリティクラスのみ
- 複雑な装飾は避け、シンプルな見た目を維持

**ドメイン固有の表現：**
- Bootstrapのセマンティックカラー（`primary`, `danger`等）をドメイン概念に流用しない
- ドメイン固有の色・スタイルは専用コンポーネント + Scoped CSSで定義
- 例：`RoleBadge.razor` + `RoleBadge.razor.css`

### 5.4 コンポーネントの配置規則

- `Components/Layout/`：レイアウト関連
- `Components/Pages/`：ルーティング対象のページ（ドメインごとにサブフォルダ）
- `Components/Shared/`：再利用可能なコンポーネント（ドメインごとにサブフォルダ）

**命名規則：**
- ページ：`Index.razor`（一覧）、`Detail.razor`（詳細）
- コンポーネント名は「対象ドメイン＋役割サフィックス」の形式とし、サフィックスは以下の意図で使い分ける
  - `*List`    — 複数件の一覧表示
  - `*Form`    — 既存データの表示・編集
  - `*AddForm` — 新規作成専用フォーム
  - `*Badge`   — ステータスやロールのインラインラベル
  - `*Select`  — 単一または複数選択UI
  - `*Panel`   — 複数の情報・操作をまとめたブロック
  - `*Section` — ページ内の意味的区画
  - `*Card`    — 単一指標や情報のカード表示
  - 新たな役割には上記に準じた名称を追加してよい

### 5.5 パラメータ設計

- 複数のboolパラメータより、意図が明確な列挙型を優先
  - モード切替：`UserDetailFormMode`（`AdminDetail` / `Profile`）
  - サイズ指定：`FormSize`（`Normal` / `Small`）、`SpinnerMode` 等
  - 文字列のマジックナンバー（`"small"` 等）は使わない
- 必須パラメータには `[Parameter, EditorRequired]` を付与
- イベントコールバックは`OnXxx`で統一（`OnEditClick`, `OnSaved`等）
- 双方向バインディングは `Value` + `EventCallback<T> ValueChanged` ペアで実装

### 5.6 ローディング表示

**LoadingSpinner：**
- データ取得中は `LoadingSpinner` で処理中であることを表示
- ローディング判定は状況に応じた条件を使用（`isLoading` フラグ、`data == null` など）
- 埋め込みコンポーネント（リスト系）は自身で `LoadingSpinner` を表示
- ダッシュボード部品など軽量なものは空白表示でも可

**SpinnerMode（用途に応じた高さ・サイズ・色）：**
- `Overlay`：SSR→Interactive切り替え用（100vh、secondary色）- InteractiveLoader専用
- `Page`：ページ全体用（高さ300px）
- `Section`：カード・セクション内（高さ200px）- デフォルト
- `Compact`：小さいフォーム・領域用（高さ100px、小さいサイズ）
- `Inline`：ボタン内のインラインスピナー

### 5.7 フォームバリデーション

**DomainEditContext：**
- ドメインバリデーションと Blazor の EditContext/ValidationMessageStore を統合するラッパー
- `EditContext` の生成、`ValidationMessageStore` の管理、`OnFieldChanged` のハンドリングを内部で行う
- 手動で `EditContext` + `ValidationMessageStore` + イベント購読を管理しない

**使い方：**

```csharp
// 初期化
editContext = new DomainEditContext(formModel);

// プロパティ単位のバリデーション（単一フィールド）
editContext.SetValidator(
    (nameof(model.DisplayName), () => User.ValidateDisplayName(model.DisplayName))
);

// 複数フィールド横断のバリデーション
editContext.SetValidator(() => Topic.Validate(form.MentorshipId, form.Title));

// テンプレートでの使用
// <EditForm EditContext="editContext.EditContext" OnSubmit="OnSubmitAsync">

// 送信時の検証
if (!editContext.Validate()) return;
```

### 5.8 `@code` ブロックの構成

**記述順序：**
1. `[Parameter]` プロパティ
2. プライベートフィールド
3. 算出プロパティ（computed properties）
4. ライフサイクルメソッド（`OnInitializedAsync` 等）
5. イベントハンドラ / ビジネスロジック
6. ヘルパーメソッド / 内部クラス

**コーディング規約：**
- インデントは4スペースで統一（`@code {` 直下から）
- 算出プロパティは PascalCase（`IsEmpty`、`ShouldShowAddButton`）
- 単純な1行メソッドは式本体メンバー（expression-bodied）を使用
