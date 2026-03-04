# セキュリティ対策メモ

Webアプリケーションとして一般的に検討すべきセキュリティ項目について、
本アプリでの実装状況と判断理由を記録したメモ。

セキュリティは「対策すれば完全に安全」というものではなく、常に継続的な見直しが必要。
ここに挙げる対策はリスクを下げるための取り組みであり、特定の攻撃を完全に防ぐことを保証するものではない。
本番運用への移行時や機能追加時には改めてレビューすること。

---

### 凡例

| 記号 | 意味 |
|------|------|
| ✅ 対応済み | リスクを低減する対策を実装している |
| ⚠️ 一部省略 | 対応しているが、より強化できる余地がある |
| ❌ 未対応 | 意図的に省略、または未実装 |
| — 適用外 | アーキテクチャ上、脅威が成立しにくい構成になっている |

---

## インジェクション・スクリプト攻撃

### XSS（クロスサイトスクリプティング）✅ 対応済み

**脅威の概要：**
攻撃者が悪意のあるスクリプトをページに埋め込み、他のユーザーのブラウザで実行させる攻撃。

**実装：**
- Blazor の Razor テンプレートエンジンは `@変数` の出力を自動で HTML エスケープする
- `@message.Content` 等、ユーザー入力を表示する箇所はこの自動エスケープが適用される
- `@((MarkupString)...)` 等の raw HTML 出力は現時点では使用していない（将来追加する場合は別途レビューが必要）

---

### CSP（コンテンツセキュリティポリシー）❌ 未対応

**脅威の概要：**
XSS 攻撃が成立した場合でも、「このページは自分のサーバーから来たスクリプトしか実行しない」とブラウザに宣言しておくことで、攻撃者が埋め込んだ外部スクリプトの実行をブラウザ側でブロックできる。XSS の根本防止ではなく、被害を軽減するための追加の防御層。

**未対応の理由：**
学習用アプリのため省略している。

**補足：**
`frame-ancestors` ディレクティブのみは、.NET 8 以降の `AddInteractiveServerRenderMode()` がフレームワークレベルで `Content-Security-Policy: frame-ancestors 'self'` を自動付与するため、部分的に対応済みの状態になっている（クリックジャッキングの節を参照）。スクリプト・スタイル等の制御を含む本格的な CSP は未実装。

**実装する場合の方針：**

変更は [Program.cs](../src/MentorApp.Web/Program.cs) と [App.razor](../src/MentorApp.Web/Components/App.razor) の2ファイルのみ。

`Program.cs` でリクエストごとに暗号学的乱数から nonce を生成し、CSP ヘッダーをレスポンスに付与する。nonce は `HttpContext.Items` 経由で Razor コンポーネントに渡す。

```csharp
// サービス登録（nonce を App.razor に渡すために必要）
builder.Services.AddHttpContextAccessor();

// ミドルウェア
app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    context.Items["csp-nonce"] = nonce;

    // 開発環境では Browser Link（HTTP）/ Browser Refresh（WebSocket）が別ポートに接続するため connect-src を緩める
    var connectSrc = app.Environment.IsDevelopment()
        ? " connect-src 'self' ws://localhost:* wss://localhost:* http://localhost:*;"
        : "";

    context.Response.Headers.Append(
        "Content-Security-Policy",
        $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;{connectSrc} frame-ancestors 'none';"
    );
    await next();
});
```

`App.razor` で nonce を取得し、`ImportMap` と `<script>` タグに付与する（`ImportMap` は .NET 9 で `Nonce` パラメータが追加されている）。

```razor
@inject IHttpContextAccessor HttpContextAccessor

@{
    var nonce = HttpContextAccessor.HttpContext?.Items["csp-nonce"] as string ?? string.Empty;
}

<ImportMap Nonce="@nonce" />
...
<script src="lib/bootstrap/dist/js/bootstrap.bundle.min.js" nonce="@nonce"></script>
<script src="@Assets["_framework/blazor.web.js"]" nonce="@nonce"></script>
```

**各ディレクティブの理由：**

| ディレクティブ | 値 | 理由 |
|------|------|------|
| `default-src` | `'self'` | フォント等、明示していないリソースのフォールバック |
| `script-src` | `'self' 'nonce-...'` | `ImportMap`（動的なインラインスクリプト）に nonce が必要 |
| `style-src` | `'self' 'unsafe-inline'` | Blazor が動的に生成する `style=""` 属性は nonce/ハッシュで制御できないため許容 |
| `img-src` | `'self' data:` | Bootstrap のドロップダウン等が `data:image/svg+xml,...` を使用 |
| `frame-ancestors` | `'none'` | クリックジャッキング対策（`<meta>` タグ不可のためヘッダー必須） |

`style-src 'unsafe-inline'` が残る点は制限だが、スクリプト実行（XSS の主な被害経路）は nonce で制御されるため、主要なリスクは低減できる。

---

### SQLインジェクション ✅ 対応済み

**脅威の概要：**
ユーザー入力を SQL クエリに直接埋め込むことで、データベースを不正操作する攻撃。

**実装：**
- EF Core（LINQ クエリ、`FindAsync`、`FirstOrDefaultAsync` 等）がクエリをパラメータ化して発行するため、SQL インジェクションのリスクを構造的に低減できている
- 生の SQL 文字列（`ExecuteSqlRaw`、`FromSqlRaw` 等）は現時点では使用していない（将来使用する場合はパラメータ化の徹底が必要）

---

## クロスサイト・リクエスト系

### CSRF（クロスサイトリクエストフォージェリ）✅ 対応済み

**脅威の概要：**
悪意のあるサイトから、ログイン済みユーザーの権限で意図しない操作を実行させる攻撃。

**実装：**
- `UseAntiforgery()` ミドルウェアをグローバルに有効化（[Program.cs](../src/MentorApp.Web/Program.cs)）
- サインアウトフォーム（唯一の通常 HTML フォーム POST）に `<AntiforgeryToken />` を付与し、サーバー側で `ValidateRequestAsync` により検証（[UserMenu.razor](../src/MentorApp.Web/Components/Layout/UserMenu.razor)、[AuthenticationExtensions.cs](../src/MentorApp.Infrastructure/Authentication/AuthenticationExtensions.cs)）
- Blazor Server の `EditForm`（トピック作成・メッセージ投稿・ユーザー更新等）は SignalR（WebSocket）経由で動作するため、通常の HTTP フォーム POST を行わず、CSRF のリスクを構造的に低減できている

---

### クリックジャッキング ⚠️ 一部省略

**脅威の概要：**
本アプリを透明な `<iframe>` として悪意のあるサイトに埋め込み、ユーザーに意図しないクリックをさせる攻撃。

**実装：**
.NET 8 以降の Blazor Web App では、`AddInteractiveServerRenderMode()` が WebSocket 圧縮の保護のためにフレームワークレベルで `Content-Security-Policy: frame-ancestors 'self'` を自動的にレスポンスヘッダーに付与する（[Program.cs](../src/MentorApp.Web/Program.cs)）。これにより、異なるオリジンからの `<iframe>` 埋め込みはブロックされる。

**残存リスク：**
`'self'`（同一オリジンからの埋め込みは許可）であり、より厳格な `'none'`（完全に埋め込み禁止）より緩い設定。`'none'` にするには以下の設定で上書きできる：

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o =>
    {
        o.ContentSecurityFrameAncestorsPolicy = "'none'";
    });
```

ただし、公式ドキュメントは WebSocket 圧縮が有効な状態で値を変更する場合に注意を促しているため、変更する際は圧縮への影響を確認すること。

**参考：**
- [ASP.NET Core Blazor content security policy（公式ドキュメント）](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy)
- [ServerComponentsEndpointOptions.ContentSecurityFrameAncestorsPolicy API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.server.servercomponentsendpointoptions.contentsecurityframeancestorspolicy)

---

### オープンリダイレクト ✅ 対応済み

**脅威の概要：**
ログイン後のリダイレクト先 URL を改ざんし、フィッシングサイト等に誘導する攻撃。

**実装：**
- Mock プロバイダーの `ReturnUrl` 処理で `Results.LocalRedirect()` を使用（[MockProviderSetup.cs](../src/MentorApp.Infrastructure/Authentication/Providers/Mock/MockProviderSetup.cs)）。外部 URL が渡された場合は例外となり、リダイレクトは行われない
- OIDC プロバイダー（Entra ID / Google）は ASP.NET Core の OIDC ミドルウェアが標準的な検証を行う

---

## ブラウザ / HTTP 設定

### Cookie セキュリティ ⚠️ 一部省略

**脅威の概要：**
Cookie の属性設定が不十分な場合、CSRF・セッションハイジャック・平文送信のリスクが高まる。

**実装状況：**

| 属性 | 設定値 | 備考 |
|------|--------|------|
| `HttpOnly` | ✅ `true`（ASP.NET Core 既定値） | JavaScript からのアクセスを遮断 |
| `SameSite` | ⚠️ `Lax`（ASP.NET Core 既定値） | 明示的な設定なし |
| `Secure` | ⚠️ `SameAsRequest`（ASP.NET Core 既定値） | 明示的な設定なし |

**`SameSite` について：**
`Lax`（既定値）は、外部サイトからのトップレベル GET ナビゲーション（メールや Slack のリンクからのアクセス）にはクッキーを付与する。`Strict` にするとこの経路を遮断できるが、外部リンクからアクセスした際に毎回再ログインが必要になる。
本アプリでは Blazor Server の SignalR と POST アンチフォージェリで主要操作のリスクを低減しているため、`Lax` のままとしている。

**`Secure` について：**
`SameAsRequest`（既定値）は HTTPS リクエスト時にのみ `Secure` 属性を付与する。本番では `UseHttpsRedirection` により HTTP アクセスは HTTPS にリダイレクトされるため、平文でのクッキー送信のリスクは限定的。`Always` にすると開発環境の HTTP プロファイル（`http://localhost:5001`）では認証クッキーが送信されず、動作不能になるため省略している。

---

### CORS（クロスオリジンリソース共有）— 適用外

**脅威の概要：**
外部ドメインからの不正な API アクセス。

**適用外の理由：**
本アプリは Blazor Server のみで構成され、外部から呼び出せる API エンドポイントを公開していない。ブラウザの同一オリジンポリシーが適用される構成のため、CORS ポリシーの明示的な設定は不要。
将来的に外部向けの API を追加する場合は CORS 設定が必要になる。

---

### HTTPS・通信の暗号化 ✅ 対応済み

**脅威の概要：**
平文の HTTP 通信を傍受し、セッションクッキーや通信内容を盗む攻撃（中間者攻撃）。

**実装：**（[Program.cs](../src/MentorApp.Web/Program.cs) 59〜70行目）

- `UseHttpsRedirection()` — HTTP アクセスを HTTPS へ自動リダイレクトする（全環境）
- `UseHsts()` — ブラウザに「このサイトは常に HTTPS で通信すること」を記憶させる（本番のみ）。次回以降はブラウザ自身が http:// を https:// に変換するため、最初の HTTP リクエストすら発生しなくなる

**開発環境での注意点：**

`UseHttpsRedirection()` は「HTTP を HTTPS にリダイレクトする」だけであり、**リダイレクト先の HTTPS ポートが実際に開いていないと機能しない。**
どのポートを開くかは [launchSettings.json](../src/MentorApp.Web/Properties/launchSettings.json) のプロファイル設定で決まる。

| プロファイル | 開いているポート | `UseHttpsRedirection()` |
|---|---|---|
| `http` | HTTP のみ（5001） | ❌ リダイレクト先がなく接続エラー |
| `https` / `https-production` | HTTP（5001）＋ HTTPS（5000） | ✅ 正常に機能する |

`http` プロファイル使用時はリダイレクトが壊れた動作になるが、開発利便性を優先した意図的な構成。launchSettings.json は開発専用であり、本番デプロイには含まれない。

**本番環境での考慮：**

本番環境では、アプリの前段に**リバースプロキシ**（Nginx 等、クライアントからのリクエストを受けてアプリに転送する中継役）や**ロードバランサー**（複数サーバーへの振り分けも担う大規模構成向け）が置かれることが多い。この場合、HTTPS の暗号化解除はリバースプロキシ側で行われ、リバースプロキシ〜アプリ間は HTTP になるため、アプリは HTTPS を意識しなくてよくなる。その場合、HTTPS の安全性はインフラ側（証明書管理・リバースプロキシの設定等）が担う役割分担になる。

---

## 認証・アクセス制御

### 認証・認可 ✅ 対応済み

**脅威の概要：**
認証の迂回や、権限のない操作の実行。

**実装：**
- 外部 IdP（OIDC）による認証（Entra ID / Google）
- Cookie 認証によるサーバーサイドセッション管理
- 全認証済みページに `[Authorize]` 属性を付与
- ロールベースのアクセス制御（Admin / Mentor / Mentee）
- `AuthorizeView` によるロールに応じた UI 出力制御
- ロール情報は IdP に依存せずアプリ独自で管理（`IClaimsTransformation`）

ただし、ロール変更は現在の SignalR 回線（接続中のページ）には即座に反映されない。次のページ遷移（HTTP リクエスト）のタイミングで `IClaimsTransformation` が再実行され DB から最新ロールが取得されるため、再ログインは不要。

---

### レート制限 ❌ 未対応

**脅威の概要：**
認証エンドポイント等に大量リクエストを送り、ブルートフォース攻撃やサービス停止を狙う。

**未対応の理由：**
学習用アプリのため省略。本番対応には ASP.NET Core の `RateLimiter` ミドルウェア（.NET 7 以降）や、Azure Front Door / Application Gateway 等のインフラ側対策が選択肢となる。

---

## 情報漏洩・機密管理

### エラー情報の漏洩 ✅ 対応済み

**脅威の概要：**
詳細なエラーメッセージやスタックトレースが攻撃者にシステム内部情報を与える。

**実装：**
- 本番環境では `UseExceptionHandler` でエラーページに転送し、スタックトレース等の内部情報を露出しない（[Program.cs](../src/MentorApp.Web/Program.cs)）
- Web 層コンポーネントでは `catch` で汎用メッセージを `ToastService` に表示し、内部エラーはサーバー側ログにのみ記録する方針（[development-guide.md](development-guide.md) 3.1参照）

---

### 機密情報の管理 ⚠️ 一部省略

**脅威の概要：**
クライアントシークレットや接続文字列がコードやリポジトリに露出する。

**現状（学習用アプリとしての方針）：**

本アプリは学習目的のため、`appsettings.Development.json` および `appsettings.Production.json` の両ファイルに実クレデンシャル（OIDC プロバイダーの `ClientSecret`）を直接記載している。どちらのファイルも `.gitignore` 対象であり、リポジトリには含まれない。ただし、プロジェクトディレクトリ内に存在するため、gitignore の設定ミスや IDE の自動コミット等により意図せずリポジトリに含まれるリスクはある。

また、開発環境（`appsettings.Development.json`）では EF Core の `EnableSensitiveDataLogging: true` が有効になっており、クエリパラメータの値（個人情報等）がログに出力される。本番環境（`appsettings.Production.json`）では `false` に設定済み。

**より安全な選択肢：**

| 環境 | 推奨方法 |
|------|------|
| ローカル開発 | `dotnet user-secrets`（User Secrets）— シークレットを OS のユーザープロファイル下（`%APPDATA%\Microsoft\UserSecrets\`）に格納するため、プロジェクトディレクトリ外に保存され、誤コミットのリスクがない |
| 本番環境 | 環境変数または Azure Key Vault 等の外部シークレット管理サービス — `appsettings.Production.json` をサーバーに配置するのではなく、シークレットをインフラ側から注入する構成が望ましい |
