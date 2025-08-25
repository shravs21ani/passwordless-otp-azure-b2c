import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Shield, LogOut, User, Calendar, Mail, Phone } from 'lucide-react';

const DashboardPage = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-primary-50 to-primary-100">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <div className="w-8 h-8 bg-primary-100 rounded-lg flex items-center justify-center mr-3">
                <Shield className="w-5 h-5 text-primary-600" />
              </div>
              <h1 className="text-xl font-semibold text-gray-900">AccessOTP Dashboard</h1>
            </div>
            <button
              onClick={handleLogout}
              className="flex items-center px-4 py-2 text-gray-700 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <LogOut className="w-4 h-4 mr-2" />
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Welcome Section */}
        <div className="bg-white rounded-2xl shadow-sm p-8 mb-8">
          <div className="flex items-center mb-6">
            <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mr-4">
              <User className="w-8 h-8 text-primary-600" />
            </div>
            <div>
              <h2 className="text-2xl font-bold text-gray-900">Welcome back, {user?.firstName}!</h2>
              <p className="text-gray-600">You're successfully authenticated using AccessOTP</p>
            </div>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-gray-50 rounded-lg p-6">
              <div className="flex items-center mb-3">
                <Mail className="w-5 h-5 text-gray-500 mr-2" />
                <span className="text-sm font-medium text-gray-700">Email</span>
              </div>
              <p className="text-gray-900">{user?.email}</p>
            </div>
            
            {user?.phoneNumber && (
              <div className="bg-gray-50 rounded-lg p-6">
                <div className="flex items-center mb-3">
                  <Phone className="w-5 h-5 text-gray-500 mr-2" />
                  <span className="text-sm font-medium text-gray-700">Phone</span>
                </div>
                <p className="text-gray-900">{user.phoneNumber}</p>
              </div>
            )}
            
            <div className="bg-gray-50 rounded-lg p-6">
              <div className="flex items-center mb-3">
                <Calendar className="w-5 h-5 text-gray-500 mr-2" />
                <span className="text-sm font-medium text-gray-700">Last Login</span>
              </div>
              <p className="text-gray-900">
                {user?.lastLoginAt 
                  ? new Date(user.lastLoginAt).toLocaleDateString()
                  : 'First time login'
                }
              </p>
            </div>
          </div>
        </div>

        {/* Features Section */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="bg-white rounded-2xl shadow-sm p-8">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">🔐 Passwordless Security</h3>
            <p className="text-gray-600 mb-4">
              Your account is protected by secure, time-based OTP authentication. No passwords to remember or compromise.
            </p>
            <ul className="space-y-2 text-sm text-gray-600">
              <li>• 6-digit numeric OTP codes</li>
              <li>• 5-minute expiration time</li>
              <li>• Smart retry mechanism (30s → 1m → 1.5m)</li>
              <li>• Multi-delivery support (SMS & Email)</li>
            </ul>
          </div>

          <div className="bg-white rounded-2xl shadow-sm p-8">
            <h3 className="text-xl font-semibold text-gray-900 mb-4">🚀 Enterprise Ready</h3>
            <p className="text-gray-600 mb-4">
              Built with enterprise-grade security and scalability in mind, integrating with Azure AD B2C and Okta.
            </p>
            <ul className="space-y-2 text-sm text-gray-600">
              <li>• Azure AD B2C integration</li>
              <li>• Okta federation support</li>
              <li>• JWT token authentication</li>
              <li>• Cross-subscription access</li>
            </ul>
          </div>
        </div>

        {/* Stats Section */}
        <div className="bg-white rounded-2xl shadow-sm p-8 mt-8">
          <h3 className="text-xl font-semibold text-gray-900 mb-6">Authentication Statistics</h3>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            <div className="text-center">
              <div className="text-3xl font-bold text-primary-600 mb-2">100%</div>
              <div className="text-sm text-gray-600">Passwordless</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-success-600 mb-2">6</div>
              <div className="text-sm text-gray-600">OTP Digits</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-warning-600 mb-2">5m</div>
              <div className="text-sm text-gray-600">Expiry Time</div>
            </div>
            <div className="text-center">
              <div className="text-3xl font-bold text-error-600 mb-2">3</div>
              <div className="text-sm text-gray-600">Max Retries</div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default DashboardPage;

