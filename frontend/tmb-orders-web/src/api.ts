const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:8080";

export type OrderStatus = 0 | 1 | 2;

export interface OrderResponse {
  id: string;
  cliente: string;
  produto: string;
  valor: number;
  status: OrderStatus;
  dataCriacao: string;
}

export interface CreateOrderRequest {
  cliente: string;
  produto: string;
  valor: number;
}

export async function fetchOrders(): Promise<OrderResponse[]> {
  const res = await fetch(`${API_URL}/api/orders`);
  if (!res.ok) {
    throw new Error("Erro ao buscar pedidos");
  }
  return res.json();
}

export async function createOrder(
  payload: CreateOrderRequest
): Promise<OrderResponse> {
  const res = await fetch(`${API_URL}/api/orders`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    throw new Error("Erro ao criar pedido");
  }

  return res.json();
}


export async function askOrders(question: string): Promise<{ answer: string }> {
  const res = await fetch(`${API_URL}/api/askorders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ question }),
  });

  if (!res.ok) {
    throw new Error("Erro ao consultar IA");
  }

  const data = await res.json();

  const answer = (data.answer ?? data.Answer ?? "").toString();

  return { answer };
}
