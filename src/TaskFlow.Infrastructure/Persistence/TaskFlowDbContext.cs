using Microsoft.EntityFrameworkCore;
using TaskEntity = TaskFlow.Domain.Tasks.Task;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>Unidad de persistencia EF Core sobre PostgreSQL Cloud (vendor-agnostic).</summary>
public sealed class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
    }
}