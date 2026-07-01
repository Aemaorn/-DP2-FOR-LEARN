export type ErrorValidatorResponse = {
  type: string;
  title: string;
  status: number;
  instance: string;
  traceId: string;
  errors: ErrorDetail[];
}

export type ErrorDetail = {
  name: string;
  reason: string;
}