import { useEffect, useState, useCallback } from "react";
import type { FormEvent } from "react";
import { createOrder, fetchOrders, askOrders } from "./api";
import type { OrderResponse } from "./api";
import logoTmb from "./assets/tmb_logo.png";

import "./App.css";

const STATUS_LABELS: Record<number, string> = {
  0: "Pendente",
  1: "Processando",
  2: "Finalizado",
};

function formatCurrency(value: number) {
  return value.toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL",
  });
}

function formatDate(value: string) {
  return new Date(value).toLocaleString("pt-BR");
}

function App() {
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [autoRefresh, setAutoRefresh] = useState(false);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [cliente, setCliente] = useState("");
  const [produto, setProduto] = useState("");
  const [valor, setValor] = useState("");

  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState<string | null>(null);
  const [asking, setAsking] = useState(false);

  const loadOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await fetchOrders();
      setOrders(data);

      const hasProcessing = data.some((o) => o.status === 1);
      setAutoRefresh(hasProcessing);
    } catch (err: any) {
      setError(err.message ?? "Erro ao carregar pedidos");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  useEffect(() => {
    if (!autoRefresh) return;

    const id = setInterval(() => {
      loadOrders();
    }, 2000);

    return () => clearInterval(id);
  }, [autoRefresh, loadOrders]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!cliente || !produto || !valor) return;

    const valorNumber = Number(valor.replace(",", "."));
    if (isNaN(valorNumber)) {
      setError("Valor inválido");
      return;
    }

    try {
      setSubmitting(true);
      setError(null);

      const created = await createOrder({
        cliente,
        produto,
        valor: valorNumber,
      });

      setCliente("");
      setProduto("");
      setValor("");

      setOrders((prev) => [created, ...prev]);
      setAutoRefresh(true);
    } catch (err: any) {
      setError(err.message ?? "Erro ao criar pedido");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleAsk(e: FormEvent) {
    e.preventDefault();
    if (!question.trim()) return;

    try {
      setAsking(true);
      setError(null);
      setAnswer(null);

      const resp = await askOrders(question);
      setAnswer(resp.answer);
    } catch (err: any) {
      setError(err.message ?? "Erro ao perguntar sobre os pedidos");
    } finally {
      setAsking(false);
    }
  }

  const isFormValid = cliente.trim() && produto.trim() && valor.trim();

  return (
    <div className="tmb-app">
      <div className="tmb-container">
        {/* tiramos a barra de logo daqui */}

        <main className="tmb-main">
          {/* CARD DE PEDIDOS */}
          <section className="tmb-card tmb-card-orders">
            {/* HEADER COM TÍTULO + LOGO DENTRO DA CAIXA BRANCA */}
            <div className="orders-header">
              <div className="orders-header-text">
                <h2 className="tmb-card-title">TMB PEDIDOS</h2>
                <p className="tmb-card-subtitle">
                  Preencha o formulário abaixo para realizar um novo pedido!
                </p>
              </div>

              <img src={logoTmb} alt="TMB" className="orders-logo" />
            </div>

            <div className="grid gap-8 md:grid-cols-[minmax(0,1.2fr)_minmax(0,1.8fr)]">
              {/* FORMULÁRIO */}
              <div>
                <h3 className="tmb-section-title">
                  Novo pedido
                </h3>

                <form className="space-y-4" onSubmit={handleSubmit}>
                  <div className="space-y-1">
                    <label className="tmb-label">Cliente</label>
                    <input
                      className="tmb-input"
                      value={cliente}
                      onChange={(e) => setCliente(e.target.value)}
                      placeholder="Nome do cliente"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="tmb-label">Produto</label>
                    <input
                      className="tmb-input"
                      value={produto}
                      onChange={(e) => setProduto(e.target.value)}
                      placeholder="Descrição do produto"
                    />
                  </div>

                  <div className="space-y-1">
                    <label className="tmb-label">Valor</label>
                    <input
                      className="tmb-input"
                      value={valor}
                      onChange={(e) => setValor(e.target.value)}
                      placeholder="Ex: 199.90"
                    />
                  </div>

                  {error && <p className="tmb-error">{error}</p>}

                  <button
                    type="submit"
                    disabled={!isFormValid || submitting}
                    className="tmb-primary-button w-full"
                  >
                    {submitting ? "Criando..." : "Criar pedido"}
                  </button>
                </form>
              </div>

              {/* LISTA DE PEDIDOS */}
              <div>
                <div className="mb-4 flex items-center justify-between gap-2">
                  <h3 className="tmb-section-title">
                    Pedidos recentes
                  </h3>
                  
                </div>

                {orders.length === 0 && !loading && (
                  <p className="text-sm text-slate-700">
                    Nenhum pedido cadastrado ainda.
                  </p>
                )}

                <div className="space-y-3 max-h-[420px] overflow-y-auto pr-2">
                  {orders.map((order, index) => {
                    const orderNumber = orders.length - index;

                    return (
                      <article
                        key={order.id}
                        className="rounded-xl border border-slate-300 bg-slate-50 p-4 text-sm flex flex-col gap-2"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="space-y-1">
                            <p className="text-xs font-semibold text-slate-500">
                              Pedido #{orderNumber}
                            </p>
                            <p className="text-base font-semibold text-slate-900">
                              {order.produto}
                            </p>
                            <p className="text-sm text-slate-800">
                              Valor:{" "}
                              <span className="font-semibold">
                                {formatCurrency(order.valor)}
                              </span>
                            </p>
                            <p className="text-xs text-slate-600">
                              Cliente:{" "}
                              <span className="font-medium">
                                {order.cliente}
                              </span>
                            </p>
                          </div>

                          <div className="text-right space-y-1">
                            <p className="text-xs text-slate-500">
                              Criação:
                              <br />
                              <span className="font-medium">
                                {formatDate(order.dataCriacao)}
                              </span>
                            </p>

                            <span
                              className={`
                                inline-flex mt-1 px-3 py-1 rounded-full text-[11px] font-semibold border
                                ${
                                  order.status === 2
                                    ? "bg-emerald-50 border-emerald-200 text-emerald-700"
                                    : order.status === 1
                                    ? "bg-amber-50 border-amber-200 text-amber-700"
                                    : "bg-slate-50 border-slate-300 text-slate-700"
                                }
                              `}
                            >
                              {STATUS_LABELS[order.status] ?? "Desconhecido"}
                            </span>
                          </div>
                        </div>
                      </article>
                    );
                  })}
                </div>
              </div>
            </div>
          </section>

          {/* CARD DA IA */}
          <section className="tmb-card tmb-card-ia">
            <h2 className="tmb-card-title">CONSULTA IA</h2>
            <p className="tmb-card-subtitle">
              Converse com nossa IA para tirar dúvidas.
            </p>

            <form onSubmit={handleAsk} className="space-y-3">
              <textarea
                className="tmb-ia-textarea"
                placeholder="Ex: Quantos pedidos estão pendentes hoje? Qual o valor total dos pedidos finalizados este mês?"
                value={question}
                onChange={(e) => setQuestion(e.target.value)}
              />

              <button
                type="submit"
                disabled={!question.trim() || asking}
                className="tmb-primary-button px-6 py-3"
              >
                {asking ? "Consultando IA..." : "Perguntar"}
              </button>
            </form>

            {answer && <div className="tmb-ia-answer mt-4">{answer}</div>}
          </section>
        </main>
      </div>
    </div>
  );
}

export default App;
