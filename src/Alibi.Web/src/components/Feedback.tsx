export function Loading({ text }: { text: string }) {
  return <p className="hint"><span className="spinner" /> {text}</p>;
}

export function ApiError({ message }: { message: string }) {
  return (
    <div className="panel">
      <h3>Não consegui falar com a API</h3>
      <p className="hint">{message}. O SQL Server pode ainda estar iniciando. Tente de novo em alguns segundos.</p>
      <button className="btn" onClick={() => location.reload()}>Tentar de novo</button>
    </div>
  );
}
