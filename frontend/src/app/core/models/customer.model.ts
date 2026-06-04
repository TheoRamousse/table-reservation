export enum VipLevel {
  None = 'None',
  Regular = 'Regular',
  VIP = 'VIP',
  VVIP = 'VVIP',
}

export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  email: string | null;
  isBlacklisted: boolean;
  noShowCount: number;
  vipLevel: VipLevel;
}
