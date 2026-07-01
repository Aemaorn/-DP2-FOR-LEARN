export type LoginResponse = {
  userId: string,
  refreshToken: string,
  accessToken: string
}

export type UserProfile = {
  id: string;
  name: string;
  departmentCode: string;
  departmentName: string;
  departmentOrganizationLevel: string;
  email: string;
  positionCode: string;
  positionName: string;
  isActive: boolean;
  signatureImageId: string;
  role: Role[];
  employeeCode: string;
  isJorPor: boolean;
  organizationLevel: string;
  businessUnitCode: string;
}

export type Role = {
  roleCode: string;
}