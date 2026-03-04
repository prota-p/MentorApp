using Microsoft.Extensions.DependencyInjection;

namespace MentorApp.Infrastructure.Time;

/// <summary>
/// 時刻プロバイダーの内部サービス登録
/// </summary>
/// <remarks>
/// - AddTimeProvider: フェーズ1（サービス登録）で使用
/// .NET 8以降の標準TimeProvider.Systemを登録。テスト時はFakeTimeProviderで差し替え可能。
/// </remarks>
internal static class TimeProviderExtensions
{
    /// <summary>
    /// TimeProvider を DI コンテナに登録する（フェーズ1: サービス登録）
    /// </summary>
    internal static IServiceCollection AddTimeProvider(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}

