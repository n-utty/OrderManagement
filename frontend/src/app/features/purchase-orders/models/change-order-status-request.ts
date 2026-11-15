import { OrderStatus } from './order-status';

export interface ChangeOrderStatusRequest {
  status: OrderStatus;
}
