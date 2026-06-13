using FluentValidation;
using LeavePortal.Core.Commands.Leave;

namespace LeavePortal.Core.Validators.Leave;

public class RejectLeaveCommandValidator : AbstractValidator<RejectLeaveCommand>
{
    public RejectLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveApplicationId)
            .GreaterThan(0).WithMessage("A valid leave application id is required.");

        RuleFor(x => x.ManagerId)
            .GreaterThan(0).WithMessage("A valid authenticated manager is required.");

        // A rejection MUST explain why — the employee needs to know the reason.
        // NotEmpty rejects null, "", and whitespace-only. MaximumLength matches the
        // ReviewComment column (NVARCHAR(1000)).
        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("A comment is required when rejecting a leave application.")
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}
