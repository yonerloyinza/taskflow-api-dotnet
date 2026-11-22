using FluentValidation;
using TaskFlow.Domain.Tasks;

namespace TaskFlow.Application.Tasks;

/// <summary>Validación de `CreateTaskRequest` (title 3-100, description ≤500, assignee ≤100, dueDate obligatoria).</summary>
public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty().WithMessage("title is required")
            .MinimumLength(3).WithMessage("title must be at least 3 characters")
            .MaximumLength(100).WithMessage("title must be at most 100 characters");

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("description must be at most 500 characters");

        RuleFor(request => request.Assignee)
            .NotEmpty().WithMessage("assignee is required")
            .MaximumLength(100).WithMessage("assignee must be at most 100 characters");

        RuleFor(request => request.DueDate)
            .NotNull().WithMessage("dueDate is required");

        RuleFor(request => request.Status)
            .Must(BeAValidStatus).WithMessage("status must be one of: pendiente, en_curso, hecha")
            .When(request => !string.IsNullOrWhiteSpace(request.Status));

        RuleFor(request => request.Priority)
            .Must(BeAValidPriority).WithMessage("priority must be one of: baja, media, alta")
            .When(request => !string.IsNullOrWhiteSpace(request.Priority));
    }

    private static bool BeAValidStatus(string? value) => TaskStatusExtensions.TryParseContract(value, out _);

    private static bool BeAValidPriority(string? value) => TaskPriorityExtensions.TryParseContract(value, out _);
}

/// <summary>Validación de `UpdateTaskRequest`: todos los campos son opcionales y se validan solo si vienen.</summary>
public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .MinimumLength(3).WithMessage("title must be at least 3 characters")
            .MaximumLength(100).WithMessage("title must be at most 100 characters")
            .When(request => request.Title is not null);

        RuleFor(request => request.Description)
            .MaximumLength(500).WithMessage("description must be at most 500 characters")
            .When(request => request.Description is not null);

        RuleFor(request => request.Assignee)
            .NotEmpty().WithMessage("assignee cannot be empty")
            .MaximumLength(100).WithMessage("assignee must be at most 100 characters")
            .When(request => request.Assignee is not null);

        RuleFor(request => request.Status)
            .Must(BeAValidStatus).WithMessage("status must be one of: pendiente, en_curso, hecha")
            .When(request => !string.IsNullOrWhiteSpace(request.Status));

        RuleFor(request => request.Priority)
            .Must(BeAValidPriority).WithMessage("priority must be one of: baja, media, alta")
            .When(request => !string.IsNullOrWhiteSpace(request.Priority));
    }

    private static bool BeAValidStatus(string? value) => TaskStatusExtensions.TryParseContract(value, out _);

    private static bool BeAValidPriority(string? value) => TaskPriorityExtensions.TryParseContract(value, out _);
}