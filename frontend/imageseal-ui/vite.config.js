import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  
  server: {
    port: 30031,
    host: '127.0.0.1',
    proxy: {
      '/api': {
        target: 'http://localhost:30032',
        changeOrigin: true
      }
    }
  },
  
  base: './',
  
  build: {
    outDir: '../../backend/ImageSeal.Api/wwwroot',
    emptyOutDir: true,
    assetsDir: 'assets',
    sourcemap: false
  }
})
