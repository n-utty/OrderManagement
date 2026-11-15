import { TestBed } from '@angular/core/testing';
import { NonNullableFormBuilder } from '@angular/forms';
import {
  OrderForm,
  applyServerErrors,
  createOrderForm,
  errorMessage,
  toRequest,
} from './order-form';

describe('order form', () => {
  let form: OrderForm;

  beforeEach(() => {
    form = createOrderForm(TestBed.inject(NonNullableFormBuilder));
  });

  it('builds a trimmed request with empty notes sent as null', () => {
    form.patchValue({
      externalReference: ' po-1 ',
      customerId: ' C-1 ',
      customerName: ' Acme ',
      customerEmail: ' buyer@acme.test ',
      currency: 'ZAR',
      notes: '   ',
      lineItems: [{ sku: ' S-1 ', name: ' Widget ', quantity: 2, unitPrice: 10.5 }],
    });

    expect(toRequest(form.getRawValue())).toEqual({
      externalReference: 'po-1',
      customerId: 'C-1',
      customerName: 'Acme',
      customerEmail: 'buyer@acme.test',
      currency: 'ZAR',
      notes: null,
      lineItems: [{ sku: 'S-1', name: 'Widget', quantity: 2, unitPrice: 10.5 }],
    });
  });

  it('places server field errors on matching controls, including line items', () => {
    const unplaced = applyServerErrors(form, {
      customerEmail: ['Customer email is not a valid email address.'],
      'lineItems[0].quantity': ['Quantity must be a whole number of at least 1.'],
      somethingElse: ['A rule the form has no field for.'],
    });

    expect(errorMessage(form.controls.customerEmail)).toBe(
      'Customer email is not a valid email address.',
    );
    const quantity = form.controls.lineItems.at(0).controls.quantity;
    expect(errorMessage(quantity)).toBe('Quantity must be a whole number of at least 1.');
    expect(quantity.touched).toBe(true);
    expect(unplaced).toEqual(['A rule the form has no field for.']);
  });

  it('clears a server error once the value changes', () => {
    applyServerErrors(form, { customerName: ['Taken.'] });

    form.controls.customerName.setValue('Acme');

    expect(form.controls.customerName.errors).toBeNull();
  });
});
