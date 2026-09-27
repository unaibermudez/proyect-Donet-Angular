# Registro de revisión del código generado con IA

Este documento recoge cada vez que el código propuesto por el asistente tuvo que
corregirse: porque yo lo rechacé, porque lo modifiqué, o porque se detectó un
problema después de escribirlo.

El objetivo no es demostrar que la IA se equivoca, sino demostrar el hábito de
**revisar críticamente** lo que genera antes de darlo por bueno.

## Formato de cada entrada

```
### NN - Título corto  (paso N)
- **Qué se generó:** ...
- **Qué problema tenía:** ...
- **Cómo se corrigió:** ...
- **Qué aprendí:** (opcional)
```

---

## Entradas

### 01 - Rama por defecto `master` en lugar de `main`  (paso 1)
- **Qué se generó:** `git init -b main` combinado con `mkdir` en la misma línea.
- **Qué problema tenía:** el comando se ejecutó sobre un repositorio ya
  inicializado, así que git ignoró `--initial-branch` con un aviso y la rama
  quedó como `master`. El aviso se pasó por alto al leer solo el final de la
  salida.
- **Cómo se corrigió:** se comprobó la rama activa con `git branch --show-current`
  y se renombró con `git branch -m master main`.
- **Qué aprendí:** conviene verificar el estado resultante, no solo asumir que el
  comando hizo lo que pedía la bandera.
