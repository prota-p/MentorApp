namespace MentorApp.Web.Constants;

/// <summary>
/// 認証関連のルートパス定数
/// </summary>
/// <remarks>
/// ルーティングパスはUI（Web層）の関心事であるため、Web層で定義する。
/// Infrastructure層の認証設定には引数として渡す（依存性逆転）。
/// </remarks>
public static class AuthRoutes
{
    public const string SignIn = "/auth/signin";
    public const string SignOut = "/auth/signout";
    public const string PostLogoutRedirect = PageRoutes.Login;
    public const string OidcCallback = "/signin-oidc";
}

/// <summary>
/// ページのルートパス定数
/// </summary>
/// <remarks>
/// Razor の @page ディレクティブでは定数を使用できないため、各 .razor ファイルではパスをハードコードしている。
/// この定数はコード内からの参照用。
/// </remarks>
public static class PageRoutes
{
    /// <summary>Blazor の &lt;base href&gt; 等で使用する技術的なルートパス</summary>
    public const string Root = "/";

    /// <summary>未認証ユーザーのリダイレクト先</summary>
    public const string Login = "/";

    public const string Home = "/home";

    public const string Error = "/error";

    public const string NotFound = "/not-found";

    /// <summary>Cookie 認証の AccessDeniedPath として使用</summary>
    public const string AccessDenied = "/access-denied";

    /// <summary>ユーザー一覧（Admin専用）</summary>
    public const string Users = "/users";

    /// <summary>ユーザー詳細（Admin専用）</summary>
    public const string UserDetail = "/users/{0}";

    /// <summary>メンタリング一覧（認証済み：自分に関係するもの、Adminは全件）</summary>
    public const string Mentorships = "/mentorships";

    /// <summary>メンタリング詳細</summary>
    public const string MentorshipDetail = "/mentorships/{0}";

    /// <summary>ログインユーザー自身のプロフィール編集</summary>
    public const string Profile = "/profile";

    public const string Topics = "/topics";

    public const string TopicDetail = "/topics/{0}";
}
