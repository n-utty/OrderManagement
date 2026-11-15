import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiError } from './api-error';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  const url = '/api/purchase-orders';
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function capture(): () => unknown {
    let caught: unknown;
    http.get(url).subscribe({ error: (error: unknown) => (caught = error) });
    return () => caught;
  }

  function respond(status: number, body: string | object): void {
    backend.expectOne(url).flush(body, { status, statusText: 'Error' });
  }

  it('maps validation problems to camelCased field errors', () => {
    const caught = capture();
    respond(400, {
      status: 400,
      title: 'One or more fields are invalid.',
      errors: {
        CustomerEmail: ['Customer email must be a valid email address.'],
        'LineItems[0].UnitPrice': ['Unit price must be greater than zero.'],
      },
    });

    const error = caught() as ApiError;
    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.title).toBe('One or more fields are invalid.');
    expect(error.fieldErrors).toEqual({
      customerEmail: ['Customer email must be a valid email address.'],
      'lineItems[0].unitPrice': ['Unit price must be greater than zero.'],
    });
  });

  it('exposes the existing order id on a conflict', () => {
    const caught = capture();
    respond(409, {
      title: 'The order conflicts with an existing order.',
      detail: 'Reference PO-1 was already submitted with different details.',
      existingOrderId: '5f0c1a9e-6c1d-4f5e-9a51-3f3c1f1c2b7a',
    });

    const error = caught() as ApiError;
    expect(error.status).toBe(409);
    expect(error.existingOrderId).toBe('5f0c1a9e-6c1d-4f5e-9a51-3f3c1f1c2b7a');
    expect(error.message).toBe('Reference PO-1 was already submitted with different details.');
  });

  it('exposes the allowed transitions when a business rule is broken', () => {
    const caught = capture();
    respond(422, {
      title: 'The request breaks a business rule.',
      detail: 'Cannot change status from Fulfilled to Pending.',
      allowedTransitions: [],
    });
    expect((caught() as ApiError).allowedTransitions).toEqual([]);

    const again = capture();
    respond(422, {
      title: 'The request breaks a business rule.',
      allowedTransitions: ['Confirmed', 'Cancelled'],
    });
    expect((again() as ApiError).allowedTransitions).toEqual(['Confirmed', 'Cancelled']);
  });

  it('keeps the server title on a 404', () => {
    const caught = capture();
    respond(404, { title: 'Not found.', detail: 'Purchase order abc was not found.' });

    const error = caught() as ApiError;
    expect(error.status).toBe(404);
    expect(error.detail).toBe('Purchase order abc was not found.');
  });

  it('falls back to a generic message when the body is not ProblemDetails', () => {
    const caught = capture();
    respond(502, 'Bad gateway');

    const error = caught() as ApiError;
    expect(error.status).toBe(502);
    expect(error.title).toBe('Something went wrong on the server. Please try again.');
    expect(error.fieldErrors).toEqual({});
  });

  it('turns a network failure into a friendly message', () => {
    const caught = capture();
    backend.expectOne(url).error(new ProgressEvent('error'));

    const error = caught() as ApiError;
    expect(error.status).toBe(0);
    expect(error.isNetworkError).toBe(true);
    expect(error.title).toBe("Can't reach the server");
    expect(error.message).toBe('Check your connection and try again.');
  });
});
