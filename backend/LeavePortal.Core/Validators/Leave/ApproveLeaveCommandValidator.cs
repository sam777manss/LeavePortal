using FluentValidation;
using LeavePortal.Core.Commands.Leave;

namespace LeavePortal.Core.Validators.Leave;

public class ApproveLeaveCommandValidator : AbstractValidator<ApproveLeaveCommand>
{
    public ApproveLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveApplicationId)
            .GreaterThan(0).WithMessage("A valid leave application id is required.");

        RuleFor(x => x.ManagerId)
            .GreaterThan(0).WithMessage("A valid authenticated manager is required.");

        // Comment is optional on approval, but if one IS provided it must fit the
        // ReviewComment column (NVARCHAR(1000)). We only check length when not null.
        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.")
            .When(x => x.Comment is not null);
    }
}
