using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimiting.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [EnableRateLimiting("Fixed")]
        public IActionResult Get()
        {
            return Ok();
        }

        [EnableRateLimiting("Sliding")]
        [HttpGet]
        [Route("TestRateLimitWeb")]
        public int GetCurrentTime()
        {
            return DateTime.Now.Second;
        }


        [EnableRateLimiting("Conccurency")]
        [HttpGet("Conccurency")]
        public async Task<IActionResult> GetAsync()
        {
            await Task.Delay(20000);
            return Ok();
        }
     
    }
}

#region Rate Limit


#region Rate Limiter Algoritmaları Nelerdir?

#region Fixed Window
//Sabit bir zaman aralığı kullanarak istekleri sınırlandıran algoritmadır.
#endregion
#region Sliding Window
//Fixed Windows algoritmasına benzerlik göstermektedir. Her sabit sürede bir zamana aralığında istekleri sınırlandırmamaktadır. Lakin sürenin yaırısndan sonra diğer periyodun request kotasını harcayacak şekilde istekleri karşılar.
#endregion
#region Token Bucket
//Akış hızını kontrol etmenizi sağlar. Bu Token Kovası olarak bilinen bir algoritmadır. Jetonla ağzına kadar dolu bir kovamızın olduğunu, hayal edelim. Bir istek gelince, bir jeton alır ve onu saklarız. Tanımlanan süre sonunda, belirlenmiş miktar kadar jetonu tekrar kovaya koyarız. Asla kovanın tutabileceğinden fazlasını eklemeyiz. Kova boş ise istek geldiğinde bunu reddederiz. Örnek olarak, kovaya 10 jeton koyalım ve her dakika, 2 jeton kovaya eklendiğini varsayalım. Diyelim 1 istek gelisin, geriye 9 (10 -1)Jeton kalsın. Sonra 3 istek daha gelsin geriye 6 (9 – 3) Jeton kalsın. Diyelim 1 dakika geçti ve kovaya 2 Jeton daha geldi. Kovamızda 8 (6 + 2) jeton oldu. Bir anda 8 istek geldi ve kovada hiç jeton (0) (8 -8) kalmadı. Yeni bir istek geldiğinde, kaynağa erişime izin verilmedi. Diyelim ki 5 dakika boyunca hiçbir istek gelmedi ve böylece kovamızda 10 (2*5) jeton birikti. İstek almamaya devam etesek bile, her 1 dakikada bir 2 yeni jeton artık kovaya konmayacak çünkü kovanın limitine gelinmiş olacaktır.
#endregion
#region Concurrency
//Asenkron requestleri sınırlanmak için kullanılan bir algoritmadır. Her istek concurrency sınırını bir azaltmakta ve bittikleri taktirde bu sınırı bir arttırmaktadırlar. Diğer algoritmalara nazaran sadece asenkron requestleri sınırlandırır. 
#endregion

#endregion

#region Attribute'lar
#region EnableRateLimiting
//Controller yahut action seviyesinde istenilen politikada rate limiti devreye sokmamızı sağlayan bir attribute'dur.
#endregion
#region DisableRateLimiting
//Controller seviyesinde devreye sokulmuş bir rate limit politikasıonın action seviyesinde pasifleştirilmesini sağlayan bir attributedur.
#endregion
#endregion

#region Minimal API'lar da Rate Limiting
//RequireRateLimiting
#endregion

#region OnRejected Property'si
//Rate limit uygulanan operasyonlarda sınırdan dolayı boşa çıkan request'lerin söz konusu olduğu durumlarda loglama vs. gibi işlemleri yapabilmek için kullandıuımız event mantığında bir properydir.
#endregion

#region Özelleştirilmiş Rate Limit Policy Oluşturma
class CustomRateLimitPolicy : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected =>
        (context, cancellationToken) =>
        {
            //Log...
            return new();
        };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartition.GetFixedWindowLimiter("", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 4,
            Window = TimeSpan.FromSeconds(12),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 2
        });
    }
}
#endregion
#endregion


