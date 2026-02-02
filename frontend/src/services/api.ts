import axios, { type AxiosInstance, type AxiosError } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5001';

// Get tenant ID from localStorage
const getTenantId = (): string | null => {
  return localStorage.getItem('tenantId');
};

// Create axios instance with default config
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add X-Tenant-ID header
apiClient.interceptors.request.use(
  (config) => {
    const tenantId = getTenantId();
    if (tenantId) {
      config.headers['X-Tenant-ID'] = tenantId;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // Log error for debugging
    console.error('API Error:', {
      url: error.config?.url,
      method: error.config?.method,
      status: error.response?.status,
      statusText: error.response?.statusText,
      data: error.response?.data,
      message: error.message,
    });

    if (error.response?.status === 400 && error.response?.data) {
      const data = error.response.data as any;
      if (data.message?.includes('X-Tenant-ID') || data.error?.includes('tenant')) {
        // Tenant ID missing - redirect to tenant selection
        localStorage.removeItem('tenantId');
        window.location.href = '/tenant-select';
      }
    }
    
    // Handle network errors (no response)
    if (!error.response) {
      console.error('Network Error - Backend may not be running or CORS issue');
    }
    
    return Promise.reject(error);
  }
);

export default apiClient;
