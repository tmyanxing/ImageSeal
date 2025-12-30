import axios from 'axios'

const request = axios.create({
  baseURL: '/api',
  timeout: 60000 // 60秒超时，处理大图片
})

// 响应拦截器
request.interceptors.response.use(
  response => response.data,
  error => {
    console.error('请求失败:', error)
    return Promise.reject(error)
  }
)

export const watermarkApi = {
  /**
   * 添加图片水印
   * @param {FormData} formData - 包含图片和水印参数的表单数据
   * @returns {Promise} API响应
   */
  addWatermark(formData) {
    return request.post('/watermark/add', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  },

  /**
   * 添加 PDF 水印
   * @param {FormData} formData - 包含 PDF 和水印参数的表单数据
   * @returns {Promise} API响应
   */
  addPdfWatermark(formData) {
    return request.post('/watermark/add-pdf', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  },

  /**
   * 健康检查
   */
  health() {
    return request.get('/watermark/health')
  }
}
