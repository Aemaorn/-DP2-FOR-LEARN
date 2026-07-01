import Cookies from 'universal-cookie';

const cookies = new Cookies();

const DEFAULT_EXPIRY_DAYS = 7;

const get = (key: string) => cookies.get(key);

const set = (key: string, value: string, days: number = DEFAULT_EXPIRY_DAYS) => {
  const expires = new Date();
  expires.setDate(expires.getDate() + days);

  cookies.set(key, value, {
    path: '/',
    secure: false,
    sameSite: 'strict',
    expires: expires,
  });
};

const remove = (key: string) =>
  cookies.remove(key, {
    path: '/',
    secure: false,
    sameSite: 'strict',
  });

export default {
  get,
  set,
  remove,
};
