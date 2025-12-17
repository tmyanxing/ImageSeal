using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using ImageSeal.Api.Models;
using System.Numerics;

namespace ImageSeal.Api.Services;

public interface IWatermarkService
{
    Task<WatermarkResult> AddWatermarkAsync(Stream imageStream, string originalFileName, WatermarkRequest request);
}

public class WatermarkService : IWatermarkService
{
    private readonly ILogger<WatermarkService> _logger;
    private readonly FontFamily _fontFamily;

    public WatermarkService(ILogger<WatermarkService> logger)
    {
        _logger = logger;
        
        // 加载支持中文的字体
        var fontCollection = new FontCollection();
        var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        
        // 按优先级尝试加载字体
        string[] fontFiles = new[]
        {
            "msyh.ttc",      // 微软雅黑
            "msyhbd.ttc",    // 微软雅黑粗体
            "simsun.ttc",    // 宋体
            "simhei.ttf",    // 黑体
            "simkai.ttf",    // 楷体
        };
        
        FontFamily loadedFamily = default;
        bool fontLoaded = false;
        
        foreach (var fontFile in fontFiles)
        {
            try
            {
                var fontPath = Path.Combine(fontsFolder, fontFile);
                if (File.Exists(fontPath))
                {
                    loadedFamily = fontCollection.Add(fontPath);
                    _logger.LogInformation("成功加载字体: {FontFile}, 字体名: {FontName}", fontFile, loadedFamily.Name);
                    fontLoaded = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("加载字体 {FontFile} 失败: {Message}", fontFile, ex.Message);
            }
        }
        
        if (!fontLoaded)
        {
            // 最后尝试使用系统字体
            if (SystemFonts.TryGet("Microsoft YaHei", out loadedFamily))
            {
                _logger.LogInformation("使用系统字体: Microsoft YaHei");
            }
            else if (SystemFonts.TryGet("SimSun", out loadedFamily))
            {
                _logger.LogInformation("使用系统字体: SimSun");
            }
            else if (SystemFonts.TryGet("Arial", out loadedFamily))
            {
                _logger.LogWarning("未找到中文字体，使用 Arial");
            }
            else
            {
                throw new Exception("无法加载任何字体");
            }
        }
        
        _fontFamily = loadedFamily;
    }

    public async Task<WatermarkResult> AddWatermarkAsync(Stream imageStream, string originalFileName, WatermarkRequest request)
    {
        try
        {
            using var image = await Image.LoadAsync<Rgba32>(imageStream);
            
            // 创建水印字体
            var fontSize = request.FontSize > 0 ? request.FontSize : 30;
            var watermarkFont = _fontFamily.CreateFont(fontSize, FontStyle.Regular);
            
            // 解析颜色和透明度
            var color = ParseColor(request.Color, request.Opacity);
            
            // 计算水印尺寸
            var textOptions = new RichTextOptions(watermarkFont)
            {
                Origin = new PointF(0, 0)
            };
            var textBounds = TextMeasurer.MeasureBounds(request.WatermarkText, textOptions);
            var textWidth = textBounds.Width;
            var textHeight = textBounds.Height;
            
            // 计算间距
            var spacing = request.Spacing > 0 ? request.Spacing : 100;
            var horizontalSpacing = textWidth + spacing;
            var verticalSpacing = textHeight + spacing;
            
            // 旋转角度（弧度）
            var angleRadians = request.Angle * MathF.PI / 180f;
            
            // 计算旋转后需要覆盖的范围（需要更大的范围以确保覆盖整个图片）
            var diagonal = MathF.Sqrt(image.Width * image.Width + image.Height * image.Height);
            var expandedWidth = diagonal * 2;
            var expandedHeight = diagonal * 2;
            
            // 绘制水印
            image.Mutate(ctx =>
            {
                // 中心点
                var centerX = image.Width / 2f;
                var centerY = image.Height / 2f;
                
                // 计算需要绘制的行数和列数
                var cols = (int)Math.Ceiling(expandedWidth / horizontalSpacing) + 2;
                var rows = (int)Math.Ceiling(expandedHeight / verticalSpacing) + 2;
                
                // 从中心向外绘制水印
                for (int row = -rows / 2; row <= rows / 2; row++)
                {
                    for (int col = -cols / 2; col <= cols / 2; col++)
                    {
                        // 计算未旋转的位置（相对于中心）
                        var x = col * horizontalSpacing;
                        var y = row * verticalSpacing;
                        
                        // 应用旋转变换
                        var rotatedX = x * MathF.Cos(angleRadians) - y * MathF.Sin(angleRadians);
                        var rotatedY = x * MathF.Sin(angleRadians) + y * MathF.Cos(angleRadians);
                        
                        // 转换为图片坐标
                        var finalX = centerX + rotatedX - textWidth / 2;
                        var finalY = centerY + rotatedY - textHeight / 2;
                        
                        // 检查是否在图片范围内（带一些余量）
                        if (finalX > -textWidth && finalX < image.Width + textWidth &&
                            finalY > -textHeight && finalY < image.Height + textHeight)
                        {
                            var drawOptions = new RichTextOptions(watermarkFont)
                            {
                                Origin = new PointF(finalX + textWidth / 2, finalY + textHeight / 2),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            
                            // 创建旋转变换
                            ctx.SetDrawingTransform(Matrix3x2.CreateRotation(angleRadians, new Vector2(finalX + textWidth / 2, finalY + textHeight / 2)));
                            ctx.DrawText(drawOptions, request.WatermarkText, color);
                            ctx.SetDrawingTransform(Matrix3x2.Identity);
                        }
                    }
                }
            });
            
            // 保存结果
            using var outputStream = new MemoryStream();
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var mimeType = "image/png";
            var outputFileName = Path.GetFileNameWithoutExtension(originalFileName) + "_watermarked.png";
            
            await image.SaveAsPngAsync(outputStream);
            
            var base64 = Convert.ToBase64String(outputStream.ToArray());
            
            return new WatermarkResult
            {
                ImageBase64 = base64,
                MimeType = mimeType,
                FileName = outputFileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加水印失败");
            throw;
        }
    }
    
    private static Color ParseColor(string colorHex, int opacity)
    {
        try
        {
            // 移除 # 前缀
            colorHex = colorHex.TrimStart('#');
            
            // 解析 RGB 值
            byte r = Convert.ToByte(colorHex.Substring(0, 2), 16);
            byte g = Convert.ToByte(colorHex.Substring(2, 2), 16);
            byte b = Convert.ToByte(colorHex.Substring(4, 2), 16);
            
            // 计算透明度 (0-100 转换为 0-255)
            byte a = (byte)(opacity * 255 / 100);
            
            return Color.FromRgba(r, g, b, a);
        }
        catch
        {
            // 默认颜色
            return Color.FromRgba(128, 128, 128, 77); // 30% 透明度的灰色
        }
    }
}
