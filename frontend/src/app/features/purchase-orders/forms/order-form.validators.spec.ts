import { FormArray, FormControl, FormGroup } from '@angular/forms';
import {
  emailAddress,
  lineItemCount,
  maxLength,
  quantity,
  referenceFormat,
  requiredText,
  supportedCurrency,
  uniqueSkus,
  unitPrice,
} from './order-form.validators';

const run = (validator: (c: FormControl) => unknown, value: unknown) =>
  validator(new FormControl(value));

function lines(...skus: string[]): FormArray {
  return new FormArray(skus.map((sku) => new FormGroup({ sku: new FormControl(sku) })));
}

describe('order form validators', () => {
  describe('requiredText', () => {
    const required = requiredText('Customer name');

    it('rejects empty and whitespace-only values, as the server does', () => {
      expect(run(required, '')).toEqual({ required: 'Customer name is required.' });
      expect(run(required, '   ')).toEqual({ required: 'Customer name is required.' });
    });

    it('accepts any other text', () => {
      expect(run(required, 'Acme')).toBeNull();
    });
  });

  it('maxLength counts the trimmed value', () => {
    const max = maxLength('SKU', 3);
    expect(run(max, ' ABC ')).toBeNull();
    expect(run(max, 'ABCD')).toEqual({ maxlength: 'SKU can be at most 3 characters.' });
  });

  describe('referenceFormat', () => {
    it.each(['PO-1001', 'po_1/2.3#4', '  PO-1  '])('accepts %j', (value) => {
      expect(run(referenceFormat, value)).toBeNull();
    });

    it.each(['PO 1001', 'PO@1', 'PO,1'])('rejects %j', (value) => {
      expect(run(referenceFormat, value)).toHaveProperty('referenceFormat');
    });

    it('leaves empty values to the required check', () => {
      expect(run(referenceFormat, '')).toBeNull();
    });
  });

  it('emailAddress rejects malformed addresses', () => {
    expect(run(emailAddress, 'buyer@acme.test')).toBeNull();
    expect(run(emailAddress, 'buyer.acme.test')).toEqual({ email: 'Enter a valid email address.' });
    expect(run(emailAddress, '')).toBeNull();
  });

  it('supportedCurrency only allows the server-supported codes', () => {
    expect(run(supportedCurrency, 'ZAR')).toBeNull();
    expect(run(supportedCurrency, 'JPY')).toHaveProperty('currency');
    expect(run(supportedCurrency, '')).toHaveProperty('currency');
  });

  describe('quantity', () => {
    it.each([1, 12, 2_147_483_647])('accepts %d', (value) => {
      expect(run(quantity, value)).toBeNull();
    });

    it('requires a value', () => {
      expect(run(quantity, null)).toEqual({ required: 'Quantity is required.' });
    });

    it.each([0, -1, 1.5])('rejects %d as not a whole number of at least 1', (value) => {
      expect(run(quantity, value)).toEqual({
        quantity: 'Quantity must be a whole number of at least 1.',
      });
    });

    it("rejects values beyond the server's 32-bit range", () => {
      expect(run(quantity, 2_147_483_648)).toEqual({ quantity: 'Quantity is too large.' });
    });
  });

  describe('unitPrice', () => {
    it.each([0, 1, 19.9, 19.99, 1000000])('accepts %d', (value) => {
      expect(run(unitPrice, value)).toBeNull();
    });

    it('requires a value', () => {
      expect(run(unitPrice, null)).toEqual({ required: 'Unit price is required.' });
    });

    it('rejects negative prices', () => {
      expect(run(unitPrice, -0.01)).toEqual({ unitPrice: "Unit price can't be negative." });
    });

    it('rejects prices above the maximum', () => {
      expect(run(unitPrice, 1000000.01)).toEqual({
        unitPrice: 'Unit price can be at most 1,000,000.00.',
      });
    });

    it.each([19.999, 1.005, 0.001])('rejects %d for having more than 2 decimals', (value) => {
      expect(run(unitPrice, value)).toEqual({
        unitPrice: 'Unit price can have at most 2 decimal places.',
      });
    });
  });

  describe('uniqueSkus', () => {
    it('accepts distinct SKUs and ignores blank ones', () => {
      expect(uniqueSkus(lines('A-1', 'B-2', '', ''))).toBeNull();
    });

    it('reports SKUs that repeat, ignoring case and surrounding spaces', () => {
      expect(uniqueSkus(lines('a-1', 'B-2', ' A-1 ', 'b-2', 'C-3'))).toEqual({
        duplicateSkus: ['A-1', 'B-2'],
      });
    });
  });

  it('lineItemCount requires between 1 and 100 lines', () => {
    expect(lineItemCount(lines('A'))).toBeNull();
    expect(lineItemCount(lines())).toHaveProperty('lineItemCount');
    expect(lineItemCount(lines(...Array.from({ length: 101 }, (_, i) => `S${i}`)))).toHaveProperty(
      'lineItemCount',
    );
  });
});
