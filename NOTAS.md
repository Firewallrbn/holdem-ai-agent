# Proyecto Final — Torneo de Poker (ElementalPoker)

Proyecto para finalizar la materia. Agente autónomo de **Limit Texas Hold'em, 6-max**, en **C#**.

## Inscripción del torneo (OBLIGATORIO)
- **Fecha límite de registro: lunes 26 de octubre de 2026, 23:59.**
- Formulario: https://forms.gle/2smLqbx2b69zWn7W9
- Un líder llena el formulario **por el equipo** (solo una persona por equipo).
- El **nombre del agente es INMUTABLE** una vez registrado.
  - Sin espacios ni tildes (letras, dígitos y `_`).
  - Debe coincidir con el nombre de la clase C# y con el nombre del archivo (`NombreAgente.cs`).

## Entregas (fechas y medio: a definir por el profesor)
1. **Informe**: PDF de máximo 5 páginas, tamaño carta. Secciones: Resumen, Introducción, Objetivos,
   Marco teórico, Metodología, Resultados, Discusión, Conclusiones.
2. **Código**: un solo archivo `NombreAgente.cs`. Clase pública que implementa `IPokerAgent`, con
   constructor sin parámetros. C# 9 / .NET Standard 2.1. Máximo **1000 ms** por decisión
   (objetivo: menos de 300 ms en el bench).

## Verificación (desde `SDK/`)
```
.\run.cmd check Agents\TuAgente.cs
.\run.cmd bench --agent TuAgente --builtin Heuristic*5 --matches 200
.\entrenar.cmd 300
```

## Estructura de la carpeta
- `SDK/` — kit AgentsPoker SDK v0.3.0 (empezar por `SDK/LEEME.md`; plantilla en `SDK/Agents/MiAgente.cs`)
- `Documentos/` — instructivo de entregas, reglas, preguntas frecuentes (`.md`; los PDF están en `pdf/`)
- `Original/` — zip original del SDK como respaldo
