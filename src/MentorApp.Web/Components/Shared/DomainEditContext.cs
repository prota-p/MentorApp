using MentorApp.Domain.Models.Shared;
using Microsoft.AspNetCore.Components.Forms;

namespace MentorApp.Web.Components.Shared;

/// <summary>
/// ドメインバリデーション機能を統合した EditContext ラッパー
/// </summary>
public class DomainEditContext
{
    private readonly EditContext editContext;
    private readonly ValidationMessageStore messageStore;
    private Func<IEnumerable<ValidationError>>? validator;

    public DomainEditContext(object model)
    {
        editContext = new EditContext(model);
        messageStore = new ValidationMessageStore(editContext);
        editContext.OnFieldChanged += (_, _) => Validate();
    }

    public EditContext EditContext => editContext;

    /// <summary>
    /// プロパティ単位のバリデーションを設定（DisplayName のみなど）
    /// </summary>
    public void SetValidator(params (string propertyName, Func<IEnumerable<string>> validator)[] validations)
    {
        validator = () => validations.SelectMany(v =>
            v.validator().ToValidationErrors(v.propertyName));
    }

    /// <summary>
    /// 複数フィールドを横断するバリデーションを設定（Topic.Validate など）
    /// </summary>
    public void SetValidator(Func<IEnumerable<ValidationError>> validator)
    {
        this.validator = validator;
    }

    public bool Validate()
    {
        if (validator is null) return true;

        var errors = validator();
        return messageStore.Validate(editContext, errors);
    }
}
