import { TestBed } from '@angular/core/testing';
import { ORDER_STATUSES } from '../../../features/purchase-orders/models';
import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  it.each(ORDER_STATUSES)('shows %s as text and exposes it for styling', (status) => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', status);
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.textContent?.trim()).toBe(status);
    expect(host.getAttribute('data-status')).toBe(status);
  });

  it('updates when the status changes', () => {
    const fixture = TestBed.createComponent(StatusBadge);
    fixture.componentRef.setInput('status', 'Pending');
    fixture.detectChanges();

    fixture.componentRef.setInput('status', 'Confirmed');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    expect(host.textContent?.trim()).toBe('Confirmed');
    expect(host.getAttribute('data-status')).toBe('Confirmed');
  });
});
