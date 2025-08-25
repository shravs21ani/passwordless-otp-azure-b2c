import React, { createContext, useContext, useState, useEffect } from 'react';
import { otpService } from '../services/otpService';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [otpStatus, setOtpStatus] = useState(null);

  useEffect(() => {
    // Check for existing authentication on app load
    const token = localStorage.getItem('accessToken');
    const userData = localStorage.getItem('user');
    
    if (token && userData) {
      try {
        setUser(JSON.parse(userData));
        setIsAuthenticated(true);
      } catch (error) {
        console.error('Error parsing stored user data:', error);
        localStorage.removeItem('accessToken');
        localStorage.removeItem('user');
      }
    }
    
    setIsLoading(false);
  }, []);

  const login = async (identifier, deliveryMethod) => {
    try {
      setIsLoading(true);
      const result = await otpService.generateOTP(identifier, deliveryMethod);
      
      if (result.success) {
        setOtpStatus({
          identifier,
          deliveryMethod,
          expiresAt: result.expiresAt,
          retryCount: result.retryCount,
          nextRetryAt: result.nextRetryAt
        });
        return { success: true, message: result.message };
      } else {
        return { success: false, message: result.message };
      }
    } catch (error) {
      console.error('Login error:', error);
      return { success: false, message: 'An error occurred during login' };
    } finally {
      setIsLoading(false);
    }
  };

  const validateOTP = async (otpCode) => {
    if (!otpStatus) {
      return { success: false, message: 'No OTP request found' };
    }

    try {
      setIsLoading(true);
      const result = await otpService.validateOTP(otpStatus.identifier, otpCode);
      
      if (result.success) {
        // Store authentication data
        localStorage.setItem('accessToken', result.accessToken);
        localStorage.setItem('refreshToken', result.refreshToken);
        localStorage.setItem('user', JSON.stringify(result.user));
        
        setUser(result.user);
        setIsAuthenticated(true);
        setOtpStatus(null);
        
        return { success: true, message: result.message };
      } else {
        return { success: false, message: result.message };
      }
    } catch (error) {
      console.error('OTP validation error:', error);
      return { success: false, message: 'An error occurred during OTP validation' };
    } finally {
      setIsLoading(false);
    }
  };

  const resendOTP = async () => {
    if (!otpStatus) {
      return { success: false, message: 'No OTP request found' };
    }

    try {
      setIsLoading(true);
      const result = await otpService.resendOTP(otpStatus.identifier, otpStatus.deliveryMethod);
      
      if (result.success) {
        setOtpStatus(prev => ({
          ...prev,
          expiresAt: result.expiresAt,
          retryCount: result.retryCount,
          nextRetryAt: result.nextRetryAt
        }));
        return { success: true, message: result.message };
      } else {
        return { success: false, message: result.message };
      }
    } catch (error) {
      console.error('OTP resend error:', error);
      return { success: false, message: 'An error occurred while resending OTP' };
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    setUser(null);
    setIsAuthenticated(false);
    setOtpStatus(null);
  };

  const getOTPStatus = async (identifier) => {
    try {
      const result = await otpService.getOTPStatus(identifier);
      return result;
    } catch (error) {
      console.error('Error getting OTP status:', error);
      return null;
    }
  };

  const value = {
    user,
    isAuthenticated,
    isLoading,
    otpStatus,
    login,
    validateOTP,
    resendOTP,
    logout,
    getOTPStatus
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

