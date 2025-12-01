import { useEffect, useState, useCallback } from "react";
import type { FormEvent } from "react";
import { createOrder, fetchOrders, askOrders } from "./api"; // 👈 já tá certo
import type { OrderResponse } from "./api";

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
  

  // Busca pedidos e liga/desliga autoRefresh conforme existir pedido "Processando"
  const loadOrders = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const data = await fetchOrders();
      setOrders(data);

      // se houver pelo menos um pedido em PROCESSING (1), liga o autoRefresh
      const hasProcessing = data.some((o) => o.status === 1);
      setAutoRefresh(hasProcessing);
    } catch (err: any) {
      setError(err.message ?? "Erro ao carregar pedidos");
    } finally {
      setLoading(false);
    }
  }, []);

  // Carrega pedidos ao abrir a tela
  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  // Enquanto existir pedido PROCESSANDO, fica atualizando a cada 2s
  useEffect(() => {
    if (!autoRefresh) return;

    const id = setInterval(() => {
      loadOrders();
    }, 2000); // 2 segundos

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

      // adiciona o pedido recém-criado no topo
      setOrders((prev) => [created, ...prev]);

      // aciona o modo de auto-refresh (worker vai mudar pra FINALIZADO em 5s)
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
    <div className="min-h-screen bg-slate-900 text-slate-100 flex flex-col items-center">
      <div className="w-full max-w-5xl px-4 py-8">
        <header className="mb-8 flex items-center justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">TMB Orders</h1>
            <p className="text-slate-400 text-sm">
              Gestão simples de pedidos para a POC da TMB.
            </p>
          </div>
          <span className="px-3 py-1 rounded-full text-xs bg-emerald-500/10 text-emerald-300 border border-emerald-500/30">
            API: http://localhost:8080
          </span>
        </header>

        <main className="grid gap-8 md:grid-cols-[minmax(0,1.2fr)_minmax(0,1.8fr)]">
          {/* Formulário */}
          <section className="bg-slate-800/70 border border-slate-700 rounded-2xl p-5 shadow-lg shadow-black/30">
            <h2 className="text-lg font-semibold mb-4">Novo pedido</h2>

            <form className="space-y-4" onSubmit={handleSubmit}>
              <div className="space-y-1">
                <label className="text-sm text-slate-300">Cliente</label>
                <input
                  className="w-full rounded-lg bg-slate-900/70 border border-slate-700 px-3 py-2 text-sm outline-none focus:border-sky-400 focus:ring-1 focus:ring-sky-400"
                  value={cliente}
                  onChange={(e) => setCliente(e.target.value)}
                  placeholder="Nome do cliente"
                />
              </div>

              <div className="space-y-1">
                <label className="text-sm text-slate-300">Produto</label>
                <input
                  className="w-full rounded-lg bg-slate-900/70 border border-slate-700 px-3 py-2 text-sm outline-none focus:border-sky-400 focus:ring-1 focus:ring-sky-400"
                  value={produto}
                  onChange={(e) => setProduto(e.target.value)}
                  placeholder="Descrição do produto"
                />
              </div>

              <div className="space-y-1">
                <label className="text-sm text-slate-300">Valor</label>
                <input
                  className="w-full rounded-lg bg-slate-900/70 border border-slate-700 px-3 py-2 text-sm outline-none focus:border-sky-400 focus:ring-1 focus:ring-sky-400"
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                  placeholder="Ex: 199.90"
                />
              </div>

              {error && (
                <p className="text-xs text-red-400 bg-red-900/30 border border-red-500/40 rounded-lg px-3 py-2">
                  {error}
                </p>
              )}

              <button
                type="submit"
                disabled={!isFormValid || submitting}
                className="w-full inline-flex items-center justify-center rounded-lg bg-sky-500 px-3 py-2 text-sm font-medium text-white shadow-lg shadow-sky-500/30 transition disabled:opacity-40 disabled:cursor-not-allowed hover:bg-sky-400"
              >
                {submitting ? "Criando..." : "Criar pedido"}
              </button>
            </form>
          </section>

          {/* Lista */}
          <section className="bg-slate-800/70 border border-slate-700 rounded-2xl p-5 shadow-lg shadow-black/30">
            <div className="mb-4 flex items-center justify-between gap-2">
              <h2 className="text-lg font-semibold">Pedidos</h2>
              <button
                onClick={loadOrders}
                disabled={loading}
                className="text-xs px-3 py-1 rounded-full border border-slate-600 bg-slate-900/60 hover:bg-slate-700/80 transition disabled:opacity-40 disabled:cursor-not-allowed"
              >
                {loading ? "Atualizando..." : "Recarregar"}
              </button>
            </div>

            {orders.length === 0 && !loading && (
              <p className="text-sm text-slate-400">
                Nenhum pedido cadastrado ainda.
              </p>
            )}

            <div className="space-y-3 max-h-[420px] overflow-y-auto pr-2">
              {orders.map((order) => (
                <article
                  key={order.id}
                  className="rounded-xl border border-slate-700 bg-slate-900/60 p-4 text-sm flex flex-col gap-2"
                >
                  <div className="flex items-center justify-between gap-2">
                    <div>
                      <p className="font-medium">{order.cliente}</p>
                      <p className="text-xs text-slate-400">
                        {order.produto}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="font-semibold">
                        {formatCurrency(order.valor)}
                      </p>
                      <span className="inline-flex mt-1 px-2 py-0.5 rounded-full text-[10px] border border-slate-600 bg-slate-800/70">
                        {STATUS_LABELS[order.status] ?? "Desconhecido"}
                      </span>
                    </div>
                  </div>

                  <div className="flex justify-between text-[11px] text-slate-400">
                    <span>Criação: {formatDate(order.dataCriacao)}</span>
                    <span>ID: {order.id.slice(0, 8)}...</span>
                  </div>
                </article>
              ))}
            </div>
          </section>

          
          <section className="bg-slate-800/70 border border-slate-700 rounded-2xl p-5 shadow-lg shadow-black/30 md:col-span-2">
            <h2 className="text-lg font-semibold mb-2">
              Pergunte sobre os pedidos (IA)
            </h2>
            <p className="text-xs text-slate-400 mb-4">
              Faça perguntas em linguagem natural, por exemplo:
              <br />
              <span className="italic">
                &quot;Quantos pedidos temos hoje?&quot;,&nbsp;
                &quot;Quantos pedidos estão pendentes?&quot;,&nbsp;
                &quot;Qual o valor total dos pedidos finalizados este mês?&quot;
              </span>
            </p>

            <form onSubmit={handleAsk} className="space-y-3">
              <textarea
                className="w-full rounded-lg bg-slate-900/70 border border-slate-700 px-3 py-2 text-sm outline-none focus:border-sky-400 focus:ring-1 focus:ring-sky-400 resize-none min-h-[70px]"
                placeholder="Digite sua pergunta sobre os pedidos..."
                value={question}
                onChange={(e) => setQuestion(e.target.value)}
              />

              <button
                type="submit"
                disabled={!question.trim() || asking}
                className="inline-flex items-center px-4 py-2 rounded-lg bg-emerald-500 text-xs font-medium text-white shadow-lg shadow-emerald-500/30 hover:bg-emerald-400 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                {asking ? "Consultando IA..." : "Perguntar"}
              </button>
            </form>

            {answer && (
              <div className="mt-4 rounded-lg border border-slate-700 bg-slate-900/70 px-3 py-3 text-sm whitespace-pre-wrap">
                {answer}
              </div>
            )}
          </section>
          
        </main>
      </div>
    </div>
  );
}

export default App;
