using iTextSharp.text;
using iTextSharp.text.pdf;
using ImageSeal.Api.Models;

namespace ImageSeal.Api.Services;

public interface IPdfWatermarkService
{
    Task<PdfWatermarkResult> AddWatermarkAsync(Stream pdfStream, string originalFileName, WatermarkRequest request);
}

public class PdfWatermarkService : IPdfWatermarkService
{
    private readonly ILogger<PdfWatermarkService> _logger;

    public PdfWatermarkService(ILogger<PdfWatermarkService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfWatermarkResult> AddWatermarkAsync(Stream pdfStream, string originalFileName, WatermarkRequest request)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var outputStream = new MemoryStream();
                
                // 读取原始 PDF
                var reader = new PdfReader(pdfStream);
                var stamper = new PdfStamper(reader, outputStream);
                
                // 获取字体（支持中文）
                var fontPath = GetChineseFontPath();
                BaseFont baseFont;
                
                if (!string.IsNullOrEmpty(fontPath))
                {
                    // 检查是否是带索引的 TTC 字体路径
                    var actualPath = fontPath.Contains(',') ? fontPath : fontPath;
                    var pathToCheck = fontPath.Split(',')[0];
                    
                    if (File.Exists(pathToCheck))
                    {
                        baseFont = BaseFont.CreateFont(actualPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                        _logger.LogInformation("使用字体: {FontPath}", fontPath);
                    }
                    else
                    {
                        baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        _logger.LogWarning("字体文件不存在，使用 Helvetica");
                    }
                }
                else
                {
                    // 回退到系统字体
                    baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    _logger.LogWarning("未找到中文字体，使用 Helvetica");
                }
                
                // 解析颜色
                var color = ParseColor(request.Color);
                
                // 计算透明度 (0-100 转换为 0-1)
                float opacity = request.Opacity / 100f;
                
                // 为每一页添加水印
                int pageCount = reader.NumberOfPages;
                for (int i = 1; i <= pageCount; i++)
                {
                    var pageSize = reader.GetPageSizeWithRotation(i);
                    var pageWidth = pageSize.Width;
                    var pageHeight = pageSize.Height;
                    
                    // 获取内容层（水印在内容之上）
                    var canvas = stamper.GetOverContent(i);
                    
                    // 设置透明度
                    var gState = new PdfGState
                    {
                        FillOpacity = opacity,
                        StrokeOpacity = opacity
                    };
                    canvas.SetGState(gState);
                    
                    // 设置字体和颜色
                    canvas.SetColorFill(color);
                    canvas.BeginText();
                    canvas.SetFontAndSize(baseFont, request.FontSize);
                    
                    // 计算水印文字尺寸
                    float textWidth = baseFont.GetWidthPoint(request.WatermarkText, request.FontSize);
                    float textHeight = request.FontSize;
                    
                    // 计算间距
                    float spacing = request.Spacing > 0 ? request.Spacing : 100;
                    float horizontalSpacing = textWidth + spacing;
                    float verticalSpacing = textHeight + spacing;
                    
                    // 旋转角度（弧度）
                    float angleRadians = request.Angle * (float)Math.PI / 180f;
                    
                    // 计算需要覆盖的范围
                    float diagonal = (float)Math.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
                    
                    // 计算行数和列数
                    int cols = (int)Math.Ceiling(diagonal * 2 / horizontalSpacing) + 2;
                    int rows = (int)Math.Ceiling(diagonal * 2 / verticalSpacing) + 2;
                    
                    // 中心点
                    float centerX = pageWidth / 2f;
                    float centerY = pageHeight / 2f;
                    
                    // 绘制平铺水印
                    for (int row = -rows / 2; row <= rows / 2; row++)
                    {
                        for (int col = -cols / 2; col <= cols / 2; col++)
                        {
                            // 计算未旋转的位置（相对于中心）
                            float x = col * horizontalSpacing;
                            float y = row * verticalSpacing;
                            
                            // 应用旋转变换
                            float rotatedX = x * (float)Math.Cos(angleRadians) - y * (float)Math.Sin(angleRadians);
                            float rotatedY = x * (float)Math.Sin(angleRadians) + y * (float)Math.Cos(angleRadians);
                            
                            // 转换为页面坐标
                            float finalX = centerX + rotatedX;
                            float finalY = centerY + rotatedY;
                            
                            // 检查是否在页面范围内（带一些余量）
                            if (finalX > -textWidth && finalX < pageWidth + textWidth &&
                                finalY > -textHeight && finalY < pageHeight + textHeight)
                            {
                                canvas.ShowTextAligned(
                                    Element.ALIGN_CENTER,
                                    request.WatermarkText,
                                    finalX,
                                    finalY,
                                    request.Angle
                                );
                            }
                        }
                    }
                    
                    canvas.EndText();
                }
                
                stamper.Close();
                reader.Close();
                
                var base64 = Convert.ToBase64String(outputStream.ToArray());
                var outputFileName = Path.GetFileNameWithoutExtension(originalFileName) + "_watermarked.pdf";
                
                return new PdfWatermarkResult
                {
                    PdfBase64 = base64,
                    MimeType = "application/pdf",
                    FileName = outputFileName,
                    PageCount = pageCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加 PDF 水印失败");
                throw;
            }
        });
    }
    
    private static string GetChineseFontPath()
    {
        var fontsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        
        // 按优先级尝试查找字体（优先使用 TTF 格式，避免 TTC 兼容性问题）
        string[] fontFiles = new[]
        {
            "simhei.ttf",    // 黑体
            "simkai.ttf",    // 楷体
            "STFANGSO.TTF",  // 华文仿宋
            "STSONG.TTF",    // 华文宋体
            "msyh.ttc,0",    // 微软雅黑 (TTC 需要指定索引)
            "simsun.ttc,0",  // 宋体 (TTC 需要指定索引)
        };
        
        foreach (var fontFile in fontFiles)
        {
            var parts = fontFile.Split(',');
            var fileName = parts[0];
            var fontPath = Path.Combine(fontsFolder, fileName);
            if (File.Exists(fontPath))
            {
                // 如果是 TTC 文件，返回带索引的路径
                if (parts.Length > 1)
                {
                    return $"{fontPath},{parts[1]}";
                }
                return fontPath;
            }
        }
        
        return string.Empty;
    }
    
    private static BaseColor ParseColor(string colorHex)
    {
        try
        {
            colorHex = colorHex.TrimStart('#');
            
            int r = Convert.ToInt32(colorHex.Substring(0, 2), 16);
            int g = Convert.ToInt32(colorHex.Substring(2, 2), 16);
            int b = Convert.ToInt32(colorHex.Substring(4, 2), 16);
            
            return new BaseColor(r, g, b);
        }
        catch
        {
            return new BaseColor(128, 128, 128); // 默认灰色
        }
    }
}
