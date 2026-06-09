using FluentValidation;
using LeavePortal.Core.Commands.Leave;

namespace LeavePortal.Core.Validators.Leave;

public class ApplyLeaveCommandValidator : AbstractValidator<ApplyLeaveCommand>
{
    public ApplyLeaveCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("A valid authenticated user is required.");

        RuleFor(x => x.LeaveTypeId)
            .GreaterThan(0).WithMessage("A valid leave type must be selected.");

        RuleFor(x => x.StartDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters.");
    }
}
