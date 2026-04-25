// src/app/core/auth/models/auth.models.ts
export interface RegisterRequest {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }
  
  export interface LoginRequest {
    email: string;
    password: string;
  }
  
  export interface UserDto {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: 'Customer' | 'Seller' | 'Admin';
  }
  
  export interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    accessTokenExpiry: string;
    user: UserDto;
  }
  
  export interface ApiError {
    message: string;
  }