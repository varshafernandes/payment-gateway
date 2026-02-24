using PaymentGateway.Api.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsonConfiguration();
builder.Services.AddObservability();
builder.Services.AddMediatRConfiguration();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSwaggerConfiguration();
builder.Services.AddHealthCheckConfiguration();

var app = builder.Build();

app.UseObservability();
app.UseSwaggerIfDevelopment();
app.MapHealthCheckEndpoints();
app.MapPaymentEndpoints();

app.Run();

public partial class Program;

