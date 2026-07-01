import http from '@/configs/axios';
import { HttpStatusCode } from 'axios';

const apiUrl = import.meta.env.VITE_APP_API_URL;

const getFileUrl = async (id: string) => {
  const lastCharacter = apiUrl.split('')[apiUrl.length - 1];
  const isSlash = lastCharacter === '/';
  const pathImage = `${apiUrl}${isSlash ? '' : '/'}api/files/${id}`;

  return pathImage;
};

const downloadFile = async (id: string, fileName?: string) => {
  const { data, status } = await http.get(`/api/files/${id}`, {
    responseType: 'blob',
  });

  if (status === HttpStatusCode.Ok) {
    const blob = new Blob([data], { type: data.type });
    const url = window.URL.createObjectURL(blob);

    window.open(url, '_blank');
    setTimeout(() => window.URL.revokeObjectURL(url), 1000);
  }
};

const uploadFile = async (file: File) => {
  const formData = new FormData();

  formData.append('File', file);

  return http.post('/api/files', formData, { headers: { isDisabledLoad: true } });
};

// Document template upload (ST007 / OnlyOffice). Backed by a separate endpoint that
// only accepts .odt — keep the generic /api/files whitelist tight.
const uploadTemplateFile = async (file: File) => {
  const formData = new FormData();

  formData.append('File', file);

  return http.post('/api/files/template', formData, { headers: { isDisabledLoad: true } });
};

const ST003Service = {
  getFileUrl,
  downloadFile,
  uploadFile,
  uploadTemplateFile,
};

export default ST003Service;
