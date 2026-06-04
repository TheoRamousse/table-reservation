export type UserRole = 'Online' | 'Staff' | 'Manager' | 'Admin';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  role: UserRole;
}
