import { Location } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { errorInterceptor } from '../../../../core/http/error.interceptor';
import { aPurchaseOrder } from '../../testing/purchase-order.fixture';
import { OrderCreatePage } from './order-create-page';

@Component({ template: '' })
class Blank {}

describe('OrderCreatePage', () => {
  const submitUrl = '/api/purchase-orders';
  let harness: RouterTestingHarness;
  let backend: HttpTestingController;
  let page: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'orders/new', component: OrderCreatePage },
          { path: 'orders/:id', component: Blank },
        ]),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/orders/new', OrderCreatePage);
    page = harness.routeNativeElement as HTMLElement;
  });

  afterEach(() => backend.verify());

  function field<T extends HTMLElement = HTMLInputElement>(selector: string): T {
    const element = page.querySelector<T>(selector);
    expect(element, selector).not.toBeNull();
    return element!;
  }

  function type(selector: string, value: string): void {
    const element = field<HTMLInputElement | HTMLSelectElement>(selector);
    element.value = value;
    element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  async function fillValidOrder(): Promise<void> {
    type('#externalReference', 'PO-2001');
    type('#currency', 'ZAR');
    type('#customerId', 'CUST-01');
    type('#customerName', 'Acme Trading');
    type('#customerEmail', 'buyer@acme.test');
    type('#line-0-sku', 'SKU-1');
    type('#line-0-name', 'Widget');
    type('#line-0-quantity', '2');
    type('#line-0-price', '10.50');
    await harness.fixture.whenStable();
  }

  function submitButton(): HTMLButtonElement {
    return field<HTMLButtonElement>('button[type="submit"]');
  }

  async function submit() {
    submitButton().click();
    await harness.fixture.whenStable();
    return backend.expectOne((request) => request.method === 'POST' && request.url === submitUrl);
  }

  function alertText(): string {
    return page.querySelector('#submit-feedback [role="alert"]')?.textContent ?? '';
  }

  it('shows field errors and sends nothing when the form is invalid', async () => {
    submitButton().click();
    await harness.fixture.whenStable();

    backend.expectNone(submitUrl);
    expect(alertText()).toContain('Some fields need attention.');
    expect(field('#externalReference-error').textContent).toContain(
      'External reference is required.',
    );
    expect(field('#line-0-price-error').textContent).toContain('Unit price is required.');
    expect(field('#externalReference').getAttribute('aria-invalid')).toBe('true');
  });

  it('shows a live estimate of line totals and the subtotal', async () => {
    await fillValidOrder();

    expect(page.textContent).toContain('ZAR 21.00');
    expect(page.textContent).toContain('Estimate – final totals are calculated by the server');
  });

  it('sends the order once, even if submitted again while in flight', async () => {
    await fillValidOrder();

    const request = await submit();
    expect(submitButton().disabled).toBe(true);
    expect(submitButton().textContent?.trim()).toBe('Submitting…');

    field<HTMLFormElement>('form').dispatchEvent(new Event('submit')); // e.g. Enter in a field
    backend.expectNone(submitUrl);

    expect(request.request.body).toEqual({
      externalReference: 'PO-2001',
      customerId: 'CUST-01',
      customerName: 'Acme Trading',
      customerEmail: 'buyer@acme.test',
      currency: 'ZAR',
      notes: null,
      lineItems: [{ sku: 'SKU-1', name: 'Widget', quantity: 2, unitPrice: 10.5 }],
    });
    request.flush(aPurchaseOrder(), { status: 201, statusText: 'Created' });
    await harness.fixture.whenStable();
  });

  it.each([
    [201, 'created'],
    [200, 'already-submitted'],
  ])('on %d opens the order with the "%s" banner', async (status, banner) => {
    await fillValidOrder();
    const order = aPurchaseOrder({ id: 'new-order-id' });

    (await submit()).flush(order, { status, statusText: 'OK' });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/orders/new-order-id');
    expect(TestBed.inject(Location).getState()).toEqual(expect.objectContaining({ banner }));
  });

  it('on 409 stays on the form and links to the existing order', async () => {
    await fillValidOrder();

    (await submit()).flush(
      {
        title: 'The order conflicts with an existing order.',
        detail: "An order with external reference 'PO-2001' already exists with different details.",
        existingOrderId: 'existing-id',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/orders/new');
    expect(alertText()).toContain('already exists with different details');
    const link = field<HTMLAnchorElement>('#submit-feedback a');
    expect(link.textContent?.trim()).toBe('Open existing order');
    expect(link.getAttribute('href')).toBe('/orders/existing-id');
    expect(submitButton().disabled).toBe(false);
  });

  it('on 400 puts the server errors on the matching fields', async () => {
    await fillValidOrder();

    (await submit()).flush(
      {
        title: 'One or more fields are invalid.',
        errors: {
          'LineItems[0].Quantity': ['Quantity must be a whole number of at least 1.'],
          CustomerEmail: ["'Customer Email' is not a valid email address."],
          LineItemsTotal: ['A rule without a field.'],
        },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await harness.fixture.whenStable();

    expect(alertText()).toContain('One or more fields are invalid.');
    expect(alertText()).toContain('A rule without a field.');
    expect(field('#line-0-quantity-error').textContent).toContain(
      'Quantity must be a whole number of at least 1.',
    );
    expect(field('#line-0-quantity').getAttribute('aria-invalid')).toBe('true');
    expect(field('#customerEmail-error').textContent).toContain('is not a valid email address');

    type('#line-0-quantity', '3');
    await harness.fixture.whenStable();
    expect(page.querySelector('#line-0-quantity-error')).toBeNull();
  });

  it('on a network error keeps what was entered and invites a retry', async () => {
    await fillValidOrder();

    (await submit()).error(new ProgressEvent('error'));
    await harness.fixture.whenStable();

    expect(alertText()).toContain("Couldn't reach the server. Your order wasn't lost – try again.");
    expect(field('#externalReference').value).toBe('PO-2001');
    expect(field('#line-0-price').value).toBe('10.50');
    expect(submitButton().disabled).toBe(false);

    (await submit()).flush(aPurchaseOrder(), { status: 201, statusText: 'Created' });
    await harness.fixture.whenStable();
  });
});
