import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { toast } from 'react-hot-toast';
import { Shield, Mail, Phone, ArrowRight, RefreshCw, Clock } from 'lucide-react';
import OTPInput from '../components/OTPInput';

const LoginPage = () => {
  const navigate = useNavigate();
  const { login, validateOTP, resendOTP, otpStatus, isLoading } = useAuth();
  
  const [identifier, setIdentifier] = useState('');
  const [deliveryMethod, setDeliveryMethod] = useState('email');
  const [otpCode, setOtpCode] = useState('');
  const [step, setStep] = useState('login'); // 'login', 'otp', 'success'
  const [countdown, setCountdown] = useState(0);
  const [canResend, setCanResend] = useState(false);

  useEffect(() => {
    if (otpStatus && step === 'otp') {
      startCountdown();
    }
  }, [otpStatus, step]);

  useEffect(() => {
    if (countdown > 0) {
      const timer = setTimeout(() => setCountdown(countdown - 1), 1000);
      return () => clearTimeout(timer);
    } else if (otpStatus?.nextRetryAt) {
      const nextRetry = new Date(otpStatus.nextRetryAt);
      const now = new Date();
      const diff = Math.max(0, Math.ceil((nextRetry - now) / 1000));
      if (diff > 0) {
        setCountdown(diff);
      } else {
        setCanResend(true);
      }
    } else {
      setCanResend(true);
    }
  }, [countdown, otpStatus]);

  const startCountdown = () => {
    if (otpStatus?.expiresAt) {
      const expiresAt = new Date(otpStatus.expiresAt);
      const now = new Date();
      const diff = Math.max(0, Math.ceil((expiresAt - now) / 1000));
      setCountdown(diff);
    }
  };

  const handleLogin = async (e) => {
    e.preventDefault();
    
    if (!identifier.trim()) {
      toast.error('Please enter your email or phone number');
      return;
    }

    const result = await login(identifier, deliveryMethod);
    
    if (result.success) {
      toast.success(result.message);
      setStep('otp');
      startCountdown();
    } else {
      toast.error(result.message);
    }
  };

  const handleOTPValidation = async () => {
    if (!otpCode.trim() || otpCode.length < 4) {
      toast.error('Please enter a valid OTP code');
      return;
    }

    const result = await validateOTP(otpCode);
    
    if (result.success) {
      toast.success('Login successful!');
      setStep('success');
      setTimeout(() => navigate('/dashboard'), 1500);
    } else {
      toast.error(result.message);
      setOtpCode('');
    }
  };

  const handleResendOTP = async () => {
    if (!canResend) return;

    const result = await resendOTP();
    
    if (result.success) {
      toast.success(result.message);
      setOtpCode('');
      setCanResend(false);
      startCountdown();
    } else {
      toast.error(result.message);
    }
  };

  const handleBackToLogin = () => {
    setStep('login');
    setOtpCode('');
    setCountdown(0);
    setCanResend(false);
  };

  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  if (step === 'success') {
    return (
      <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-xl p-8 max-w-md w-full text-center animate-fade-in">
          <div className="w-16 h-16 bg-success-100 rounded-full flex items-center justify-center mx-auto mb-6">
            <Shield className="w-8 h-8 text-success-600" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-4">Welcome Back!</h2>
          <p className="text-gray-600 mb-6">You have successfully logged in using AccessOTP.</p>
          <div className="w-8 h-8 border-4 border-success-200 border-t-success-600 rounded-full animate-spin mx-auto"></div>
          <p className="text-sm text-gray-500 mt-4">Redirecting to dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl p-8 max-w-md w-full">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Shield className="w-8 h-8 text-primary-600" />
          </div>
          <h1 className="text-3xl font-bold text-gray-900 mb-2">AccessOTP</h1>
          <p className="text-gray-600">Secure, passwordless authentication</p>
        </div>

        {step === 'login' ? (
          /* Login Form */
          <form onSubmit={handleLogin} className="space-y-6">
            <div>
              <label htmlFor="identifier" className="block text-sm font-medium text-gray-700 mb-2">
                Email or Phone Number
              </label>
              <input
                type="text"
                id="identifier"
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-colors"
                placeholder="Enter your email or phone"
                disabled={isLoading}
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Delivery Method
              </label>
              <div className="grid grid-cols-2 gap-3">
                <button
                  type="button"
                  onClick={() => setDeliveryMethod('email')}
                  className={`flex items-center justify-center px-4 py-3 rounded-lg border transition-colors ${
                    deliveryMethod === 'email'
                      ? 'border-primary-500 bg-primary-50 text-primary-700'
                      : 'border-gray-300 hover:border-gray-400'
                  }`}
                >
                  <Mail className="w-5 h-5 mr-2" />
                  Email
                </button>
                <button
                  type="button"
                  onClick={() => setDeliveryMethod('sms')}
                  className={`flex items-center justify-center px-4 py-3 rounded-lg border transition-colors ${
                    deliveryMethod === 'sms'
                      ? 'border-primary-500 bg-primary-50 text-primary-700'
                      : 'border-gray-300 hover:border-gray-400'
                  }`}
                >
                  <Phone className="w-5 h-5 mr-2" />
                  SMS
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={isLoading || !identifier.trim()}
              className="w-full bg-primary-600 text-white py-3 px-4 rounded-lg hover:bg-primary-700 focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center"
            >
              {isLoading ? (
                <RefreshCw className="w-5 h-5 animate-spin mr-2" />
              ) : (
                <ArrowRight className="w-5 h-5 mr-2" />
              )}
              Send OTP
            </button>
          </form>
        ) : (
          /* OTP Validation Form */
          <div className="space-y-6">
            <div className="text-center">
              <h2 className="text-xl font-semibold text-gray-900 mb-2">Enter OTP</h2>
              <p className="text-gray-600">
                We've sent a {deliveryMethod === 'email' ? '6-digit code' : '6-digit code'} to{' '}
                <span className="font-medium">{identifier}</span>
              </p>
            </div>

            <OTPInput
              value={otpCode}
              onChange={setOtpCode}
              length={6}
              disabled={isLoading}
            />

            <div className="text-center space-y-4">
              <button
                onClick={handleOTPValidation}
                disabled={isLoading || otpCode.length < 6}
                className="w-full bg-primary-600 text-white py-3 px-4 rounded-lg hover:bg-primary-700 focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isLoading ? (
                  <RefreshCw className="w-5 h-5 animate-spin mx-auto" />
                ) : (
                  'Verify OTP'
                )}
              </button>

              <div className="flex items-center justify-between text-sm">
                <button
                  type="button"
                  onClick={handleBackToLogin}
                  className="text-primary-600 hover:text-primary-700 font-medium"
                >
                  ← Back to login
                </button>
                
                <button
                  type="button"
                  onClick={handleResendOTP}
                  disabled={!canResend || isLoading}
                  className="text-primary-600 hover:text-primary-700 font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
                >
                  <RefreshCw className="w-4 h-4 mr-1" />
                  Resend
                </button>
              </div>

              {countdown > 0 && (
                <div className="flex items-center justify-center text-sm text-gray-500">
                  <Clock className="w-4 h-4 mr-1" />
                  OTP expires in {formatTime(countdown)}
                </div>
              )}

              {otpStatus?.retryCount > 0 && (
                <div className="text-sm text-gray-500">
                  Retry {otpStatus.retryCount}/3
                </div>
              )}
            </div>
          </div>
        )}

        {/* Footer */}
        <div className="mt-8 text-center text-sm text-gray-500">
          <p>Secure authentication powered by Azure B2C & Okta</p>
          <p className="mt-1">© 2024 AccessOTP. All rights reserved.</p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;

