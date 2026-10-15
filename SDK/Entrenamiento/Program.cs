using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AgentsPoker.Core;
using AgentsPoker.Core.Agents;

// =====================================================================================================
// LABORATORIO DE ENTRENAMIENTO (no se entrega: edítalo a tu gusto)
//
//   .\entrenar.cmd          cada prueba corre 30 s          (macOS/Linux: bash entrenar.sh)
//   .\entrenar.cmd 300      cada prueba corre 300 s: más manos = resultados más precisos
//
// Compila todos los .cs de Agents/ con las reglas de la Arena (C# 9, .NET Standard 2.1).
// Para DEPURAR: abre la carpeta del SDK en VS Code / Visual Studio, pon un breakpoint en tu agente
// (Agents/MiAgente.cs) y ejecuta este proyecto con F5.
// =====================================================================================================

// 1) TU AGENTE: cambia MiAgente por el nombre de tu clase.
Func<IPokerAgent> crearMiAgente = () => new MiAgente();

// 2) RIVALES de entrenamiento (de 1 a 5): agentes base del SDK y/o clases de Agents/ (p. ej. () => new Rival()).
//    El Jefe Final del torneo ("Archimago") es SimpleHeuristicAgent: conviene ganarle.
var rivales = new List<(string Nombre, Func<IPokerAgent> Crear)>
{
    ("Heuristic", () => new SimpleHeuristicAgent()),
    ("Heuristic2", () => new SimpleHeuristicAgent()),
    ("Rival", () => new Rival()),
    ("Station", () => new CallingStationAgent()),
    ("Raiser", () => new AlwaysRaiseAgent()),
};

Console.OutputEncoding = System.Text.Encoding.UTF8;
double segundos = args.Length > 0 ? double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture) : 30;
Console.WriteLine($"AgentsPoker.Core v{typeof(IPokerAgent).Assembly.GetName().Version.ToString(3)} | rivales: {string.Join(", ", rivales.Select(r => r.Nombre))} | {segundos} s por prueba");

// -----------------------------------------------------------------------------------------------------
// A) LARGO PLAZO: manos seguidas en modo Training (sin timeout = máxima velocidad).
//    bb/100 = ciegas grandes ganadas cada 100 manos. > 0 significa que ganas fichas a la larga.
//    El "±" es el margen de error (95 %): si es mayor que el resultado, aún no sabes si ganas. Corre más tiempo.
// -----------------------------------------------------------------------------------------------------
{
    var cfg = GameConfig.Training(seed: 1);
    var mesa = new PokerTable(cfg);
    var yo = new Cronometro(crearMiAgente());
    mesa.Seat(yo, "Yo");
    foreach (var r in rivales) mesa.Seat(r.Crear(), r.Nombre);

    long manos = 0;
    double sumaBb = 0, sumaBb2 = 0;
    int errores = 0;
    var reloj = Stopwatch.StartNew();
    while (reloj.Elapsed.TotalSeconds < segundos)
    {
        for (int s = 0; s < mesa.SeatCount; s++)
            if (mesa.GetStack(s) == 0) mesa.SetStack(s, cfg.StartingStack); // recompra: nadie queda fuera
        var res = mesa.PlayHand();
        double bb = res.Net[0] / (double)cfg.BigBlind;
        sumaBb += bb;
        sumaBb2 += bb * bb;
        manos++;
        foreach (var e in res.Errors)
            if (e.Seat == 0 && errores++ < 10) Console.WriteLine("  ERROR " + e);
    }
    double media = sumaBb / manos;
    double margen = 1.96 * Math.Sqrt(Math.Max(0, sumaBb2 / manos - media * media) / manos);
    Console.WriteLine($"\nA) Largo plazo: {manos:N0} manos ({manos / reloj.Elapsed.TotalSeconds:N0} manos/s)");
    Console.WriteLine($"   Resultado: {media * 100:+0.0;-0.0} ± {margen * 100:0.0} bb/100");
    Console.WriteLine($"   Decisión: {yo.TotalMs / Math.Max(1, yo.Decisiones):F2} ms prom, {yo.MaxMs:F1} ms máx   Errores: {errores}");
    if (yo.MaxMs > 300) Console.WriteLine("   ¡Cuidado! La Arena (Unity/Mono) es más lenta que .NET en tu PC: apunta a < 300 ms máx aquí.");
}

// -----------------------------------------------------------------------------------------------------
// B) FORMATO TORNEO: mesas de 30 manos, stack 10 000, top 3 clasifica, asientos al azar.
//    Esto es lo que cuenta en el torneo: Clasif% = % de mesas en las que quedas en el top 3.
//    Como en la Arena, cada mesa crea una instancia NUEVA de cada agente (lo aprendido con OnHandEnd se reinicia).
// -----------------------------------------------------------------------------------------------------
{
    var rng = new Random(7);
    int mesas = 0, clasifica = 0, gana = 0;
    long rankSuma = 0, stackSuma = 0;
    int clasificanPorMesa = Math.Min(3, rivales.Count);
    var reloj = Stopwatch.StartNew();
    while (reloj.Elapsed.TotalSeconds < segundos)
    {
        var asientos = new List<(string Nombre, Func<IPokerAgent> Crear)> { ("Yo", crearMiAgente) };
        asientos.AddRange(rivales);
        asientos = asientos.OrderBy(_ => rng.Next()).ToList();

        var mesa = new PokerTable(GameConfig.Training(seed: rng.Next()));
        foreach (var a in asientos) mesa.Seat(a.Crear(), a.Nombre);
        var partida = new TableMatch(mesa, handsToPlay: 30, qualifiers: clasificanPorMesa);
        partida.PlayAll();

        var yo = partida.GetStandings().First(s => s.Name == "Yo");
        mesas++;
        if (yo.Qualifies) clasifica++;
        if (yo.Rank == 1) gana++;
        rankSuma += yo.Rank;
        stackSuma += yo.Stack;
    }
    double p = clasifica / (double)mesas;
    Console.WriteLine($"\nB) Formato torneo: {mesas:N0} mesas de 30 manos, {1 + rivales.Count} jugadores, top {clasificanPorMesa} clasifica");
    Console.WriteLine($"   Clasif% {100 * p:F1} ± {100 * 1.96 * Math.Sqrt(p * (1 - p) / mesas):F1}   Gana% {100.0 * gana / mesas:F1}" +
                      $"   Puesto prom {rankSuma / (double)mesas:F2}   Stack prom {stackSuma / mesas:N0}");
}

Console.WriteLine("\nSiguiente paso, con el timeout real del torneo (1000 ms):  .\\run.cmd bench --agent MiAgente --builtin Heuristic*5");

/// <summary>Mide el tiempo de cada decisión y reenvía OnHandEnd si tu agente lo implementa.</summary>
sealed class Cronometro : IPokerAgent, IHandObserver
{
    private readonly IPokerAgent _agente;
    public double MaxMs, TotalMs;
    public long Decisiones;

    public Cronometro(IPokerAgent agente) { _agente = agente; }

    public void Initialize(string agentName) => _agente.Initialize(agentName);

    public ActionType GetAction(PlayerObservation o)
    {
        var sw = Stopwatch.StartNew();
        try { return _agente.GetAction(o); }
        finally
        {
            double ms = sw.Elapsed.TotalMilliseconds;
            TotalMs += ms;
            Decisiones++;
            if (ms > MaxMs) MaxMs = ms;
        }
    }

    public void OnHandEnd(HandSummary s)
    {
        if (_agente is IHandObserver h) h.OnHandEnd(s);
    }
}
