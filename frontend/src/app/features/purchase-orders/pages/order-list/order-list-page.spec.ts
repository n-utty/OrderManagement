import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { aPage, aPurchaseOrder } from '../../testing/purchase-order.fixture';
import { OrderListPage } from './order-list-page';

@Component({ template: '' })
class Blank {}

describe('OrderListPage', () => {
  let harness: RouterTestingHarness;
  let backend: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            { path: 'orders', component: OrderListPage },
            { path: 'orders/:id', component: Blank },
          ],
          withComponentInputBinding(),
        ),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => backend.verify());

  async function open(url: string): Promise<void> {
    await harness.navigateByUrl(url, OrderListPage);
    TestBed.tick();
  }

  function expectList(params: Record<string, string>) {
    const request = backend.expectOne((req) => req.url === '/api/purchase-orders');
    for (const [key, value] of Object.entries(params)) {
      expect(request.request.params.get(key)).toBe(value);
    }
    return request;
  }

  async function render(): Promise<HTMLElement> {
    await harness.fixture.whenStable();
    return harness.routeNativeElement as HTMLElement;
  }

  it('renders a row per order with reference, total and status', async () => {
    await open('/orders');
    expectList({ page: '1', pageSize: '20' }).flush(
      aPage([
        aPurchaseOrder(),
        aPurchaseOrder({ id: 'b', externalReference: 'PO-1002', status: 'Fulfilled', total: 99 }),
      ]),
    );

    const page = await render();
    const rows = page.querySelectorAll('tbody tr');
    expect(rows).toHaveLength(2);

    const cells = Array.from(rows[0].querySelectorAll('td'), (cell) => cell.textContent?.trim());
    expect(cells[0]).toBe('PO-1001');
    expect(cells[1]).toBe('Acme Trading');
    expect(cells[2]).toBe('1');
    expect(cells[3]).toBe('ZAR 1,234.50');
    expect(cells[4]).toBe('Pending');
    expect(rows[0].querySelector('a')?.getAttribute('href')).toBe(
      '/orders/7b0f3c4e-2d1a-4f6b-9c8e-1a2b3c4d5e6f',
    );
    expect(page.textContent).toContain('Showing 1–2 of 2');
  });

  it('reads the filter and page from the query string', async () => {
    await open('/orders?status=Confirmed&page=3');

    expectList({ status: 'Confirmed', page: '3' }).flush(
      aPage([], { page: 3, totalCount: 41, totalPages: 3 }),
    );
    await render();
  });

  it('puts the chosen status in the URL and resets to the first page', async () => {
    await open('/orders?page=2');
    expectList({ page: '2' }).flush(
      aPage([aPurchaseOrder()], { page: 2, totalCount: 21, totalPages: 2 }),
    );
    const page = await render();

    const select = page.querySelector<HTMLSelectElement>('#status-filter')!;
    select.value = 'Confirmed';
    select.dispatchEvent(new Event('change'));

    // whenStable() would wait on the very request we're about to assert, so wait on the URL instead.
    const router = TestBed.inject(Router);
    await vi.waitFor(() => expect(router.url).toBe('/orders?status=Confirmed'));
    TestBed.tick();

    expectList({ status: 'Confirmed', page: '1' }).flush(aPage([]));
    await render();
  });

  it('invites the user to create the first order when there are none', async () => {
    await open('/orders');
    expectList({}).flush(aPage([]));

    const page = await render();
    expect(page.textContent).toContain('No orders yet');
    expect(page.querySelector('a[href="/orders/new"]')).not.toBeNull();
  });
});
