using FluentValidation;

namespace SkFileBlog.Features.Auth.Login;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters");
            
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}