var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration; // 取得 IConfiguration
// 取得檔案儲存位置
var fileStorePath = config.GetValue<string>("FileStorePath");
Directory.CreateDirectory(fileStorePath);
// 取得允許的副檔名
var allowExts = config.GetSection("AllowExts").Get<string[]>();
// 加入 CORS 服務
const string OriginsFromSetting = "OriginsFromAppSettingsJson";
builder.Services.AddCors(options => {
    options.AddPolicy(
        name: OriginsFromSetting,
        builder => {
            builder.WithOrigins(
                // 轉 string[] 需要 Microsoft.Extensions.Configuration.Binder
                config.GetSection("AllowOrigins").Get<string[]>());
        }
    );
});
var app = builder.Build();
// 啟用 CORS Middleware
app.UseCors();
app.MapGet("/", () => "Hello World!");
app.MapPost("/", async (context) => {
    var fileName = context.Request.Query["f"].FirstOrDefault();
    var res = "Unknown";
    if (string.IsNullOrEmpty(fileName) || 
        fileName.IndexOfAny(Path.GetInvalidPathChars()) > -1) 
        res = "Invalid filename";
    else if (!allowExts.Contains(Path.GetExtension(fileName))) 
        res = "Invalid file type";
    else {
        // 只保留檔名(去除可能夾帶的路徑)加上時間序號
        fileName = DateTime.Now.ToString("yyyyMMdd-HHmmss-") + Path.GetFileName(fileName);
        using (var ms = new MemoryStream()) 
        {
            await context.Request.Body.CopyToAsync(ms);
            var filePath = Path.Combine(fileStorePath, fileName);
            var data = ms.ToArray();
            if (context.Request.Query["t"].FirstOrDefault() == "base64") {
                data = Convert.FromBase64String(System.Text.Encoding.UTF8.GetString(data));
            }
            File.WriteAllBytes(filePath, data);
            res = "OK";
        }
    }
    await context.Response.WriteAsync(res);
}).RequireCors(OriginsFromSetting);
app.Run();