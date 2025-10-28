import axios from 'axios';
import Cookies from 'js-cookie';

const API_BASE_URL = process.env.NEXT_PUBLIC_AUTH_SERVER_URL || 'https://vorld-auth.onrender.com/api';
const VORLD_APP_ID = process.env.NEXT_PUBLIC_VORLD_APP_ID || '';

// SHA-256 hash function
async function sha256(message: string): Promise<string> {
  const msgBuffer = new TextEncoder().encode(message);
  const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
  return hashHex;
}

export interface UserProfile {
  id: string;
  email: string;
  username: string;
  verified: boolean;
  authMethods: string[];
  totalConnectedAccounts: number;
  states: {
    developer: string;
    gameDeveloper: string;
  };
  wallets?: Array<{
    address: string;
    type: string;
    isDefault: boolean;
  }>;
}

export class VorldAuthService {
  private api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
      'Content-Type': 'application/json',
      'x-vorld-app-id': VORLD_APP_ID,
    },
  });

  // Email/Password Authentication
  async loginWithEmail(email: string, password: string) {
    try {
      console.log('AuthService: Starting login for', email);
      console.log('AuthService: API Base URL:', API_BASE_URL);
      console.log('AuthService: Vorld App ID:', VORLD_APP_ID);

      // Hash password with SHA-256 before sending to backend
      const hashedPassword = await sha256(password);
      console.log('AuthService: Password hashed');

      const response = await this.api.post('/auth/login', {
        email,
        password: hashedPassword
      });

      console.log('AuthService: Login response received:', response.data);

      // The API returns { success: true, data: { user, accessToken, refreshToken }, timestamp }
      const apiData = response.data.data;

      // Store tokens
      if (apiData.accessToken) {
        console.log('AuthService: Storing access token');
        Cookies.set('vorld_access_token', apiData.accessToken, { expires: 7 });
      }
      if (apiData.refreshToken) {
        console.log('AuthService: Storing refresh token');
        Cookies.set('vorld_refresh_token', apiData.refreshToken, { expires: 30 });
      }

      return {
        success: true,
        data: apiData
      };
    } catch (error: any) {
      console.error('AuthService: Login error:', error);
      console.error('AuthService: Error response:', error.response?.data);
      return {
        success: false,
        error: error.response?.data?.message || error.message || 'Login failed'
      };
    }
  }

  // Verify OTP
  async verifyOTP(email: string, otp: string) {
    try {
      const response = await this.api.post('/auth/verify-otp', {
        email,
        otp
      });

      // The API returns { success: true, data: { user, accessToken, refreshToken }, timestamp }
      const apiData = response.data.data;

      // Store tokens
      if (apiData.accessToken) {
        Cookies.set('vorld_access_token', apiData.accessToken, { expires: 7 });
      }
      if (apiData.refreshToken) {
        Cookies.set('vorld_refresh_token', apiData.refreshToken, { expires: 30 });
      }

      return {
        success: true,
        data: apiData
      };
    } catch (error: any) {
      return {
        success: false,
        error: error.response?.data?.message || 'OTP verification failed'
      };
    }
  }

  // Get User Profile
  async getProfile() {
    try {
      const token = Cookies.get('vorld_access_token');
      console.log('AuthService: Getting profile, token exists?', !!token);

      if (!token) {
        return {
          success: false,
          error: 'No access token found'
        };
      }

      const response = await this.api.get('/user/profile', {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      console.log('AuthService: Profile response:', response.data);

      // The API might return { success: true, data: {...}, timestamp }
      // Extract the actual data
      const profileData = response.data.data || response.data;

      return {
        success: true,
        data: profileData
      };
    } catch (error: any) {
      console.error('AuthService: Profile error:', error);
      return {
        success: false,
        error: error.response?.data?.message || 'Failed to get profile'
      };
    }
  }

  // Logout
  async logout() {
    try {
      const token = Cookies.get('vorld_access_token');

      if (token) {
        await this.api.post('/auth/logout', {}, {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        });
      }

      // Clear tokens
      Cookies.remove('vorld_access_token');
      Cookies.remove('vorld_refresh_token');

      return {
        success: true
      };
    } catch (error: any) {
      // Clear tokens anyway
      Cookies.remove('vorld_access_token');
      Cookies.remove('vorld_refresh_token');

      return {
        success: false,
        error: error.response?.data?.message || 'Logout failed'
      };
    }
  }

  // Get access token
  getAccessToken(): string | undefined {
    return Cookies.get('vorld_access_token');
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!Cookies.get('vorld_access_token');
  }
}
