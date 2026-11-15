import { HttpErrorResponse } from '@angular/common/http';

export type FieldErrors = Readonly<Record<string, readonly string[]>>;

/** Every failed API call surfaces as this, built from the server's ProblemDetails where present. */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail: string | null;
  readonly fieldErrors: FieldErrors;
  readonly existingOrderId: string | null;
  readonly allowedTransitions: readonly string[];

  constructor(init: {
    status: number;
    title: string;
    detail?: string | null;
    fieldErrors?: FieldErrors;
    existingOrderId?: string | null;
    allowedTransitions?: readonly string[];
  }) {
    super(init.detail ?? init.title);
    this.name = 'ApiError';
    this.status = init.status;
    this.title = init.title;
    this.detail = init.detail ?? null;
    this.fieldErrors = init.fieldErrors ?? {};
    this.existingOrderId = init.existingOrderId ?? null;
    this.allowedTransitions = init.allowedTransitions ?? [];
  }

  get isNetworkError(): boolean {
    return this.status === 0;
  }
}

interface ProblemDetails {
  title?: unknown;
  detail?: unknown;
  errors?: unknown;
  existingOrderId?: unknown;
  allowedTransitions?: unknown;
}

export function toApiError(response: HttpErrorResponse): ApiError {
  if (response.status === 0) {
    return new ApiError({
      status: 0,
      title: "Can't reach the server",
      detail: 'Check your connection and try again.',
    });
  }

  const problem: ProblemDetails = isObject(response.error) ? response.error : {};

  return new ApiError({
    status: response.status,
    title: asString(problem.title) ?? fallbackTitle(response.status),
    detail: asString(problem.detail),
    fieldErrors: toFieldErrors(problem.errors),
    existingOrderId: asString(problem.existingOrderId),
    allowedTransitions: asStringArray(problem.allowedTransitions),
  });
}

// The API keys errors by C# property path ("LineItems[0].UnitPrice"); forms use camelCase.
function toFieldErrors(errors: unknown): FieldErrors {
  if (!isObject(errors)) {
    return {};
  }
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [camelCasePath(key), asStringArray(messages)]),
  );
}

function camelCasePath(path: string): string {
  return path
    .split('.')
    .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
    .join('.');
}

function fallbackTitle(status: number): string {
  if (status === 404) return 'Not found.';
  if (status >= 500) return 'Something went wrong on the server. Please try again.';
  return 'The request failed.';
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function asString(value: unknown): string | null {
  return typeof value === 'string' && value.length > 0 ? value : null;
}

function asStringArray(value: unknown): string[] {
  return Array.isArray(value) ? value.filter((item) => typeof item === 'string') : [];
}
