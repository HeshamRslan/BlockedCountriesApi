using BlockedCountriesApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TemporalBlockStore>();
builder.Services.AddHostedService<TemporalBlockCleanupService>();
builder.Services.AddSingleton<BlockedCountryStore>(); 
builder.Services.AddSingleton<BlockedAttemptLogStore>();
builder.Services.AddSingleton<IGeoIpService, GeoIpService>();
builder.Services.AddHttpClient();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
