using MentorApp.Domain.Models.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace MentorApp.Web.Components.Shared;

/// <summary>
/// ドメイン ValidationError を Blazor のバリデーション機構に適用する拡張メソッド
/// </summary>
public static class DomainValidationAdapter
{
    /// <summary>
    /// ドメインバリデーションを実行し、結果を EditContext に反映
    /// </summary>
    public static bool Validate(
        this ValidationMessageStore messageStore,
        EditContext editContext,
        IEnumerable<ValidationError> errors)
    {
        messageStore.Clear();
        var hasErrors = messageStore.AddDomainErrors(editContext, errors);
        editContext.NotifyValidationStateChanged();
        return !hasErrors;
    }

    public static bool AddDomainErrors(
        this ValidationMessageStore messageStore,
        EditContext editContext,
        IEnumerable<ValidationError> errors)
    {
        var hasErrors = false;
        foreach (var error in errors)
        {
            var fieldIdentifier = new FieldIdentifier(editContext.Model, error.Property);
            messageStore.Add(fieldIdentifier, error.Message);
            hasErrors = true;
        }

        return hasErrors;
    }
}
