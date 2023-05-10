using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
                
            }));

    options.AddFixedWindowLimiter("Fixed", _options =>
    {
        _options.Window = TimeSpan.FromSeconds(12);
        _options.PermitLimit = 4;
        _options.QueueLimit = 2;
        _options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        
    });
    options.AddSlidingWindowLimiter("Sliding", _options =>
    {
        _options.Window = TimeSpan.FromSeconds(12);
        _options.PermitLimit = 4; // Her periyotta işlenecek istek sayısı
        _options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        _options.QueueLimit = 2; // kuyrukta bekleyecek istek sayısı 
        _options.SegmentsPerWindow = 2; // Bir sonraki periyotta kullanılacak istek sayısı 
    });
    options.AddTokenBucketLimiter("Token", _options =>
    {
        _options.TokenLimit = 4; 
        _options.TokensPerPeriod = 4;
        _options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        _options.QueueLimit = 2;
        _options.ReplenishmentPeriod = TimeSpan.FromSeconds(12);
    });

    options.AddConcurrencyLimiter("Conccurency", _options =>
    {
        _options.PermitLimit = 4;
        _options.QueueLimit = 2;
        _options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddPolicy<string, CustomRateLimitPolicy>("CustomPolicy");
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync(
                $"Çok fazla istekde bulundunuz. Lütfen sonra tekrar deneyin {retryAfter.TotalMinutes} dakika. ", cancellationToken: token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Çok fazla istekde bulundunuz. Lütfen sonra tekrar deneyin. ", cancellationToken: token);
        }
    };
});


var app = builder.Build();

//app.MapGet("/", () =>
//{

//});

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


