namespace ImageSeal.Api.Models;

/// <summary>
/// 水印请求模型
/// </summary>
public class WatermarkRequest
{
    /// <summary>
    /// 水印文字
    /// </summary>
    public string WatermarkText { get; set; } = string.Empty;
    
    /// <summary>
    /// 字体大小 (默认 30)
    /// </summary>
    public int FontSize { get; set; } = 30;
    
    /// <summary>
    /// 水印颜色 (默认 #888888)
    /// </summary>
    public string Color { get; set; } = "#888888";
    
    /// <summary>
    /// 透明度 (0-100, 默认 30)
    /// </summary>
    public int Opacity { get; set; } = 30;
    
    /// <summary>
    /// 旋转角度 (默认 -30 度)
    /// </summary>
    public float Angle { get; set; } = -30f;
    
    /// <summary>
    /// 水印间距 (默认 100)
    /// </summary>
    public int Spacing { get; set; } = 100;
}

/// <summary>
/// API 响应模型
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

/// <summary>
/// 水印结果模型
/// </summary>
public class WatermarkResult
{
    /// <summary>
    /// Base64 编码的图片数据
    /// </summary>
    public string ImageBase64 { get; set; } = string.Empty;
    
    /// <summary>
    /// 图片 MIME 类型
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
