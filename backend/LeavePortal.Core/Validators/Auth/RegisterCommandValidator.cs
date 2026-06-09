using FluentValidation;
using LeavePortal.Core.Commands.Auth;

namespace LeavePortal.Core.Validators.Auth;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(r => r == "Employee" || r == "Manager")
            .WithMessage("Role must be Employee or Manager.");

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("A valid department must be selected.");
    }
}
