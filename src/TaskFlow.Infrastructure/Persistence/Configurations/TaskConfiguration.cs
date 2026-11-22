using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = TaskFlow.Domain.Tasks.Task;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo EF Core de la tabla `tasks` (contrato OpenAPI 3.1).</summary>
public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .ValueGeneratedOnAdd();

        builder.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(task => task.Description)
            .HasMaxLength(500);

        builder.Property(task => task.Status)
            .HasConversion(ContractEnumConverters.Status)
            .HasMaxLength(20);

        builder.Property(task => task.Priority)
            .HasConversion(ContractEnumConverters.Priority)
            .HasMaxLength(10);

        builder.Property(task => task.Assignee)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(task => task.DueDate)
            .HasColumnType("timestamptz");

        builder.Property(task => task.CreatedAt)
            .HasColumnType("timestamptz");

        builder.Property(task => task.UpdatedAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.Priority);
        builder.HasIndex(task => task.Assignee);
        builder.HasIndex(task => task.DueDate);
    }
}