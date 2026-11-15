import { Location } from '@angular/common';
import { Params, Router } from '@angular/router';

export type OrderBanner = 'created' | 'already-submitted';

/** What pages hand to the order detail page through router navigation state. */
export interface OrderNavigationState {
  /** The list's filter and page, so "Back to orders" returns to them. */
  listQuery?: Params;
  /** A one-off message about how the user got here, e.g. after submitting. */
  banner?: OrderBanner;
}

// During a navigation the state is on the router; after a refresh, browser history still holds it.
export function readOrderNavigationState(router: Router, location: Location): OrderNavigationState {
  const state = router.currentNavigation()?.extras.state ?? location.getState();
  return (state ?? {}) as OrderNavigationState;
}
