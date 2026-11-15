import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
} from '@angular/forms';
import { FieldErrors } from '../../../core/http/api-error';
import { CreatePurchaseOrderRequest } from '../models';
import {
  ORDER_LIMITS,
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

export type LineItemForm = FormGroup<{
  sku: FormControl<string>;
  name: FormControl<string>;
  quantity: FormControl<number | null>;
  unitPrice: FormControl<number | null>;
}>;

export type OrderForm = FormGroup<{
  externalReference: FormControl<string>;
  customerId: FormControl<string>;
  customerName: FormControl<string>;
  customerEmail: FormControl<string>;
  currency: FormControl<string>;
  notes: FormControl<string>;
  lineItems: FormArray<LineItemForm>;
}>;

export function createOrderForm(fb: NonNullableFormBuilder): OrderForm {
  return fb.group({
    externalReference: fb.control('', [
      requiredText('External reference'),
      maxLength('External reference', ORDER_LIMITS.externalReference),
      referenceFormat,
    ]),
    customerId: fb.control('', [
      requiredText('Customer ID'),
      maxLength('Customer ID', ORDER_LIMITS.customerId),
    ]),
    customerName: fb.control('', [
      requiredText('Customer name'),
      maxLength('Customer name', ORDER_LIMITS.customerName),
    ]),
    customerEmail: fb.control('', [
      requiredText('Customer email'),
      maxLength('Customer email', ORDER_LIMITS.customerEmail),
      emailAddress,
    ]),
    currency: fb.control('', supportedCurrency),
    notes: fb.control('', maxLength('Notes', ORDER_LIMITS.notes)),
    lineItems: fb.array([createLineItem(fb)], [lineItemCount, uniqueSkus]),
  });
}

export function createLineItem(fb: NonNullableFormBuilder): LineItemForm {
  return fb.group({
    sku: fb.control('', [requiredText('SKU'), maxLength('SKU', ORDER_LIMITS.sku)]),
    name: fb.control('', [requiredText('Name'), maxLength('Name', ORDER_LIMITS.lineItemName)]),
    quantity: fb.control<number | null>(1, quantity),
    unitPrice: fb.control<number | null>(null, unitPrice),
  });
}

export function toRequest(value: ReturnType<OrderForm['getRawValue']>): CreatePurchaseOrderRequest {
  return {
    externalReference: value.externalReference.trim(),
    customerId: value.customerId.trim(),
    customerName: value.customerName.trim(),
    customerEmail: value.customerEmail.trim(),
    currency: value.currency,
    notes: value.notes.trim() || null,
    lineItems: value.lineItems.map((item) => ({
      sku: item.sku.trim(),
      name: item.name.trim(),
      quantity: item.quantity ?? 0,
      unitPrice: item.unitPrice ?? 0,
    })),
  };
}

/** The first message on a control, given validators that return their message as the error value. */
export function errorMessage(control: AbstractControl): string | null {
  const errors = control.errors;
  if (!errors) return null;
  const first = Object.values(errors).find((value) => typeof value === 'string');
  return typeof first === 'string' ? first : null;
}

/**
 * Puts the server's field errors on the matching controls ("lineItems[0].quantity" included).
 * Returns the messages that have no matching control, so the caller can show them elsewhere.
 * A server error clears itself the next time the control's value changes and validators re-run.
 */
export function applyServerErrors(form: OrderForm, fieldErrors: FieldErrors): string[] {
  const unplaced: string[] = [];
  for (const [path, messages] of Object.entries(fieldErrors)) {
    const message = messages[0];
    if (!message) continue;
    const control = path ? form.get(toControlPath(path)) : null;
    if (control) {
      // First, so it is the message shown even if a client rule also fails.
      control.setErrors({ server: message, ...control.errors });
      control.markAsTouched();
    } else {
      unplaced.push(message);
    }
  }
  return unplaced;
}

// "lineItems[0].quantity" → ['lineItems', 0, 'quantity']
function toControlPath(path: string): (string | number)[] {
  return path
    .split(/[.[\]]/)
    .filter(Boolean)
    .map((segment) => (/^\d+$/.test(segment) ? Number(segment) : segment));
}
