using FluentValidation;

namespace SkFileBlog.Features.Posts.Update;

public class UpdatePostValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");
            
        RuleFor(x => x.Slug)
            .MaximumLength(200).WithMessage("Slug cannot exceed 200 characters");
            
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
            
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required");
            
        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tags cannot be empty")
            .MaximumLength(50).WithMessage("Tag cannot exceed 50 characters");
            
        RuleForEach(x => x.Categories)
            .NotEmpty().WithMessage("Categories cannot be empty")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");
    }
}