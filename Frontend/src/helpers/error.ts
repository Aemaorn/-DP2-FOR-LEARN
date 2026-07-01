import type { ErrorValidatorResponse } from '@/models/shared/error';

export const errorMessageHandler = (errorMessage: any): string => {
  if (typeof (errorMessage) === 'string') {
    return errorMessage;
  }

  const error = errorMessage as ErrorValidatorResponse;

  return error.errors[0]?.reason;
};