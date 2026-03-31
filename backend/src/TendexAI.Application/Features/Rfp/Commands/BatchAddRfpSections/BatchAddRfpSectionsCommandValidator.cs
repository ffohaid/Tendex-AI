using FluentValidation;

namespace TendexAI.Application.Features.Rfp.Commands.BatchAddRfpSections;

public sealed class BatchAddRfpSectionsCommandValidator : AbstractValidator<BatchAddRfpSectionsCommand>
{
    public BatchAddRfpSectionsCommandValidator()
    {
        RuleFor(x => x.CompetitionId)
            .NotEmpty().WithMessage("معرّف المنافسة مطلوب.");

        RuleFor(x => x.Sections)
            .NotEmpty().WithMessage("يجب إضافة قسم واحد على الأقل.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("معرّف المستخدم مطلوب.");

        RuleForEach(x => x.Sections).ChildRules(section =>
        {
            section.RuleFor(s => s.TitleAr)
                .NotEmpty().WithMessage("عنوان القسم بالعربية مطلوب.")
                .MaximumLength(500).WithMessage("عنوان القسم يجب ألا يتجاوز 500 حرف.");
        });
    }
}
