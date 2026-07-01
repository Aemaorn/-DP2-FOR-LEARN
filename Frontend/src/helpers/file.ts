const getFileUrl = (id: string): string => {
  const apiPath = import.meta.env.VITE_APP_API_URL as string;
  const lastCharacter = apiPath.split('')[apiPath.length - 1];
  const isSlash = lastCharacter === '/';

  const pathImage = `${apiPath}${isSlash ? '' : '/'}api/files/${id}`;

  return pathImage;
};

const isFile = (data: unknown): data is File => data instanceof File;

const isFileList = (data: unknown): data is File[] => Array.isArray(data);

const isFileSizeValid = (file: File, maxSizeMB = 10): boolean => {
  const maxSizeBytes = maxSizeMB * 1024 * 1024;
  return file.size <= maxSizeBytes;
};

const isFileTypeValid = (file: File, acceptedTypes: string[]): boolean => {
  if (!acceptedTypes || acceptedTypes.length === 0) return true;

  return acceptedTypes.some((type) => {
    if (type.endsWith('/*')) {
      const prefix = type.slice(0, -2);
      return file.type.startsWith(prefix);
    }
    return file.type === type;
  });
};

const getFileExtension = (mimeType: string): string => {
  const mimeMap: Record<string, string> = {
    'application/msword': '.doc',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document': '.docx',
    'application/vnd.ms-excel': '.xls',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': '.xlsx',
    'text/csv': '.csv',
    'application/pdf': '.pdf',
    'image/jpeg': '.jpg',
    'image/png': '.png',
    'image/gif': '.gif',
    'image/webp': '.webp',
    'image/svg+xml': '.svg',
  };

  return mimeMap[mimeType] ?? (mimeType.startsWith('image/') ? '.' + mimeType.split('/')[1] : '');
};

const getFileExtensions = (mimeTypes: string[]): string =>
  mimeTypes
    .map((mimeType) => {
      const mimeMap: Record<string, string> = {
         'application/msword': '.doc',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document': '.docx',
        'application/vnd.ms-excel': '.xls',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': '.xlsx',
        'text/csv': '.csv',
        'application/pdf': '.pdf',
        'application/vnd.oasis.opendocument.text': '.odt',
        'image/jpeg': '.jpg',
        'image/png': '.png',
        'image/gif': '.gif',
        'image/webp': '.webp',
        'image/svg+xml': '.svg'
      };

      return (
        mimeMap[mimeType] ??
        (mimeType.startsWith('image/') && mimeType !== 'image/*'
          ? '.' + mimeType.split('/')[1]
          : '')
      );
    })
    .filter((ext) => ext !== '') // remove unknowns or image/*
    .join(',');

const FileHelper = {
  getFileUrl,
  isFile,
  isFileList,
  isFileSizeValid,
  isFileTypeValid,
  getFileExtension,
  getFileExtensions,
};

export default FileHelper;
