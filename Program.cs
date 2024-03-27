using HotChocolate.Data.Filters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(x => x.UseInMemoryDatabase("Test"));

builder.Services.AddGraphQLServer()
    .AddQueryType()
    .AddFiltering()
    .RegisterDbContext<AppDbContext>()
    .AddType<Query>()
    .AddType<MachineType>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

class AppDbContext : DbContext
{
    public DbSet<Machine> Machines { get; set; }

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MachineLock>()
            .HasKey(x => new { x.MachineId, x.Lock });
    }
}

public class Machine
{
    public Guid Id { get; set; }
    public ISet<MachineLock> Locks { get; set; } = new HashSet<MachineLock>();
}
public record MachineLock(Guid MachineId, LockType Lock);
public enum LockType
{
    Lock1,
    Lock2
}

[ExtendObjectType(OperationTypeNames.Query)]
class Query
{
    [UseFiltering<MachineFilterType>]
    public IQueryable<Machine> GetMachines(AppDbContext appDbContext)
    {
        return appDbContext.Machines.AsNoTracking();
    }
}
class MachineType : ObjectType<Machine>
{
    protected override void Configure(IObjectTypeDescriptor<Machine> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Id);
    }
}
class MachineFilterType : FilterInputType<Machine>
{
    protected override void Configure(IFilterInputTypeDescriptor<Machine> descriptor)
    {
        descriptor.Name("MachineFilterInput");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Locks.Select(y => y.Lock)).Name("locks");
    }
}