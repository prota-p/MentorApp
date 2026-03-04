using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;

namespace MentorApp.Infrastructure.Authentication.Providers.Shared;

/// <summary>
/// 認証プロバイダー固有の設定を抽象化するインターフェース
/// </summary>
/// <remarks>
/// Strategy パターンにより、プロバイダー固有の設定ロジックをカプセル化する。
/// 各プロバイダー（Mock / EntraId / Google）はこのインターフェースを実装する。
/// Cookie認証は全プロバイダー共通のため、AuthenticationExtensions で一元管理される。
/// 
/// ライフサイクル:
/// - フェーズ1（サービス登録）: ConfigureAuthentication
/// - フェーズ2（ミドルウェア構成）: MapSignInEndpoint
/// フェーズ間で同一インスタンスを共有するため、DIコンテナにシングルトンとして登録される。
/// </remarks>
internal interface IProviderSetup
{
    /// <summary>
    /// このプロバイダー用のクレームマッピング
    /// </summary>
    /// <remarks>
    /// ClaimMappingIdentityResolver で使用し、外部クレームからユーザー情報を抽出する。
    /// </remarks>
    public IdentityClaimMappings ClaimMappings { get; }

    /// <summary>
    /// プロバイダー固有の認証スキームを追加する（フェーズ1）
    /// </summary>
    public void ConfigureAuthentication(AuthenticationBuilder builder, AuthenticationPathOptions pathOptions);

    /// <summary>
    /// プロバイダー固有のサインインエンドポイントを設定する（フェーズ2）
    /// </summary>
    public void MapSignInEndpoint(WebApplication app, AuthenticationPathOptions pathOptions);
}
