using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// Add rate limiting
var rateLimiterOptions = new RateLimiterOptions();

rateLimiterOptions.AddConcurrencyLimiter(
    policyName: "get",
    concurrencyLimiterOptions: new ConcurrencyLimiterOptions(
        permitLimit: 2,
        queueProcessingOrder: QueueProcessingOrder.OldestFirst,
        queueLimit: 2
    )
);

rateLimiterOptions.AddNoLimiter(policyName: "admin");

rateLimiterOptions.AddPolicy(
    policyName: "post",
    partitioner: httpContext => {
        if (!StringValues.IsNullOrEmpty(httpContext.Request.Headers["token"])) {
            return RateLimitPartition.CreateTokenBucketLimiter("token", key =>
                new TokenBucketRateLimiterOptions(
                    tokenLimit: 5,
                    queueProcessingOrder: QueueProcessingOrder.OldestFirst,
                    queueLimit: 1,
                    replenishmentPeriod: TimeSpan.FromSeconds(5),
                    tokensPerPeriod: 1,
                    autoReplenishment: true)
                );
        } else { return RateLimitPartition.Create("default", key => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions(
            permitLimit: 2,
            queueProcessingOrder: QueueProcessingOrder.OldestFirst,
            queueLimit: 1,
            window: TimeSpan.FromSeconds(10),
            autoReplenishment: true))
        ); }
    }
);

app.UseRateLimiter(rateLimiterOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// app.UseAuthorization();

// app.MapControllers();

app.MapGet("/get", context => context.Response.WriteAsync("get")).RequireRateLimiting("get");

app.MapGet("/admin", context => context.Response.WriteAsync("admin")).RequireRateLimiting("admin").RequireAuthorization("admin");

app.MapPost("/post", context => context.Response.WriteAsync("post")).RequireRateLimiting("post");

app.Run();
