using Microsoft.Extensions.Options;

namespace EFCore.Options;
public class DatabaseOptionsSetup : IConfigureOptions<DatabaseOptions>
{
    private const string ConfigurationSectionName = "DatabaseOptions";
    private readonly IConfiguration configuration;

    public DatabaseOptionsSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }
    public void Configure(DatabaseOptions options)
    {
        var connectionString = this.configuration.GetConnectionString("Database");

        options.ConnectionString = connectionString;

        this.configuration.GetSection(ConfigurationSectionName).Bind(options);
    }
}
