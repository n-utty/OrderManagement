import {
  AbstractControl,
  FormArray,
  FormControl,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';

// Mirrors CreatePurchaseOrderRequestValidator and LineItemRequestValidator on the server.
// Each validator returns its message as the error value, so templates just show it.
export const ORDER_LIMITS = {
  externalReference: 50,
  customerId: 50,
  customerName: 200,
  customerEmail: 254,
  notes: 1000,
  sku: 64,
  lineItemName: 200,
  maxLineItems: 100,
} as const;

export const CURRENCIES = ['ZAR', 'USD', 'EUR', 'GBP'] as const;

const REFERENCE_PATTERN = /^[A-Za-z0-9_/.#-]+$/;
const MAX_QUANTITY = 2_147_483_647; // The server's quantity is a 32-bit int.

export function requiredText(label: string): ValidatorFn {
  return (control) => (text(control).trim() === '' ? { required: `${label} is required.` } : null);
}

export function maxLength(label: string, max: number): ValidatorFn {
  return (control) =>
    text(control).trim().length > max
      ? { maxlength: `${label} can be at most ${max} characters.` }
      : null;
}

export const referenceFormat: ValidatorFn = (control) => {
  const value = text(control).trim();
  return value && !REFERENCE_PATTERN.test(value)
    ? {
        referenceFormat:
          'External reference may only contain letters, digits and the characters - _ / . #',
      }
    : null;
};

export const emailAddress: ValidatorFn = (control) => {
  const value = text(control).trim();
  return value && Validators.email(new FormControl(value))
    ? { email: 'Enter a valid email address.' }
    : null;
};

export const supportedCurrency: ValidatorFn = (control) =>
  (CURRENCIES as readonly string[]).includes(text(control))
    ? null
    : { currency: `Choose a currency: ${CURRENCIES.join(', ')}.` };

export const quantity: ValidatorFn = (control) => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: 'Quantity is required.' };
  if (typeof value !== 'number' || !Number.isInteger(value) || value < 1) {
    return { quantity: 'Quantity must be a whole number of at least 1.' };
  }
  return value > MAX_QUANTITY ? { quantity: 'Quantity is too large.' } : null;
};

export const unitPrice: ValidatorFn = (control) => {
  const value: unknown = control.value;
  if (value === null || value === '') return { required: 'Unit price is required.' };
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    return { unitPrice: 'Enter a price, for example 12.50.' };
  }
  if (value < 0) return { unitPrice: "Unit price can't be negative." };
  // toFixed rounds to cents; anything it changes had more than 2 decimals.
  return Number(value.toFixed(2)) === value
    ? null
    : { unitPrice: 'Unit price can have at most 2 decimal places.' };
};

export const lineItemCount: ValidatorFn = (control) => {
  const count = (control as FormArray).length;
  return count >= 1 && count <= ORDER_LIMITS.maxLineItems
    ? null
    : {
        lineItemCount: `An order must have between 1 and ${ORDER_LIMITS.maxLineItems} line items.`,
      };
};

/** Error value: the duplicated SKUs, normalised (trimmed, upper case) as the server compares them. */
export const uniqueSkus: ValidatorFn = (control): ValidationErrors | null => {
  const seen = new Set<string>();
  const duplicates = new Set<string>();
  for (const item of (control as FormArray).controls) {
    const sku = normaliseSku(item.get('sku')?.value);
    if (!sku) continue;
    if (seen.has(sku)) duplicates.add(sku);
    seen.add(sku);
  }
  return duplicates.size ? { duplicateSkus: [...duplicates] } : null;
};

export function normaliseSku(value: unknown): string {
  return typeof value === 'string' ? value.trim().toUpperCase() : '';
}

function text(control: AbstractControl): string {
  return typeof control.value === 'string' ? control.value : '';
}
