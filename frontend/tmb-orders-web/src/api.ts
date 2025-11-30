export interface OrderStatusHistoryResponse {
  status: number;
  changedAt: string;
}

export interface OrderResponse {
  id: string;
  cliente: string;
  produto: string;
  valor: number;
  status: number;
  dataCriacao: string;
  statusHistory: OrderStatusHistoryResponse[];
}

export interface CreateOrderRequest {
  cliente: string;
  produto: string;
  valor: number;
}

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

export async function fetchOrders(): Promise<OrderResponse[]> {
  const res = await fetch(`${API_BASE_URL}/api/orders`);
  if (!res.ok) {
    throw new Error("Erro ao carregar pedidos");
  }
  return res.json();
}

export async function createOrder(
  payload: CreateOrderRequest
): Promise<OrderResponse> {
  const res = await fetch(`${API_BASE_URL}/api/orders`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || "Erro ao criar pedido");
  }

  return res.json();
}
