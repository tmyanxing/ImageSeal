using Microsoft.AspNetCore.Mvc;
using ImageSeal.Api.Models;
using ImageSeal.Api.Services;

namespace ImageSeal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WatermarkController : ControllerBase
{
    private readonly IWatermarkService _watermarkService;
    private readonly IPdfWatermarkService _pdfWatermarkService;
    private readonly ILogger<WatermarkController> _logger;

    public WatermarkController(
        IWatermarkService watermarkService, 
        IPdfWatermarkService pdfWatermarkService,
        ILogger<WatermarkController> logger)
    {
        _watermarkService = watermarkService;
        _pdfWatermarkService = pdfWatermarkService;
        _logger = logger;
    }

    /// <summary>
    /// 添加水印
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <param name="watermarkText">水印文字</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="color">水印颜色 (十六进制)</param>
    /// <param name="opacity">透明度 (0-100)</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="spacing">水印间距</param>
    /// <returns>带水印的图片 Base64</returns>
    [HttpPost("add")]
    public async Task<ApiResponse<WatermarkResult>> AddWatermark(
        IFormFile file,
        [FromForm] string watermarkText,
        [FromForm] int fontSize = 30,
        [FromForm] string color = "#888888",
        [FromForm] int opacity = 30,
        [FromForm] float angle = -30f,
        [FromForm] int spacing = 100)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<WatermarkResult>
                {
                    Success = false,
                    Message = "请上传图片文件",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(watermarkText))
            {
                return new ApiResponse<WatermarkResult>
                {
                    Success = false,
                    Message = "请输入水印文字",
                    Data = null
                };
            }

            // 验证文件类型
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return new ApiResponse<WatermarkResult>
                {
                    Success = false,
                    Message = "不支持的图片格式，请上传 JPG、PNG、GIF、BMP 或 WebP 格式的图片",
                    Data = null
                };
            }

            var request = new WatermarkRequest
            {
                WatermarkText = watermarkText,
                FontSize = fontSize,
                Color = color,
                Opacity = opacity,
                Angle = angle,
                Spacing = spacing
            };

            using var stream = file.OpenReadStream();
            var result = await _watermarkService.AddWatermarkAsync(stream, file.FileName, request);

            _logger.LogInformation("水印添加成功: {FileName}", file.FileName);

            return new ApiResponse<WatermarkResult>
            {
                Success = true,
                Message = "水印添加成功",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加水印失败");
            return new ApiResponse<WatermarkResult>
            {
                Success = false,
                Message = $"添加水印失败: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 为 PDF 添加水印
    /// </summary>
    /// <param name="file">PDF 文件</param>
    /// <param name="watermarkText">水印文字</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="color">水印颜色 (十六进制)</param>
    /// <param name="opacity">透明度 (0-100)</param>
    /// <param name="angle">旋转角度</param>
    /// <param name="spacing">水印间距</param>
    /// <returns>带水印的 PDF Base64</returns>
    [HttpPost("add-pdf")]
    public async Task<ApiResponse<PdfWatermarkResult>> AddPdfWatermark(
        IFormFile file,
        [FromForm] string watermarkText,
        [FromForm] int fontSize = 30,
        [FromForm] string color = "#888888",
        [FromForm] int opacity = 30,
        [FromForm] float angle = -30f,
        [FromForm] int spacing = 100)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new ApiResponse<PdfWatermarkResult>
                {
                    Success = false,
                    Message = "请上传 PDF 文件",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(watermarkText))
            {
                return new ApiResponse<PdfWatermarkResult>
                {
                    Success = false,
                    Message = "请输入水印文字",
                    Data = null
                };
            }

            // 验证文件类型
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
            {
                return new ApiResponse<PdfWatermarkResult>
                {
                    Success = false,
                    Message = "请上传 PDF 格式的文件",
                    Data = null
                };
            }

            var request = new WatermarkRequest
            {
                WatermarkText = watermarkText,
                FontSize = fontSize,
                Color = color,
                Opacity = opacity,
                Angle = angle,
                Spacing = spacing
            };

            using var stream = file.OpenReadStream();
            var result = await _pdfWatermarkService.AddWatermarkAsync(stream, file.FileName, request);

            _logger.LogInformation("PDF 水印添加成功: {FileName}, 共 {PageCount} 页", file.FileName, result.PageCount);

            return new ApiResponse<PdfWatermarkResult>
            {
                Success = true,
                Message = $"PDF 水印添加成功，共处理 {result.PageCount} 页",
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加 PDF 水印失败");
            return new ApiResponse<PdfWatermarkResult>
            {
                Success = false,
                Message = $"添加 PDF 水印失败: {ex.Message}",
                Data = null
            };
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public ApiResponse<string> Health()
    {
        return new ApiResponse<string>
        {
            Success = true,
            Message = "服务正常运行",
            Data = "OK"
        };
    }
}
