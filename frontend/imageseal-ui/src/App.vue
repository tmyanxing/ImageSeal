<template>
  <a-config-provider :locale="zhCN">
    <div class="app-container">
      <a-layout class="layout">
        <a-layout-header class="header">
          <div class="logo">
            <img src="/favicon.svg" alt="logo" class="logo-img" />
            <span class="logo-text">ImageSeal</span>
          </div>
          <span class="header-subtitle">图片水印工具</span>
        </a-layout-header>
        
        <a-layout-content class="content">
          <div class="main-container">
            <a-row :gutter="24">
              <!-- 左侧：上传和设置 -->
              <a-col :xs="24" :lg="8">
                <a-card title="📤 上传文件" class="upload-card">
                  <a-upload-dragger
                    v-model:fileList="fileList"
                    name="file"
                    :maxCount="1"
                    :beforeUpload="beforeUpload"
                    :showUploadList="false"
                    accept=".jpg,.jpeg,.png,.gif,.bmp,.webp,.pdf"
                    @change="handleFileChange"
                  >
                    <p class="ant-upload-drag-icon">
                      <inbox-outlined />
                    </p>
                    <p class="ant-upload-text">点击或拖拽文件到此区域</p>
                    <p class="ant-upload-hint">支持 JPG、PNG、GIF、BMP、WebP、PDF 格式</p>
                  </a-upload-dragger>
                  
                  <div v-if="originalImage && !isPdfFile" class="original-preview">
                    <a-divider>原图预览</a-divider>
                    <img :src="originalImage" alt="原图" class="preview-img" />
                    <p class="file-name">{{ selectedFileName }}</p>
                  </div>
                  
                  <div v-if="isPdfFile" class="original-preview">
                    <a-divider>PDF 文件</a-divider>
                    <div class="pdf-info">
                      <file-pdf-outlined style="font-size: 48px; color: #ff4d4f;" />
                      <p class="file-name">{{ selectedFileName }}</p>
                    </div>
                  </div>
                </a-card>

                <a-card title="⚙️ 水印设置" class="settings-card">
                  <a-form :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }">
                    <a-form-item label="水印文字">
                      <a-input 
                        v-model:value="watermarkSettings.text" 
                        placeholder="请输入水印文字"
                        allow-clear
                      />
                    </a-form-item>
                    
                    <a-form-item label="字体大小">
                      <a-slider 
                        v-model:value="watermarkSettings.fontSize" 
                        :min="10" 
                        :max="100"
                        :marks="{ 10: '10', 30: '30', 50: '50', 100: '100' }"
                      />
                    </a-form-item>
                    
                    <a-form-item label="水印颜色">
                      <div class="color-picker-wrapper">
                        <input 
                          type="color" 
                          v-model="watermarkSettings.color" 
                          class="color-picker"
                        />
                        <a-input 
                          v-model:value="watermarkSettings.color" 
                          style="width: 100px; margin-left: 10px;"
                        />
                      </div>
                    </a-form-item>
                    
                    <a-form-item label="透明度">
                      <a-slider 
                        v-model:value="watermarkSettings.opacity" 
                        :min="5" 
                        :max="100"
                        :tip-formatter="val => `${val}%`"
                        :marks="{ 5: '5%', 30: '30%', 50: '50%', 100: '100%' }"
                      />
                    </a-form-item>
                    
                    <a-form-item label="旋转角度">
                      <a-slider 
                        v-model:value="watermarkSettings.angle" 
                        :min="-90" 
                        :max="90"
                        :tip-formatter="val => `${val}°`"
                        :marks="{ '-90': '-90°', '-30': '-30°', 0: '0°', 30: '30°', 90: '90°' }"
                      />
                    </a-form-item>
                    
                    <a-form-item label="水印间距">
                      <a-slider 
                        v-model:value="watermarkSettings.spacing" 
                        :min="20" 
                        :max="300"
                        :marks="{ 20: '20', 100: '100', 200: '200', 300: '300' }"
                      />
                    </a-form-item>
                  </a-form>
                  
                  <a-button 
                    type="primary" 
                    size="large" 
                    block 
                    :loading="loading"
                    :disabled="!selectedFile || !watermarkSettings.text"
                    @click="generateWatermark"
                  >
                    <template #icon><thunderbolt-outlined /></template>
                    生成水印
                  </a-button>
                </a-card>
              </a-col>
              
              <!-- 右侧：预览和下载 -->
              <a-col :xs="24" :lg="16">
                <a-card title="🖼️ 效果预览" class="preview-card">
                  <template #extra>
                    <a-button 
                      v-if="resultImage || resultPdf" 
                      type="primary" 
                      @click="downloadResult"
                    >
                      <template #icon><download-outlined /></template>
                      {{ isPdfFile ? '下载 PDF' : '下载图片' }}
                    </a-button>
                  </template>
                  
                  <div v-if="loading" class="loading-container">
                    <a-spin size="large" tip="正在生成水印..." />
                  </div>
                  
                  <div v-else-if="resultImage && !isPdfFile" class="result-container">
                    <img :src="resultImage" alt="水印效果" class="result-img" />
                  </div>
                  
                  <div v-else-if="resultPdf && isPdfFile" class="result-container">
                    <div class="pdf-preview-container">
                      <iframe 
                        :src="resultPdf" 
                        class="pdf-preview-iframe"
                        title="PDF 预览"
                      />
                    </div>
                    <p class="pdf-preview-hint">{{ resultMessage }}</p>
                  </div>
                  
                  <a-empty v-else description="上传图片或 PDF 并设置水印后，点击生成按钮预览效果">
                    <template #image>
                      <picture-outlined style="font-size: 80px; color: #d9d9d9;" />
                    </template>
                  </a-empty>
                </a-card>
              </a-col>
            </a-row>
          </div>
        </a-layout-content>
        
        <a-layout-footer class="footer">
          ImageSeal ©{{ new Date().getFullYear() }} - 图片水印工具
        </a-layout-footer>
      </a-layout>
    </div>
  </a-config-provider>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { message } from 'ant-design-vue'
import { 
  InboxOutlined, 
  ThunderboltOutlined, 
  DownloadOutlined,
  PictureOutlined,
  FilePdfOutlined
} from '@ant-design/icons-vue'
import zhCN from 'ant-design-vue/es/locale/zh_CN'
import { watermarkApi } from '@/api/watermark'

// 文件列表
const fileList = ref([])
const selectedFile = ref(null)
const selectedFileName = ref('')
const originalImage = ref('')
const resultImage = ref('')
const resultPdf = ref('')
const resultFileName = ref('')
const resultMessage = ref('')
const loading = ref(false)
const isPdfFile = ref(false)

// 水印设置
const watermarkSettings = reactive({
  text: '',
  fontSize: 30,
  color: '#888888',
  opacity: 30,
  angle: -30,
  spacing: 100
})

// 上传前处理
const beforeUpload = (file) => {
  const isImage = file.type.startsWith('image/')
  const isPdf = file.type === 'application/pdf'
  if (!isImage && !isPdf) {
    message.error('只能上传图片或 PDF 文件！')
    return false
  }
  
  const isLt50M = file.size / 1024 / 1024 < 50
  if (!isLt50M) {
    message.error('图片大小不能超过 50MB！')
    return false
  }
  
  return false // 阻止自动上传
}

// 文件选择处理
const handleFileChange = (info) => {
  const file = info.file
  selectedFile.value = file
  selectedFileName.value = file.name
  isPdfFile.value = file.type === 'application/pdf'
  
  // 清除之前的结果
  resultImage.value = ''
  resultPdf.value = ''
  resultFileName.value = ''
  resultMessage.value = ''
  
  if (isPdfFile.value) {
    // PDF 文件不需要预览原图
    originalImage.value = ''
  } else {
    // 读取图片预览
    const reader = new FileReader()
    reader.onload = (e) => {
      originalImage.value = e.target.result
    }
    reader.readAsDataURL(file)
  }
}

// 生成水印
const generateWatermark = async () => {
  if (!selectedFile.value) {
    message.warning('请先上传文件')
    return
  }
  
  if (!watermarkSettings.text.trim()) {
    message.warning('请输入水印文字')
    return
  }
  
  loading.value = true
  
  try {
    const formData = new FormData()
    formData.append('file', selectedFile.value)
    formData.append('watermarkText', watermarkSettings.text)
    formData.append('fontSize', watermarkSettings.fontSize)
    formData.append('color', watermarkSettings.color)
    formData.append('opacity', watermarkSettings.opacity)
    formData.append('angle', watermarkSettings.angle)
    formData.append('spacing', watermarkSettings.spacing)
    
    if (isPdfFile.value) {
      // PDF 水印
      const res = await watermarkApi.addPdfWatermark(formData)
      
      if (res.success) {
        resultPdf.value = `data:${res.data.mimeType};base64,${res.data.pdfBase64}`
        resultFileName.value = res.data.fileName
        resultMessage.value = res.message
        message.success(res.message || 'PDF 水印生成成功！')
      } else {
        message.error(res.message || '生成失败')
      }
    } else {
      // 图片水印
      const res = await watermarkApi.addWatermark(formData)
      
      if (res.success) {
        resultImage.value = `data:${res.data.mimeType};base64,${res.data.imageBase64}`
        resultFileName.value = res.data.fileName
        message.success('水印生成成功！')
      } else {
        message.error(res.message || '生成失败')
      }
    }
  } catch (error) {
    console.error('生成水印失败:', error)
    message.error('生成水印失败: ' + (error.message || '未知错误'))
  } finally {
    loading.value = false
  }
}

// 下载结果
const downloadResult = () => {
  const dataUrl = isPdfFile.value ? resultPdf.value : resultImage.value
  const defaultName = isPdfFile.value ? 'watermarked.pdf' : 'watermarked.png'
  
  if (!dataUrl) {
    message.warning('没有可下载的文件')
    return
  }
  
  const link = document.createElement('a')
  link.href = dataUrl
  link.download = resultFileName.value || defaultName
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  
  message.success(isPdfFile.value ? 'PDF 已开始下载' : '图片已开始下载')
}
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial,
    'Noto Sans', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol',
    'Noto Color Emoji';
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.app-container {
  min-height: 100vh;
}

.layout {
  min-height: 100vh;
}

.header {
  display: flex;
  align-items: center;
  background: linear-gradient(135deg, #1890ff 0%, #722ed1 100%);
  padding: 0 24px;
}

.logo {
  display: flex;
  align-items: center;
}

.logo-img {
  width: 36px;
  height: 36px;
  margin-right: 10px;
}

.logo-text {
  color: white;
  font-size: 22px;
  font-weight: bold;
}

.header-subtitle {
  color: rgba(255, 255, 255, 0.85);
  margin-left: 20px;
  font-size: 14px;
}

.content {
  padding: 24px;
  background: #f0f2f5;
}

.main-container {
  max-width: 1600px;
  margin: 0 auto;
}

.upload-card,
.settings-card {
  margin-bottom: 16px;
}

.original-preview {
  margin-top: 16px;
  text-align: center;
}

.preview-img {
  max-width: 100%;
  max-height: 200px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.file-name {
  margin-top: 8px;
  color: #666;
  font-size: 12px;
  word-break: break-all;
}

.color-picker-wrapper {
  display: flex;
  align-items: center;
}

.color-picker {
  width: 40px;
  height: 32px;
  padding: 0;
  border: 1px solid #d9d9d9;
  border-radius: 4px;
  cursor: pointer;
}

.preview-card {
  min-height: 500px;
}

.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 400px;
}

.result-container {
  text-align: center;
  padding: 20px;
}

.result-img {
  max-width: 100%;
  max-height: 70vh;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.pdf-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 20px;
}

.pdf-preview-container {
  width: 100%;
  height: 70vh;
  border: 1px solid #d9d9d9;
  border-radius: 8px;
  overflow: hidden;
}

.pdf-preview-iframe {
  width: 100%;
  height: 100%;
  border: none;
}

.pdf-preview-hint {
  margin-top: 12px;
  color: #666;
  font-size: 14px;
}

.footer {
  text-align: center;
  color: #666;
  background: #f0f2f5;
}

/* 响应式调整 */
@media (max-width: 992px) {
  .preview-card {
    margin-top: 16px;
  }
}
</style>
