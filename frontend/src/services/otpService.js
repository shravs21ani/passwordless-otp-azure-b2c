import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'https://localhost:7001/api';

// Create axios instance with default config
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add request interceptor to include auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Add response interceptor to handle auth errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token expired or invalid, redirect to login
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const otpService = {
  /**
   * Generate OTP for the specified user
   * @param {string} identifier - Email or phone number
   * @param {string} deliveryMethod - 'email' or 'sms'
   * @returns {Promise<Object>} OTP generation result
   */
  async generateOTP(identifier, deliveryMethod) {
    try {
      const response = await api.post('/otp/generate', {
        identifier,
        deliveryMethod: deliveryMethod.toUpperCase()
      });
      return response.data;
    } catch (error) {
      console.error('Error generating OTP:', error);
      throw error;
    }
  },

  /**
   * Validate OTP code
   * @param {string} identifier - Email or phone number
   * @param {string} otpCode - OTP code to validate
   * @returns {Promise<Object>} OTP validation result
   */
  async validateOTP(identifier, otpCode) {
    try {
      const response = await api.post('/otp/validate', {
        identifier,
        otpCode
      });
      return response.data;
    } catch (error) {
      console.error('Error validating OTP:', error);
      throw error;
    }
  },

  /**
   * Resend OTP with retry logic
   * @param {string} identifier - Email or phone number
   * @param {string} deliveryMethod - 'email' or 'sms'
   * @returns {Promise<Object>} OTP resend result
   */
  async resendOTP(identifier, deliveryMethod) {
    try {
      const response = await api.post('/otp/resend', {
        identifier,
        deliveryMethod: deliveryMethod.toUpperCase()
      });
      return response.data;
    } catch (error) {
      console.error('Error resending OTP:', error);
      throw error;
    }
  },

  /**
   * Cancel active OTP request
   * @param {string} identifier - Email or phone number
   * @returns {Promise<boolean>} Success/failure result
   */
  async cancelOTP(identifier) {
    try {
      const response = await api.post('/otp/cancel', { identifier });
      return response.data;
    } catch (error) {
      console.error('Error cancelling OTP:', error);
      throw error;
    }
  },

  /**
   * Get OTP status for a user
   * @param {string} identifier - Email or phone number
   * @returns {Promise<Object>} OTP status information
   */
  async getOTPStatus(identifier) {
    try {
      const response = await api.get(`/otp/status/${identifier}`);
      return response.data;
    } catch (error) {
      console.error('Error getting OTP status:', error);
      throw error;
    }
  },

  /**
   * Health check for OTP service
   * @returns {Promise<Object>} Service health status
   */
  async healthCheck() {
    try {
      const response = await api.get('/otp/health');
      return response.data;
    } catch (error) {
      console.error('Error checking OTP service health:', error);
      throw error;
    }
  }
};

export default otpService;

