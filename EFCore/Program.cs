using Dapper;
using EFCore;
using EFCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DatabaseContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPut("increase-salaries", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company == null)
        return Results.NotFound($"The company with Id '{companyId}' was not found");
    foreach (var employee in company.Employees)
        employee.Salary += 1.1m;

    company.LastSalaryUpdateUtc = DateTime.UtcNow;
    
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

// here we are avoiding parsistent inconsistency in db with transaction
app.MapPut("increase-salaries-sql", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company == null)
        return Results.NotFound($"The company with Id '{companyId}' was not found");

    await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database
        .ExecuteSqlInterpolatedAsync($"UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = {company.Id}");

    company.LastSalaryUpdateUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.MapPut("increase-salaries-sql-dapper", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(c => c.Id == companyId);

    if (company == null)
        return Results.NotFound($"The company with Id '{companyId}' was not found");

    var transaction = await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.GetDbConnection()
        .ExecuteAsync("UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId= @CompanyId",
        new { CompanyId = company.Id }, transaction.GetDbTransaction());

    company.LastSalaryUpdateUtc = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.Run();
