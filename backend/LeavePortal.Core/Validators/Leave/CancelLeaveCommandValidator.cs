using FluentValidation;
using LeavePortal.Core.Commands.Leave;

namespace LeavePortal.Core.Validators.Leave;

public class CancelLeaveCommandValidator : AbstractValidator<CancelLeaveCommand>
{
    public CancelLeaveCommandValidator()
    {
        RuleFor(x => x.LeaveApplicationId)
            .GreaterThan(0).WithMessage("A valid leave application id is required.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("A valid authenticated user is required.");
    }
}
