import { Location } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { errorInterceptor } from '../../../../core/http/error.interceptor';
import { aPurchaseOrder } from '../../testing/purchase-order.fixture';
import { OrderDetailPage } from './order-detail-page';

describe('OrderDetailPage', () => {
  const id = 'order-1';
  const orderUrl = `/api/purchase-orders/${id}`;
  let harness: RouterTestingHarness;
  let backend: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [{ path: 'orders/:id', component: OrderDetailPage }],
          withComponentInputBinding(),
        ),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => backend.verify());

  async function openWith(order = aPurchaseOrder({ id })): Promise<HTMLElement> {
    await harness.navigateByUrl(`/orders/${id}`, OrderDetailPage);
    TestBed.tick();
    backend.expectOne(orderUrl).flush(order);
    return render();
  }

  async function render(): Promise<HTMLElement> {
    await harness.fixture.whenStable();
    return harness.routeNativeElement as HTMLElement;
  }

  function actionLabels(page: HTMLElement): string[] {
    return Array.from(page.querySelectorAll('app-order-status-actions button'), (button) =>
      button.textContent!.trim(),
    );
  }

  function click(page: HTMLElement, label: string): void {
    const button = Array.from(page.querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === label,
    );
    expect(button, `button "${label}"`).toBeDefined();
    button!.click();
  }

  it('offers only the transitions the server allows', async () => {
    const page = await openWith(
      aPurchaseOrder({ id, allowedTransitions: ['Confirmed', 'Cancelled'] }),
    );

    expect(actionLabels(page)).toEqual(['Confirm order', 'Cancel order']);
  });

  it('shows the customer account number under that label', async () => {
    const page = await openWith(aPurchaseOrder({ id, customerId: 'ACC-1042' }));

    const terms = Array.from(page.querySelectorAll('dt'), (dt) => dt.textContent?.trim());
    expect(terms).toContain('Customer account no.');
    expect(page.querySelector('dt + dd.mono')?.textContent?.trim()).toBe('ACC-1042');
  });

  it('offers fulfilment once confirmed', async () => {
    const page = await openWith(
      aPurchaseOrder({ id, status: 'Confirmed', allowedTransitions: ['Fulfilled', 'Cancelled'] }),
    );

    expect(actionLabels(page)).toEqual(['Mark as fulfilled', 'Cancel order']);
  });

  it('says so when the order is in a final status', async () => {
    const page = await openWith(
      aPurchaseOrder({ id, status: 'Fulfilled', allowedTransitions: [] }),
    );

    expect(actionLabels(page)).toEqual([]);
    expect(page.textContent).toContain('This order is in a final status');
  });

  it('updates the view from the response after a status change', async () => {
    const page = await openWith();

    click(page, 'Confirm order');
    const request = backend.expectOne(`${orderUrl}/status`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ status: 'Confirmed' });
    request.flush(
      aPurchaseOrder({ id, status: 'Confirmed', allowedTransitions: ['Fulfilled', 'Cancelled'] }),
    );
    await render();

    expect(page.querySelector('app-status-badge')?.textContent?.trim()).toBe('Confirmed');
    expect(actionLabels(page)).toEqual(['Mark as fulfilled', 'Cancel order']);
  });

  it('asks before cancelling and shows a rejected change inline', async () => {
    const page = await openWith();

    click(page, 'Cancel order');
    await render();
    backend.expectNone(`${orderUrl}/status`);
    expect(page.textContent).toContain("Cancel this order? This can't be undone.");

    click(page, 'Yes, cancel order');
    backend.expectOne(`${orderUrl}/status`).flush(
      {
        title: 'The request breaks a business rule.',
        detail:
          "Order PO-1001 is Fulfilled, which is a final status, so it can't be changed to Cancelled.",
        allowedTransitions: [],
      },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    TestBed.tick();
    backend
      .expectOne(orderUrl)
      .flush(aPurchaseOrder({ id, status: 'Fulfilled', allowedTransitions: [] }));
    await render();

    expect(page.querySelector('[role="alert"]')?.textContent).toContain('which is a final status');
    expect(page.textContent).toContain('This order is in a final status');
  });

  it('shows "Order not found" for a 404', async () => {
    await harness.navigateByUrl(`/orders/${id}`, OrderDetailPage);
    TestBed.tick();
    backend
      .expectOne(orderUrl)
      .flush({ title: 'Not found.' }, { status: 404, statusText: 'Not Found' });
    const page = await render();

    expect(page.querySelector('h2')?.textContent).toBe('Order not found');
    expect(page.querySelector('a[href="/orders"]')).not.toBeNull();
  });

  it('links back to the list with the filter and page it came from', async () => {
    await TestBed.inject(Router).navigateByUrl(`/orders/${id}`, {
      state: { listQuery: { status: 'Confirmed', page: 2 } },
    });
    TestBed.tick();
    backend.expectOne(orderUrl).flush(aPurchaseOrder({ id }));
    const page = await render();

    expect(page.querySelector('.back-link')?.getAttribute('href')).toBe(
      '/orders?status=Confirmed&page=2',
    );
  });

  it.each([
    ['created', 'Order created'],
    ['already-submitted', 'This order was already submitted – showing the existing order'],
  ])('shows the %s banner once, then forgets it', async (banner, text) => {
    await TestBed.inject(Router).navigateByUrl(`/orders/${id}`, {
      state: { banner, listQuery: { page: 2 } },
    });
    TestBed.tick();
    backend.expectOne(orderUrl).flush(aPurchaseOrder({ id }));
    const page = await render();

    expect(page.querySelector('.notice')?.textContent).toContain(text);
    // Gone from history so a refresh won't repeat it; the rest of the state is kept.
    const state = TestBed.inject(Location).getState() as Record<string, unknown>;
    expect(state['banner']).toBeUndefined();
    expect(state['listQuery']).toEqual({ page: 2 });
  });

  it('shows no banner when arriving from the list', async () => {
    const page = await openWith();

    expect(page.querySelector('.notice')).toBeNull();
  });
});
