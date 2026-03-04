using MentorApp.Application.Contracts.Authentication;
using MentorApp.Application.Mentorships;
using MentorApp.Application.Topics;
using MentorApp.Application.Users;
using MentorApp.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MentorApp.Application;

/// <summary>
/// Application 層のサービスを DI コンテナに登録する拡張メソッド
/// </summary>
public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ドメインサービスの登録
        services.AddScoped<MentorshipDuplicationCheckService>();
        services.AddScoped<RoleChangeValidationService>();

        // アプリケーションサービスの登録
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
        services.AddScoped<UserService>();
        services.AddScoped<MentorshipService>();
        services.AddScoped<TopicService>();

        return services;
    }
}
