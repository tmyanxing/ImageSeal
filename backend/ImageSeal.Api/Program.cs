using ImageSeal.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 配置 Kestrel 监听端口
builder.WebHost.UseUrls("http://*:30032");

// 添加控制器
builder.Services.AddControllers();

// 注册服务
builder.Services.AddScoped<IWatermarkService, WatermarkService>();
builder.Services.AddScoped<IPdfWatermarkService, PdfWatermarkService>();

// 配置 CORS（允许前端调用）
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ImageSeal API", Version = "v1" });
});

// 配置上传文件大小限制 (50MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 52428800; // 50MB
});

var app = builder.Build();

// 启用 Swagger
app.UseSwagger();
app.UseSwaggerUI();

// 启用 CORS
app.UseCors("AllowAll");

// 启用静态文件（托管前端页面）
app.UseDefaultFiles();
app.UseStaticFiles();

// 启用路由
app.UseRouting();

// 映射控制器
app.MapControllers();

// 所有未匹配的路由返回 index.html（支持 Vue Router 的 history 模式）
app.MapFallbackToFile("index.html");

Console.WriteLine("============================================");
Console.WriteLine("    ImageSeal - 图片水印工具已启动");
Console.WriteLine("============================================");
Console.WriteLine("应用地址: http://localhost:30032");
Console.WriteLine("API 文档: http://localhost:30032/swagger");
Console.WriteLine("============================================");
Console.WriteLine("按 Ctrl+C 退出程序");

app.Run();
