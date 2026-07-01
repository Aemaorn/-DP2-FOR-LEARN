import { jwtDecode } from "jwt-decode";

export const isNullorEmpty = (value?: string | number) => {
  return (value === null || value === '' || value === undefined);
};

interface JwtPayload {
  exp: number,
  sub: string,
};

export function isTokenExpired(token: string): boolean {
  if (!token) {
    return false;
  }

  try {
    const deCodetoken = jwtDecode<JwtPayload>(token);
    const currentTime = Math.floor(Date.now() / 1000);

    return deCodetoken.exp < currentTime;
  } catch (error) {
    console.error("Error decoding token:", error)
    return true;
  }
};

export function decodeUserId(token: string): string | void {
  if (!token) {
    return;
  }

  try {
    const deCodetoken = jwtDecode<JwtPayload>(token);

    return deCodetoken.sub;
  }
  catch (error) {
    console.error("Error decoding token:", error);
    return;
  }
}