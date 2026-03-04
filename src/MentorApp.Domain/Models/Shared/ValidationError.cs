namespace MentorApp.Domain.Models.Shared;

/// <summary>
/// 単一のプロパティバリデーション結果
/// </summary>
public record ValidationError(string Property, string Message);
